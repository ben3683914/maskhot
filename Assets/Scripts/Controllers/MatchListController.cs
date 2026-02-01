using UnityEngine;
using System;
using System.Collections.Generic;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Controllers
{
    /// <summary>
    /// Singleton controller that manages candidate selection and navigation.
    /// Provides the UI-facing interface for the candidate queue.
    /// Absorbs the responsibilities of the deprecated SocialFeedController.
    /// </summary>
    public class MatchListController : MonoBehaviour
    {
        public static MatchListController Instance { get; private set; }

        /// <summary>
        /// Event fired when the selected candidate changes.
        /// Passes the new candidate (null if cleared).
        /// </summary>
        public event Action<CandidateProfileSO> OnSelectionChanged;

        /// <summary>
        /// Event fired when the queue is updated.
        /// </summary>
        public event Action OnQueueUpdated;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // Current selection state
        private CandidateProfileSO currentCandidate;
        private int currentIndex = -1;

        #region Properties

        /// <summary>
        /// The currently selected candidate profile.
        /// Returns null if no candidate is selected.
        /// </summary>
        public CandidateProfileSO CurrentCandidate => currentCandidate;

        /// <summary>
        /// The index of the current selection in the queue.
        /// Returns -1 if no candidate is selected.
        /// </summary>
        public int CurrentIndex => currentIndex;

        /// <summary>
        /// Returns true if a candidate is currently selected.
        /// </summary>
        public bool HasSelection => currentCandidate != null;

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

        /// <summary>
        /// Returns true if there is a next candidate to navigate to.
        /// </summary>
        public bool HasNext
        {
            get
            {
                if (MatchQueueManager.Instance == null) return false;
                return currentIndex < MatchQueueManager.Instance.Count - 1;
            }
        }

        /// <summary>
        /// Returns true if there is a previous candidate to navigate to.
        /// </summary>
        public bool HasPrevious => currentIndex > 0;

        /// <summary>
        /// The candidate queue (read-only).
        /// Use this for displaying the candidate list in UI.
        /// </summary>
        public IReadOnlyList<CandidateProfileSO> Queue =>
            MatchQueueManager.Instance?.Queue ?? (IReadOnlyList<CandidateProfileSO>)Array.Empty<CandidateProfileSO>();

        /// <summary>
        /// Total number of candidates in the queue.
        /// </summary>
        public int Count => MatchQueueManager.Instance?.Count ?? 0;

        /// <summary>
        /// Number of candidates still pending a decision.
        /// </summary>
        public int PendingCount => MatchQueueManager.Instance?.PendingCount ?? 0;

        /// <summary>
        /// Gets the decision state for a candidate.
        /// Use this for styling candidates in the queue UI.
        /// </summary>
        public CandidateDecision GetDecision(CandidateProfileSO candidate)
        {
            return MatchQueueManager.Instance?.GetDecision(candidate) ?? CandidateDecision.Pending;
        }

        #endregion

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
                Debug.Log("MatchListController: Initialized");
            }
        }

        private void OnEnable()
        {
            // Subscribe to queue changes
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from queue changes
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged -= HandleQueueChanged;
            }
        }

        private void Start()
        {
            // Re-subscribe in Start in case MatchQueueManager wasn't ready in OnEnable
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged -= HandleQueueChanged;
                MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
            }
        }

        #endregion

        #region Selection Methods

        /// <summary>
        /// Selects a candidate by index in the queue.
        /// </summary>
        /// <param name="index">The index to select</param>
        /// <returns>True if selection was successful</returns>
        public bool SelectByIndex(int index)
        {
            if (MatchQueueManager.Instance == null)
            {
                Debug.LogWarning("MatchListController: MatchQueueManager not available");
                return false;
            }

            var candidate = MatchQueueManager.Instance.GetCandidateAt(index);
            if (candidate == null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"MatchListController: Invalid index {index}");
                }
                return false;
            }

            return SelectCandidate(candidate);
        }

        /// <summary>
        /// Selects a specific candidate.
        /// </summary>
        /// <param name="candidate">The candidate to select</param>
        /// <returns>True if selection was successful</returns>
        public bool SelectCandidate(CandidateProfileSO candidate)
        {
            if (candidate == null)
            {
                ClearSelection();
                return true;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogWarning("MatchListController: MatchQueueManager not available");
                return false;
            }

            if (!MatchQueueManager.Instance.IsInQueue(candidate))
            {
                Debug.LogWarning($"MatchListController: Candidate {candidate.profile.characterName} not in queue");
                return false;
            }

            if (currentCandidate == candidate)
            {
                if (verboseLogging)
                {
                    Debug.Log($"MatchListController: {candidate.profile.characterName} already selected, skipping");
                }
                return true;
            }

            var previousCandidate = currentCandidate;
            currentCandidate = candidate;
            currentIndex = MatchQueueManager.Instance.GetIndexOf(candidate);

            if (verboseLogging)
            {
                string previousName = previousCandidate != null ? previousCandidate.profile.characterName : "null";
                Debug.Log($"MatchListController: Selection changed from {previousName} to {candidate.profile.characterName} (index {currentIndex})");
                Debug.Log($"  Posts available: {CurrentPosts.Count}");
            }

            OnSelectionChanged?.Invoke(currentCandidate);
            return true;
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearSelection()
        {
            if (currentCandidate == null) return;

            var previousCandidate = currentCandidate;
            currentCandidate = null;
            currentIndex = -1;

            if (verboseLogging)
            {
                Debug.Log($"MatchListController: Cleared selection (was {previousCandidate.profile.characterName})");
            }

            OnSelectionChanged?.Invoke(null);
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Selects the next candidate in the queue.
        /// </summary>
        /// <returns>True if navigation was successful</returns>
        public bool SelectNext()
        {
            if (!HasNext)
            {
                if (verboseLogging)
                {
                    Debug.Log("MatchListController: No next candidate available");
                }
                return false;
            }

            return SelectByIndex(currentIndex + 1);
        }

        /// <summary>
        /// Selects the previous candidate in the queue.
        /// </summary>
        /// <returns>True if navigation was successful</returns>
        public bool SelectPrevious()
        {
            if (!HasPrevious)
            {
                if (verboseLogging)
                {
                    Debug.Log("MatchListController: No previous candidate available");
                }
                return false;
            }

            return SelectByIndex(currentIndex - 1);
        }

        /// <summary>
        /// Selects the first candidate in the queue.
        /// </summary>
        /// <returns>True if navigation was successful</returns>
        public bool SelectFirst()
        {
            if (MatchQueueManager.Instance == null || MatchQueueManager.Instance.Count == 0)
            {
                return false;
            }

            return SelectByIndex(0);
        }

        /// <summary>
        /// Selects the first pending (undecided) candidate.
        /// Useful for auto-advancing after a decision.
        /// </summary>
        /// <returns>True if a pending candidate was found and selected</returns>
        public bool SelectNextPending()
        {
            if (MatchQueueManager.Instance == null) return false;

            var pending = MatchQueueManager.Instance.GetPendingCandidates();
            if (pending.Count == 0)
            {
                if (verboseLogging)
                {
                    Debug.Log("MatchListController: No pending candidates remaining");
                }
                return false;
            }

            // Find the first pending candidate after current index, or first overall
            var queue = MatchQueueManager.Instance.Queue;
            for (int i = currentIndex + 1; i < queue.Count; i++)
            {
                if (MatchQueueManager.Instance.GetDecision(queue[i]) == CandidateDecision.Pending)
                {
                    return SelectByIndex(i);
                }
            }

            // Wrap around to beginning
            for (int i = 0; i <= currentIndex && i < queue.Count; i++)
            {
                if (MatchQueueManager.Instance.GetDecision(queue[i]) == CandidateDecision.Pending)
                {
                    return SelectByIndex(i);
                }
            }

            return false;
        }

        #endregion

        #region Event Handlers

        private void HandleQueueChanged()
        {
            // If current selection is no longer in queue, clear it
            if (currentCandidate != null && MatchQueueManager.Instance != null)
            {
                if (!MatchQueueManager.Instance.IsInQueue(currentCandidate))
                {
                    ClearSelection();
                }
                else
                {
                    // Update index in case queue order changed
                    currentIndex = MatchQueueManager.Instance.GetIndexOf(currentCandidate);
                }
            }

            OnQueueUpdated?.Invoke();
        }

        #endregion
    }
}
