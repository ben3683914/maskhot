using UnityEngine;
using System;
using System.Collections.Generic;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Matching;

namespace Maskhot.Controllers
{
    /// <summary>
    /// Singleton controller that handles accept/reject decisions and scoring.
    /// Provides UI-facing interface for decision-making and tracks session statistics.
    /// Evaluates decisions against current quest criteria to determine correctness.
    /// </summary>
    public class DecisionController : MonoBehaviour
    {
        public static DecisionController Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Automatically advance to next pending candidate after decision")]
        public bool autoAdvance = true;

        [Header("Rewards")]
        [Tooltip("Multiplier for correct decision rewards (reward = score × multiplier)")]
        public float correctRewardMultiplier = 3f;

        [Tooltip("Flat reward for incorrect decisions (to continue playing)")]
        public int incorrectReward = 50;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // Decision results tracking (by candidate instance ID)
        private Dictionary<int, DecisionResult> decisionResults = new Dictionary<int, DecisionResult>();

        // Session statistics
        private int truePositives = 0;
        private int trueNegatives = 0;
        private int falsePositives = 0;
        private int falseNegatives = 0;

        #region Events

        /// <summary>
        /// Fired when a decision is made on a candidate.
        /// Includes the full decision result with correctness and scoring data.
        /// </summary>
        public event Action<DecisionResult> OnDecisionResult;

        /// <summary>
        /// Fired when all candidates in the queue have been decided.
        /// </summary>
        public event Action OnAllDecisionsComplete;

        #endregion

        #region Properties

        /// <summary>
        /// Number of correct accepts (accepted valid matches).
        /// </summary>
        public int CorrectAccepts => truePositives;

        /// <summary>
        /// Number of correct rejects (rejected non-matches).
        /// </summary>
        public int CorrectRejects => trueNegatives;

        /// <summary>
        /// Number of incorrect accepts (accepted non-matches).
        /// </summary>
        public int FalseAccepts => falsePositives;

        /// <summary>
        /// Number of incorrect rejects (rejected valid matches).
        /// </summary>
        public int FalseRejects => falseNegatives;

        /// <summary>
        /// Total number of correct decisions.
        /// </summary>
        public int TotalCorrect => truePositives + trueNegatives;

        /// <summary>
        /// Total number of incorrect decisions.
        /// </summary>
        public int TotalIncorrect => falsePositives + falseNegatives;

        /// <summary>
        /// Total decisions made this session.
        /// </summary>
        public int TotalDecisions => TotalCorrect + TotalIncorrect;

        /// <summary>
        /// Accuracy as a percentage (0-100). Returns 0 if no decisions made.
        /// </summary>
        public float Accuracy
        {
            get
            {
                if (TotalDecisions == 0) return 0f;
                return (TotalCorrect / (float)TotalDecisions) * 100f;
            }
        }

        /// <summary>
        /// Returns true if all candidates in the queue have been decided.
        /// </summary>
        public bool AllDecided
        {
            get
            {
                if (MatchQueueManager.Instance == null) return false;
                return MatchQueueManager.Instance.AllDecided;
            }
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
                Debug.Log("DecisionController: Initialized");
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
            // Re-subscribe in Start in case managers weren't ready in OnEnable
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
                QuestManager.Instance.OnQuestCleared += HandleQuestCleared;
            }

            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
                QuestManager.Instance.OnQuestCleared -= HandleQuestCleared;
            }

            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.OnQueueChanged -= HandleQueueChanged;
            }
        }

        #endregion

        #region Decision Methods

        /// <summary>
        /// Accepts the currently selected candidate.
        /// </summary>
        /// <returns>The decision result, or null if no candidate selected</returns>
        public DecisionResult? AcceptCurrent()
        {
            return MakeDecision(CandidateDecision.Accepted);
        }

        /// <summary>
        /// Rejects the currently selected candidate.
        /// </summary>
        /// <returns>The decision result, or null if no candidate selected</returns>
        public DecisionResult? RejectCurrent()
        {
            return MakeDecision(CandidateDecision.Rejected);
        }

        /// <summary>
        /// Makes a decision on a specific candidate.
        /// </summary>
        /// <param name="candidate">The candidate to decide on</param>
        /// <param name="decision">Accept or Reject</param>
        /// <returns>The decision result, or null if invalid</returns>
        public DecisionResult? MakeDecisionOn(CandidateProfileSO candidate, CandidateDecision decision)
        {
            if (candidate == null)
            {
                Debug.LogWarning("DecisionController: Cannot make decision on null candidate");
                return null;
            }

            if (decision == CandidateDecision.Pending)
            {
                Debug.LogWarning("DecisionController: Cannot set decision to Pending, use ResetDecision instead");
                return null;
            }

            return ProcessDecision(candidate, decision);
        }

        private DecisionResult? MakeDecision(CandidateDecision decision)
        {
            if (MatchListController.Instance == null)
            {
                Debug.LogWarning("DecisionController: MatchListController not available");
                return null;
            }

            var candidate = MatchListController.Instance.CurrentCandidate;
            if (candidate == null)
            {
                Debug.LogWarning("DecisionController: No candidate currently selected");
                return null;
            }

            return ProcessDecision(candidate, decision);
        }

        private DecisionResult? ProcessDecision(CandidateProfileSO candidate, CandidateDecision decision)
        {
            if (MatchQueueManager.Instance == null)
            {
                Debug.LogWarning("DecisionController: MatchQueueManager not available");
                return null;
            }

            if (!MatchQueueManager.Instance.IsInQueue(candidate))
            {
                Debug.LogWarning($"DecisionController: Candidate {candidate.profile.characterName} not in queue");
                return null;
            }

            // Check if already decided
            var existingDecision = MatchQueueManager.Instance.GetDecision(candidate);
            if (existingDecision != CandidateDecision.Pending)
            {
                if (verboseLogging)
                {
                    Debug.Log($"DecisionController: {candidate.profile.characterName} already decided ({existingDecision})");
                }
                // Return existing result if we have it
                if (decisionResults.TryGetValue(candidate.GetInstanceID(), out var existingResult))
                {
                    return existingResult;
                }
            }

            // Evaluate the candidate against current quest criteria
            var matchResult = EvaluateCandidate(candidate);
            bool wasActualMatch = matchResult?.IsMatch ?? false;
            float matchScore = matchResult?.Score ?? 0f;

            // Determine if decision was correct
            bool isCorrect = (decision == CandidateDecision.Accepted && wasActualMatch) ||
                            (decision == CandidateDecision.Rejected && !wasActualMatch);

            // Create result
            var result = new DecisionResult
            {
                Candidate = candidate,
                Decision = decision,
                WasActualMatch = wasActualMatch,
                IsCorrect = isCorrect,
                MatchScore = matchScore,
                MatchEvaluation = matchResult
            };

            // Record the decision in MatchQueueManager
            if (decision == CandidateDecision.Accepted)
            {
                MatchQueueManager.Instance.Accept(candidate);
            }
            else
            {
                MatchQueueManager.Instance.Reject(candidate);
            }

            // Track statistics
            UpdateStatistics(result.Outcome);

            // Store result
            decisionResults[candidate.GetInstanceID()] = result;

            if (verboseLogging)
            {
                string correctStr = isCorrect ? "CORRECT" : "INCORRECT";
                string matchStr = wasActualMatch ? "was a match" : "was NOT a match";
                Debug.Log($"DecisionController: {candidate.profile.characterName} - {decision} ({correctStr})");
                Debug.Log($"  Candidate {matchStr} (score: {matchScore:F0})");
            }

            // Fire event
            OnDecisionResult?.Invoke(result);

            // Determine next action based on decision outcome
            // Correct accept (TruePositive) = found a match, quest complete
            // Incorrect or reject = continue reviewing
            if (result.Outcome == DecisionOutcome.TruePositive)
            {
                // Found a good match! Quest complete - award score-based reward
                int reward = Mathf.RoundToInt(result.MatchScore * correctRewardMultiplier);
                if (reward > 0 && MoneyController.Instance != null)
                {
                    MoneyController.Instance.AddMoney(reward);
                }

                if (verboseLogging)
                {
                    Debug.Log($"DecisionController: Correct match found! Quest complete. Reward: ${reward}");
                }
                OnAllDecisionsComplete?.Invoke();
            }
            else if (MatchQueueManager.Instance.AllDecided)
            {
                // Ran out of candidates without finding a match - award consolation reward
                if (incorrectReward > 0 && MoneyController.Instance != null)
                {
                    MoneyController.Instance.AddMoney(incorrectReward);
                }

                if (verboseLogging)
                {
                    Debug.Log($"DecisionController: All candidates exhausted. Consolation: ${incorrectReward}");
                }
                OnAllDecisionsComplete?.Invoke();
            }
            else if (autoAdvance && MatchListController.Instance != null)
            {
                // Continue reviewing - advance to next pending candidate
                MatchListController.Instance.SelectNextPending();
            }

            return result;
        }

        private MatchResult EvaluateCandidate(CandidateProfileSO candidate)
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.HasActiveQuest)
            {
                if (verboseLogging)
                {
                    Debug.Log("DecisionController: No active quest, cannot evaluate match");
                }
                return null;
            }

            var quest = QuestManager.Instance.CurrentQuest;
            if (quest.matchCriteria == null)
            {
                if (verboseLogging)
                {
                    Debug.Log("DecisionController: Quest has no match criteria");
                }
                return null;
            }

            return MatchEvaluator.Evaluate(candidate, quest.matchCriteria);
        }

        private void UpdateStatistics(DecisionOutcome outcome)
        {
            switch (outcome)
            {
                case DecisionOutcome.TruePositive:
                    truePositives++;
                    break;
                case DecisionOutcome.TrueNegative:
                    trueNegatives++;
                    break;
                case DecisionOutcome.FalsePositive:
                    falsePositives++;
                    break;
                case DecisionOutcome.FalseNegative:
                    falseNegatives++;
                    break;
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets the decision result for a specific candidate.
        /// Returns null if no decision has been made.
        /// </summary>
        public DecisionResult? GetResultForCandidate(CandidateProfileSO candidate)
        {
            if (candidate == null) return null;

            if (decisionResults.TryGetValue(candidate.GetInstanceID(), out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Checks if a candidate has been decided this session.
        /// </summary>
        public bool HasDecisionFor(CandidateProfileSO candidate)
        {
            if (candidate == null) return false;
            return decisionResults.ContainsKey(candidate.GetInstanceID());
        }

        /// <summary>
        /// Gets all decision results for the current session.
        /// </summary>
        public IReadOnlyCollection<DecisionResult> GetAllResults()
        {
            return decisionResults.Values;
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Resets all session statistics and decision results.
        /// Call this when starting a new quest/session.
        /// </summary>
        public void ResetSession()
        {
            decisionResults.Clear();
            truePositives = 0;
            trueNegatives = 0;
            falsePositives = 0;
            falseNegatives = 0;

            if (verboseLogging)
            {
                Debug.Log("DecisionController: Session reset");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleQuestStarted(Quest quest)
        {
            // Reset session when new quest starts
            ResetSession();

            if (verboseLogging)
            {
                string clientName = quest?.client?.clientName ?? "Unknown";
                Debug.Log($"DecisionController: New quest started for '{clientName}', session reset");
            }
        }

        private void HandleQuestCleared()
        {
            if (verboseLogging)
            {
                Debug.Log($"DecisionController: Quest cleared, final accuracy: {Accuracy:F1}%");
            }
        }

        private void HandleQueueChanged()
        {
            // If queue was cleared/repopulated, we might need to clean up stale results
            // For now, we keep results until explicit reset or new quest
        }

        #endregion
    }
}
