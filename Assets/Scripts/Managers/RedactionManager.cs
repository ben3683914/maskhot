using UnityEngine;
using System;
using System.Collections.Generic;
using Maskhot.Data;

namespace Maskhot.Managers
{
    /// <summary>
    /// Singleton manager that owns post unredaction state.
    ///
    /// Responsibilities:
    /// - Track which posts have been unredacted this session
    /// - Fire events when unredaction state changes
    /// - Reset state when queue changes
    ///
    /// Does NOT handle:
    /// - Determining if a post should appear redacted (Controller)
    /// - Getting display text (Controller)
    /// - Validating unredaction requests (Controller)
    /// </summary>
    public class RedactionManager : MonoBehaviour
    {
        public static RedactionManager Instance { get; private set; }

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        /// <summary>
        /// Event fired when a post is marked as unredacted.
        /// Parameter: the post that was unredacted
        /// </summary>
        public event Action<SocialMediaPost> OnPostUnredacted;

        /// <summary>
        /// Event fired when all unredaction state is reset.
        /// </summary>
        public event Action OnRedactionReset;

        // Set of posts that have been unredacted this session
        private HashSet<SocialMediaPost> unredactedPosts = new HashSet<SocialMediaPost>();

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
                Debug.Log("RedactionManager: Initialized");
            }
        }

        private void OnEnable()
        {
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
            }
        }

        private void OnDisable()
        {
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged -= HandleQueueChanged;
            }
        }

        private void Start()
        {
            // Re-subscribe in case MatchQueueManager wasn't ready in OnEnable
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged -= HandleQueueChanged;
                MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the count of unredacted posts this session.
        /// </summary>
        public int UnredactedCount => unredactedPosts.Count;

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if a specific post has been unredacted.
        /// This only checks the unredaction state, not whether the post is guaranteed.
        /// </summary>
        /// <param name="post">The post to check</param>
        /// <returns>True if the post has been unredacted</returns>
        public bool IsUnredacted(SocialMediaPost post)
        {
            if (post == null)
            {
                return false;
            }

            return unredactedPosts.Contains(post);
        }

        /// <summary>
        /// Marks a post as unredacted.
        /// Fires OnPostUnredacted event.
        /// </summary>
        /// <param name="post">The post to mark as unredacted</param>
        /// <returns>True if the post was newly unredacted, false if already unredacted or null</returns>
        public bool MarkUnredacted(SocialMediaPost post)
        {
            if (post == null)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("RedactionManager: MarkUnredacted called with null post");
                }
                return false;
            }

            // Check if already unredacted
            if (unredactedPosts.Contains(post))
            {
                if (verboseLogging)
                {
                    Debug.Log("RedactionManager: Post already unredacted");
                }
                return false;
            }

            // Mark as unredacted
            unredactedPosts.Add(post);

            if (verboseLogging)
            {
                string preview = string.IsNullOrEmpty(post.content) ? "(empty)"
                    : post.content.Length > 30 ? post.content.Substring(0, 27) + "..."
                    : post.content;
                Debug.Log($"RedactionManager: Marked post as unredacted: \"{preview}\"");
            }

            OnPostUnredacted?.Invoke(post);
            return true;
        }

        /// <summary>
        /// Resets all unredaction state.
        /// Called automatically when the queue changes.
        /// </summary>
        public void ResetAll()
        {
            int previousCount = unredactedPosts.Count;
            unredactedPosts.Clear();

            if (verboseLogging)
            {
                Debug.Log($"RedactionManager: Reset all (cleared {previousCount} unredacted posts)");
            }

            OnRedactionReset?.Invoke();
        }

        #endregion

        #region Private Methods

        private void HandleQueueChanged()
        {
            ResetAll();
        }

        #endregion
    }
}
