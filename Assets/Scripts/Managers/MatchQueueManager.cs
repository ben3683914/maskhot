using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Maskhot.Controllers;
using Maskhot.Data;

namespace Maskhot.Managers
{
    /// <summary>
    /// Tracks the decision state for a candidate in the queue.
    /// </summary>
    public enum CandidateDecision
    {
        Pending,
        Accepted,
        Rejected
    }

    /// <summary>
    /// Singleton manager that handles the candidate queue for a matching session.
    /// Manages queue population, decision tracking, and candidate filtering.
    /// Does not handle selection state - that's MatchListController's responsibility.
    /// </summary>
    public class MatchQueueManager : MonoBehaviour
    {
        public static MatchQueueManager Instance { get; private set; }

        [Header("Queue Settings")]
        [Tooltip("Default number of candidates when populating queue")]
        public int defaultQueueSize = 5;

        [Tooltip("Minimum good matches to include when populating for a quest (0 = random)")]
        public int minGoodMatches = 1;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // The queue of candidates for the current session
        private List<CandidateProfileSO> queue = new List<CandidateProfileSO>();

        // Decision tracking per candidate (by instance ID for safety)
        private Dictionary<int, CandidateDecision> decisions = new Dictionary<int, CandidateDecision>();

        /// <summary>
        /// Event fired when the queue is modified (populated, cleared, etc.).
        /// </summary>
        public event Action OnQueueChanged;

        /// <summary>
        /// Event fired when a decision is made on a candidate.
        /// </summary>
        public event Action<CandidateProfileSO, CandidateDecision> OnDecisionMade;

        #region Properties

        /// <summary>
        /// Returns a read-only view of the current queue.
        /// </summary>
        public IReadOnlyList<CandidateProfileSO> Queue => queue.AsReadOnly();

        /// <summary>
        /// Returns the number of candidates in the queue.
        /// </summary>
        public int Count => queue.Count;

        /// <summary>
        /// Returns the number of candidates still pending decision.
        /// </summary>
        public int PendingCount => queue.Count(c => GetDecision(c) == CandidateDecision.Pending);

        /// <summary>
        /// Returns the number of accepted candidates.
        /// </summary>
        public int AcceptedCount => queue.Count(c => GetDecision(c) == CandidateDecision.Accepted);

        /// <summary>
        /// Returns the number of rejected candidates.
        /// </summary>
        public int RejectedCount => queue.Count(c => GetDecision(c) == CandidateDecision.Rejected);

        /// <summary>
        /// Returns true if the queue has any candidates.
        /// </summary>
        public bool HasCandidates => queue.Count > 0;

        /// <summary>
        /// Returns true if all candidates have been decided.
        /// </summary>
        public bool AllDecided => queue.Count > 0 && PendingCount == 0;

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
                Debug.Log("MatchQueueManager: Initialized");
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            // Re-subscribe in Start in case QuestManager wasn't ready in OnEnable
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleQuestStarted(Quest quest)
        {
            // Populate queue for the new quest
            if (ProfileManager.Instance != null)
            {
                PopulateForQuest(quest);

                // Auto-select first candidate
                if (MatchListController.Instance != null && queue.Count > 0)
                {
                    MatchListController.Instance.SelectFirst();
                }
            }

            if (verboseLogging)
            {
                string clientName = quest?.client?.clientName ?? "Unknown";
                Debug.Log($"MatchQueueManager: Quest started for '{clientName}', populated {queue.Count} candidates");
            }
        }

        #endregion

        #region Queue Population

        /// <summary>
        /// Populates the queue with random candidates.
        /// Clears any existing queue and decisions.
        /// </summary>
        /// <param name="count">Number of candidates to add (uses defaultQueueSize if 0)</param>
        public void PopulateRandom(int count = 0)
        {
            if (count <= 0) count = defaultQueueSize;

            ClearQueue();

            var allCandidates = ProfileManager.Instance.GetAllCandidates();
            if (allCandidates == null || allCandidates.Length == 0)
            {
                Debug.LogWarning("MatchQueueManager: No candidates available to populate queue");
                return;
            }

            // Shuffle and take up to count
            var shuffled = allCandidates.OrderBy(_ => UnityEngine.Random.value).ToList();
            int toTake = Mathf.Min(count, shuffled.Count);

            for (int i = 0; i < toTake; i++)
            {
                queue.Add(shuffled[i]);
                decisions[shuffled[i].GetInstanceID()] = CandidateDecision.Pending;
            }

            if (verboseLogging)
            {
                Debug.Log($"MatchQueueManager: Populated queue with {queue.Count} random candidates");
            }

            OnQueueChanged?.Invoke();
        }

        /// <summary>
        /// Populates the queue with a balanced mix of candidates for a quest.
        /// Ensures at least minGoodMatches candidates that pass the criteria.
        /// </summary>
        /// <param name="quest">The quest to balance candidates for</param>
        /// <param name="count">Total number of candidates (uses defaultQueueSize if 0)</param>
        public void PopulateForQuest(Quest quest, int count = 0)
        {
            if (count <= 0) count = defaultQueueSize;
            if (quest == null || quest.matchCriteria == null)
            {
                Debug.LogWarning("MatchQueueManager: Invalid quest, falling back to random population");
                PopulateRandom(count);
                return;
            }

            ClearQueue();

            var allCandidates = ProfileManager.Instance.GetAllCandidates();
            if (allCandidates == null || allCandidates.Length == 0)
            {
                Debug.LogWarning("MatchQueueManager: No candidates available to populate queue");
                return;
            }

            // Evaluate all candidates
            var goodMatches = new List<CandidateProfileSO>();
            var badMatches = new List<CandidateProfileSO>();

            foreach (var candidate in allCandidates)
            {
                var result = Maskhot.Matching.MatchEvaluator.Evaluate(candidate, quest.matchCriteria);
                if (result.IsMatch)
                {
                    goodMatches.Add(candidate);
                }
                else
                {
                    badMatches.Add(candidate);
                }
            }

            // Shuffle both lists
            goodMatches = goodMatches.OrderBy(_ => UnityEngine.Random.value).ToList();
            badMatches = badMatches.OrderBy(_ => UnityEngine.Random.value).ToList();

            // Take at least minGoodMatches good ones
            int goodToTake = Mathf.Min(minGoodMatches, goodMatches.Count);
            int remainingSlots = count - goodToTake;

            // Fill remaining with a mix (prefer bad matches to make it challenging)
            var selected = new List<CandidateProfileSO>();
            selected.AddRange(goodMatches.Take(goodToTake));

            // Fill rest from bad matches first, then good if needed
            int badToTake = Mathf.Min(remainingSlots, badMatches.Count);
            selected.AddRange(badMatches.Take(badToTake));

            int stillNeeded = count - selected.Count;
            if (stillNeeded > 0)
            {
                selected.AddRange(goodMatches.Skip(goodToTake).Take(stillNeeded));
            }

            // Shuffle final selection so good matches aren't always first
            queue = selected.OrderBy(_ => UnityEngine.Random.value).ToList();

            foreach (var candidate in queue)
            {
                decisions[candidate.GetInstanceID()] = CandidateDecision.Pending;
            }

            if (verboseLogging)
            {
                Debug.Log($"MatchQueueManager: Populated queue with {queue.Count} candidates " +
                         $"({goodToTake} good matches guaranteed) for quest");
            }

            OnQueueChanged?.Invoke();
        }

        /// <summary>
        /// Clears the queue and all decision tracking.
        /// </summary>
        public void ClearQueue()
        {
            queue.Clear();
            decisions.Clear();

            if (verboseLogging)
            {
                Debug.Log("MatchQueueManager: Queue cleared");
            }

            OnQueueChanged?.Invoke();
        }

        #endregion

        #region Decision Tracking

        /// <summary>
        /// Gets the decision state for a candidate.
        /// </summary>
        public CandidateDecision GetDecision(CandidateProfileSO candidate)
        {
            if (candidate == null) return CandidateDecision.Pending;

            if (decisions.TryGetValue(candidate.GetInstanceID(), out var decision))
            {
                return decision;
            }

            return CandidateDecision.Pending;
        }

        /// <summary>
        /// Marks a candidate as accepted.
        /// </summary>
        public void Accept(CandidateProfileSO candidate)
        {
            SetDecision(candidate, CandidateDecision.Accepted);
        }

        /// <summary>
        /// Marks a candidate as rejected.
        /// </summary>
        public void Reject(CandidateProfileSO candidate)
        {
            SetDecision(candidate, CandidateDecision.Rejected);
        }

        /// <summary>
        /// Resets a candidate's decision back to pending.
        /// </summary>
        public void ResetDecision(CandidateProfileSO candidate)
        {
            SetDecision(candidate, CandidateDecision.Pending);
        }

        private void SetDecision(CandidateProfileSO candidate, CandidateDecision decision)
        {
            if (candidate == null) return;

            int id = candidate.GetInstanceID();
            if (!decisions.ContainsKey(id))
            {
                Debug.LogWarning($"MatchQueueManager: Candidate {candidate.profile.characterName} not in queue");
                return;
            }

            var previousDecision = decisions[id];
            decisions[id] = decision;

            if (verboseLogging)
            {
                Debug.Log($"MatchQueueManager: {candidate.profile.characterName} " +
                         $"{previousDecision} -> {decision}");
            }

            OnDecisionMade?.Invoke(candidate, decision);
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets the candidate at the specified index.
        /// </summary>
        public CandidateProfileSO GetCandidateAt(int index)
        {
            if (index < 0 || index >= queue.Count) return null;
            return queue[index];
        }

        /// <summary>
        /// Gets the index of a candidate in the queue.
        /// Returns -1 if not found.
        /// </summary>
        public int GetIndexOf(CandidateProfileSO candidate)
        {
            if (candidate == null) return -1;
            return queue.IndexOf(candidate);
        }

        /// <summary>
        /// Returns all pending candidates.
        /// </summary>
        public List<CandidateProfileSO> GetPendingCandidates()
        {
            return queue.Where(c => GetDecision(c) == CandidateDecision.Pending).ToList();
        }

        /// <summary>
        /// Returns all accepted candidates.
        /// </summary>
        public List<CandidateProfileSO> GetAcceptedCandidates()
        {
            return queue.Where(c => GetDecision(c) == CandidateDecision.Accepted).ToList();
        }

        /// <summary>
        /// Returns all rejected candidates.
        /// </summary>
        public List<CandidateProfileSO> GetRejectedCandidates()
        {
            return queue.Where(c => GetDecision(c) == CandidateDecision.Rejected).ToList();
        }

        /// <summary>
        /// Checks if a candidate is in the current queue.
        /// </summary>
        public bool IsInQueue(CandidateProfileSO candidate)
        {
            return candidate != null && queue.Contains(candidate);
        }

        #endregion
    }
}
