using UnityEngine;
using System;
using System.Collections.Generic;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Controllers
{
    /// <summary>
    /// Singleton controller that provides a UI-facing interface for post redaction.
    /// Subscribes to RedactionManager events and provides display-ready methods.
    /// UI should use this instead of accessing RedactionManager directly.
    ///
    /// Responsibilities:
    /// - Determine if a post should appear redacted (combines guaranteed + unredacted state)
    /// - Provide display text (full content or block characters)
    /// - Validate and execute unredact requests
    /// - Compute counts for UI display
    /// - Fire events for UI updates
    /// </summary>
    public class RedactionController : MonoBehaviour
    {
        public static RedactionController Instance { get; private set; }

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        #region Events

        /// <summary>
        /// Fired when a post is unredacted.
        /// Parameters: (CandidateProfileSO candidate, SocialMediaPost post)
        /// </summary>
        public event Action<CandidateProfileSO, SocialMediaPost> OnPostUnredacted;

        /// <summary>
        /// Fired when redaction state is reset (e.g., new quest started).
        /// </summary>
        public event Action OnRedactionReset;

        #endregion

        // Cache of which candidate owns each recently unredacted post (for event enrichment)
        private Dictionary<SocialMediaPost, CandidateProfileSO> postOwnerCache = new Dictionary<SocialMediaPost, CandidateProfileSO>();

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (verboseLogging)
            {
                Debug.Log("RedactionController: Initialized");
            }
        }

        private void OnEnable()
        {
            if (RedactionManager.Instance != null)
            {
                RedactionManager.Instance.OnPostUnredacted += HandlePostUnredacted;
                RedactionManager.Instance.OnRedactionReset += HandleRedactionReset;
            }
        }

        private void OnDisable()
        {
            if (RedactionManager.Instance != null)
            {
                RedactionManager.Instance.OnPostUnredacted -= HandlePostUnredacted;
                RedactionManager.Instance.OnRedactionReset -= HandleRedactionReset;
            }
        }

        private void Start()
        {
            // Re-subscribe in case RedactionManager wasn't ready in OnEnable
            if (RedactionManager.Instance != null)
            {
                RedactionManager.Instance.OnPostUnredacted -= HandlePostUnredacted;
                RedactionManager.Instance.OnRedactionReset -= HandleRedactionReset;

                RedactionManager.Instance.OnPostUnredacted += HandlePostUnredacted;
                RedactionManager.Instance.OnRedactionReset += HandleRedactionReset;
            }
        }

        #endregion

        #region Public Methods - Redaction Status

        /// <summary>
        /// Checks if a post is currently redacted.
        /// A post is visible (not redacted) if:
        /// - It's a guaranteed post, OR
        /// - It has been unredacted via the manager
        /// </summary>
        /// <param name="candidate">The candidate who owns this post</param>
        /// <param name="post">The post to check</param>
        /// <returns>True if the post is redacted, false if visible</returns>
        public bool IsRedacted(CandidateProfileSO candidate, SocialMediaPost post)
        {
            if (candidate == null || post == null)
            {
                return false;
            }

            // Guaranteed posts are never redacted
            if (IsGuaranteedPost(candidate, post))
            {
                return false;
            }

            // Check if this post has been unredacted
            if (RedactionManager.Instance != null && RedactionManager.Instance.IsUnredacted(post))
            {
                return false;
            }

            // Default: random posts are redacted
            return true;
        }

        /// <summary>
        /// Checks if a specific post is a guaranteed post (always visible).
        /// </summary>
        /// <param name="candidate">The candidate who owns this post</param>
        /// <param name="post">The post to check</param>
        /// <returns>True if the post is guaranteed</returns>
        public bool IsGuaranteedPost(CandidateProfileSO candidate, SocialMediaPost post)
        {
            if (candidate == null || post == null)
            {
                return false;
            }

            return candidate.guaranteedPosts.Contains(post);
        }

        #endregion

        #region Public Methods - Display

        /// <summary>
        /// Gets the display text for a post (full content or block characters).
        /// </summary>
        /// <param name="candidate">The candidate who owns this post</param>
        /// <param name="post">The post to get text for</param>
        /// <returns>The content to display</returns>
        public string GetDisplayText(CandidateProfileSO candidate, SocialMediaPost post)
        {
            if (post == null)
            {
                return string.Empty;
            }

            if (IsRedacted(candidate, post))
            {
                return post.GetRedactedContent();
            }

            return post.content;
        }

        #endregion

        #region Public Methods - Unredaction

        /// <summary>
        /// Attempts to unredact a post.
        /// Validates the request and delegates to RedactionManager.
        /// </summary>
        /// <param name="candidate">The candidate who owns this post</param>
        /// <param name="post">The post to unredact</param>
        /// <returns>True if the post was unredacted, false if invalid or already visible</returns>
        public bool TryUnredact(CandidateProfileSO candidate, SocialMediaPost post)
        {
            if (candidate == null || post == null)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("RedactionController: TryUnredact called with null candidate or post");
                }
                return false;
            }

            // Can't unredact guaranteed posts (they're already visible)
            if (IsGuaranteedPost(candidate, post))
            {
                if (verboseLogging)
                {
                    Debug.Log("RedactionController: Post is guaranteed, already visible");
                }
                return false;
            }

            // Check if already unredacted
            if (RedactionManager.Instance != null && RedactionManager.Instance.IsUnredacted(post))
            {
                if (verboseLogging)
                {
                    Debug.Log("RedactionController: Post already unredacted");
                }
                return false;
            }

            // Try to spend money first
            if (MoneyController.Instance == null || !MoneyController.Instance.TrySpendForUnredact())
            {
                if (verboseLogging)
                {
                    Debug.Log("RedactionController: Cannot afford to unredact");
                }
                return false;
            }

            // Cache the owner for event enrichment
            postOwnerCache[post] = candidate;

            // Delegate to manager
            if (RedactionManager.Instance != null)
            {
                bool result = RedactionManager.Instance.MarkUnredacted(post);

                if (verboseLogging && result)
                {
                    string preview = string.IsNullOrEmpty(post.content) ? "(empty)"
                        : post.content.Length > 30 ? post.content.Substring(0, 27) + "..."
                        : post.content;
                    Debug.Log($"RedactionController: Unredacted post for {candidate.profile.characterName}: \"{preview}\"");
                }

                return result;
            }

            return false;
        }

        #endregion

        #region Public Methods - Counts

        /// <summary>
        /// Gets the count of redacted posts for a candidate.
        /// </summary>
        /// <param name="candidate">The candidate to check</param>
        /// <returns>Number of redacted posts</returns>
        public int GetRedactedCount(CandidateProfileSO candidate)
        {
            if (candidate == null)
            {
                return 0;
            }

            var posts = candidate.GetPostsForPlaythrough();
            int count = 0;

            foreach (var post in posts)
            {
                if (IsRedacted(candidate, post))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets the count of visible (unredacted) posts for a candidate.
        /// Includes guaranteed posts.
        /// </summary>
        /// <param name="candidate">The candidate to check</param>
        /// <returns>Number of visible posts</returns>
        public int GetVisibleCount(CandidateProfileSO candidate)
        {
            if (candidate == null)
            {
                return 0;
            }

            var posts = candidate.GetPostsForPlaythrough();
            return posts.Count - GetRedactedCount(candidate);
        }

        /// <summary>
        /// Gets the total post count for a candidate.
        /// </summary>
        /// <param name="candidate">The candidate to check</param>
        /// <returns>Total number of posts</returns>
        public int GetTotalCount(CandidateProfileSO candidate)
        {
            if (candidate == null)
            {
                return 0;
            }

            return candidate.GetPostsForPlaythrough().Count;
        }

        /// <summary>
        /// Gets the guaranteed post count for a candidate.
        /// </summary>
        /// <param name="candidate">The candidate to check</param>
        /// <returns>Number of guaranteed posts</returns>
        public int GetGuaranteedCount(CandidateProfileSO candidate)
        {
            if (candidate == null)
            {
                return 0;
            }

            return candidate.guaranteedPosts.Count;
        }

        #endregion

        #region Event Handlers

        private void HandlePostUnredacted(SocialMediaPost post)
        {
            // Look up the candidate from our cache
            CandidateProfileSO candidate = null;
            if (postOwnerCache.TryGetValue(post, out candidate))
            {
                // Keep in cache in case of future lookups
            }

            if (verboseLogging)
            {
                string candidateName = candidate?.profile.characterName ?? "(unknown)";
                Debug.Log($"RedactionController: Received OnPostUnredacted for {candidateName}");
            }

            // Fire our enriched event
            OnPostUnredacted?.Invoke(candidate, post);
        }

        private void HandleRedactionReset()
        {
            // Clear our owner cache
            postOwnerCache.Clear();

            if (verboseLogging)
            {
                Debug.Log("RedactionController: Received OnRedactionReset");
            }

            OnRedactionReset?.Invoke();
        }

        #endregion
    }
}
