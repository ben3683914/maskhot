using UnityEngine;
using System;
using System.Collections.Generic;
using Maskhot.Data;

namespace Maskhot.Controllers
{
    /// <summary>
    /// Singleton controller that manages the currently selected candidate's social feed data.
    /// Provides data access for UI components - does not handle rendering.
    /// </summary>
    public class SocialFeedController : MonoBehaviour
    {
        public static SocialFeedController Instance { get; private set; }

        /// <summary>
        /// Event fired when the current candidate changes.
        /// Passes the new candidate (null if cleared).
        /// </summary>
        public event Action<CandidateProfileSO> OnCandidateChanged;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // Current candidate being viewed
        private CandidateProfileSO currentCandidate;

        /// <summary>
        /// The currently selected candidate profile.
        /// Returns null if no candidate is selected.
        /// </summary>
        public CandidateProfileSO CurrentCandidate => currentCandidate;

        /// <summary>
        /// Returns true if a candidate is currently selected.
        /// </summary>
        public bool HasCandidate => currentCandidate != null;

        /// <summary>
        /// Gets the posts for the current candidate.
        /// Returns an empty list if no candidate is selected.
        /// Posts are cached per session - switching candidates preserves their posts.
        /// </summary>
        public List<SocialMediaPost> CurrentPosts
        {
            get
            {
                if (currentCandidate == null)
                {
                    return new List<SocialMediaPost>();
                }
                return currentCandidate.GetPostsForPlaythrough();
            }
        }

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
                Debug.Log("SocialFeedController: Initialized");
            }
        }

        /// <summary>
        /// Sets the current candidate to display.
        /// Pass null to clear the feed.
        /// </summary>
        /// <param name="candidate">The candidate profile to display, or null to clear</param>
        public void SetCandidate(CandidateProfileSO candidate)
        {
            if (currentCandidate == candidate)
            {
                if (verboseLogging)
                {
                    string name = candidate != null ? candidate.profile.characterName : "null";
                    Debug.Log($"SocialFeedController: Candidate already set to {name}, skipping");
                }
                return;
            }

            CandidateProfileSO previousCandidate = currentCandidate;
            currentCandidate = candidate;

            if (verboseLogging)
            {
                string previousName = previousCandidate != null ? previousCandidate.profile.characterName : "null";
                string newName = candidate != null ? candidate.profile.characterName : "null";
                Debug.Log($"SocialFeedController: Changed candidate from {previousName} to {newName}");

                if (candidate != null)
                {
                    Debug.Log($"  Posts available: {CurrentPosts.Count}");
                }
            }

            OnCandidateChanged?.Invoke(currentCandidate);
        }

        /// <summary>
        /// Clears the current candidate selection.
        /// Equivalent to calling SetCandidate(null).
        /// </summary>
        public void ClearFeed()
        {
            SetCandidate(null);
        }
    }
}
