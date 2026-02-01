using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify DecisionController functionality.
    ///
    /// Setup:
    /// 1. Attach to a GameObject alongside ProfileManager, MatchQueueManager,
    ///    MatchListController, QuestManager, and DecisionController
    /// 2. Optionally assign a test client for quest-based tests
    /// 3. Enter Play Mode
    ///
    /// How to Run: Click the buttons in the Inspector (enabled during Play Mode)
    ///
    /// What to Verify:
    /// - Decisions are recorded correctly (accept/reject)
    /// - Correctness evaluation works (comparing to quest criteria)
    /// - Statistics update properly (TP, TN, FP, FN)
    /// - Events fire on decision and completion
    /// - Auto-advance works when enabled
    /// - Session resets on new quest
    /// </summary>
    public class DecisionControllerTester : MonoBehaviour
    {
        [Header("Test Targets")]
        [Tooltip("Client to use for quest-based testing")]
        public ClientProfileSO testClient;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        private bool decisionEventReceived = false;
        private DecisionResult lastDecisionResult;
        private bool completionEventReceived = false;

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
            // Re-subscribe in case controller wasn't ready
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (DecisionController.Instance != null)
            {
                DecisionController.Instance.OnDecisionResult += HandleDecisionResult;
                DecisionController.Instance.OnAllDecisionsComplete += HandleAllDecisionsComplete;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (DecisionController.Instance != null)
            {
                DecisionController.Instance.OnDecisionResult -= HandleDecisionResult;
                DecisionController.Instance.OnAllDecisionsComplete -= HandleAllDecisionsComplete;
            }
        }

        private void HandleDecisionResult(DecisionResult result)
        {
            decisionEventReceived = true;
            lastDecisionResult = result;

            if (verboseOutput)
            {
                string name = result.Candidate?.profile.characterName ?? "null";
                Debug.Log($"DecisionControllerTester: OnDecisionResult fired - {name} ({result.Decision}, {result.Outcome})");
            }
        }

        private void HandleAllDecisionsComplete()
        {
            completionEventReceived = true;

            if (verboseOutput)
            {
                Debug.Log("DecisionControllerTester: OnAllDecisionsComplete fired");
            }
        }

        /// <summary>
        /// Tests accepting the current candidate
        /// </summary>
        public void TestAcceptCurrent()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER TESTER: Accept Current ===");
            sb.AppendLine();

            EnsureTestSetup();

            // Select first candidate
            MatchListController.Instance.SelectFirst();
            var candidate = MatchListController.Instance.CurrentCandidate;

            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate selected");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"  Selected: {candidate.profile.characterName}");
            sb.AppendLine();

            // Make decision
            decisionEventReceived = false;
            var result = DecisionController.Instance.AcceptCurrent();

            sb.AppendLine("--- DECISION RESULT ---");
            if (result.HasValue)
            {
                var r = result.Value;
                sb.AppendLine($"  Decision: {r.Decision}");
                sb.AppendLine($"  Was Actual Match: {r.WasActualMatch}");
                sb.AppendLine($"  Is Correct: {r.IsCorrect}");
                sb.AppendLine($"  Outcome: {r.Outcome}");
                sb.AppendLine($"  Match Score: {r.MatchScore:F1}");
                sb.AppendLine($"  Match Reason: {r.MatchReason}");
                sb.AppendLine();
                sb.AppendLine($"  Event fired: {decisionEventReceived}");
            }
            else
            {
                sb.AppendLine("  Result: null (decision failed)");
            }
            sb.AppendLine();

            LogCurrentStats(sb);

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests rejecting the current candidate
        /// </summary>
        public void TestRejectCurrent()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER TESTER: Reject Current ===");
            sb.AppendLine();

            EnsureTestSetup();

            // Select first pending candidate
            MatchListController.Instance.SelectNextPending();
            var candidate = MatchListController.Instance.CurrentCandidate;

            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No pending candidate available");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"  Selected: {candidate.profile.characterName}");
            sb.AppendLine();

            // Make decision
            decisionEventReceived = false;
            var result = DecisionController.Instance.RejectCurrent();

            sb.AppendLine("--- DECISION RESULT ---");
            if (result.HasValue)
            {
                var r = result.Value;
                sb.AppendLine($"  Decision: {r.Decision}");
                sb.AppendLine($"  Was Actual Match: {r.WasActualMatch}");
                sb.AppendLine($"  Is Correct: {r.IsCorrect}");
                sb.AppendLine($"  Outcome: {r.Outcome}");
                sb.AppendLine($"  Match Score: {r.MatchScore:F1}");
                sb.AppendLine($"  Match Reason: {r.MatchReason}");
                sb.AppendLine();
                sb.AppendLine($"  Event fired: {decisionEventReceived}");
            }
            else
            {
                sb.AppendLine("  Result: null (decision failed)");
            }
            sb.AppendLine();

            LogCurrentStats(sb);

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests deciding on all candidates and verifies completion event
        /// </summary>
        public void TestDecideAll()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER TESTER: Decide All ===");
            sb.AppendLine();

            EnsureTestSetup();

            completionEventReceived = false;
            int decisionCount = 0;

            sb.AppendLine("--- MAKING DECISIONS ---");

            // Decide on all pending candidates (alternate accept/reject)
            while (MatchQueueManager.Instance.PendingCount > 0)
            {
                MatchListController.Instance.SelectNextPending();
                var candidate = MatchListController.Instance.CurrentCandidate;

                if (candidate == null) break;

                // Alternate between accept and reject for variety
                DecisionResult? result;
                if (decisionCount % 2 == 0)
                {
                    result = DecisionController.Instance.AcceptCurrent();
                }
                else
                {
                    result = DecisionController.Instance.RejectCurrent();
                }

                if (result.HasValue)
                {
                    var r = result.Value;
                    string correctStr = r.IsCorrect ? "CORRECT" : "WRONG";
                    sb.AppendLine($"  [{decisionCount + 1}] {candidate.profile.characterName}: {r.Decision} ({correctStr})");
                    decisionCount++;
                }
            }
            sb.AppendLine();

            sb.AppendLine("--- COMPLETION ---");
            sb.AppendLine($"  Total decisions: {decisionCount}");
            sb.AppendLine($"  AllDecided: {DecisionController.Instance.AllDecided}");
            sb.AppendLine($"  Completion event fired: {completionEventReceived}");
            sb.AppendLine();

            LogCurrentStats(sb);

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests that statistics are calculated correctly
        /// </summary>
        public void TestStatistics()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER TESTER: Statistics ===");
            sb.AppendLine();

            LogCurrentStats(sb);

            // Also show all results
            var results = DecisionController.Instance.GetAllResults();
            if (results.Count > 0)
            {
                sb.AppendLine("--- ALL DECISION RESULTS ---");
                foreach (var r in results)
                {
                    string correctStr = r.IsCorrect ? "CORRECT" : "WRONG";
                    sb.AppendLine($"  {r.Candidate?.profile.characterName}: {r.Decision} ({r.Outcome}, {correctStr})");
                    sb.AppendLine($"    Match: {r.WasActualMatch}, Score: {r.MatchScore:F1}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests session reset
        /// </summary>
        public void TestSessionReset()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER TESTER: Session Reset ===");
            sb.AppendLine();

            sb.AppendLine("--- BEFORE RESET ---");
            LogCurrentStats(sb);

            DecisionController.Instance.ResetSession();

            sb.AppendLine("--- AFTER RESET ---");
            LogCurrentStats(sb);

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests auto-advance functionality
        /// </summary>
        public void TestAutoAdvance()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER TESTER: Auto-Advance ===");
            sb.AppendLine();

            // Force fresh setup for this test (need pending candidates)
            ForceNewTestSetup();

            bool wasAutoAdvance = DecisionController.Instance.autoAdvance;
            DecisionController.Instance.autoAdvance = true;

            // Select first pending candidate
            MatchListController.Instance.SelectFirst();
            var firstCandidate = MatchListController.Instance.CurrentCandidate;
            int firstIndex = MatchListController.Instance.CurrentIndex;

            sb.AppendLine($"  Queue size: {MatchQueueManager.Instance.Count}");
            sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
            sb.AppendLine($"  Before decision: {firstCandidate?.profile.characterName} (index {firstIndex})");

            var result = DecisionController.Instance.AcceptCurrent();

            var afterCandidate = MatchListController.Instance.CurrentCandidate;
            int afterIndex = MatchListController.Instance.CurrentIndex;

            sb.AppendLine($"  After decision: {afterCandidate?.profile.characterName} (index {afterIndex})");

            bool didAdvance = afterCandidate != firstCandidate;
            sb.AppendLine($"  Auto-advanced: {didAdvance}");

            if (!didAdvance && MatchQueueManager.Instance.PendingCount > 0)
            {
                sb.AppendLine($"  WARNING: Did not advance but {MatchQueueManager.Instance.PendingCount} candidates still pending");
            }
            else if (!didAdvance && MatchQueueManager.Instance.PendingCount == 0)
            {
                sb.AppendLine($"  (No pending candidates to advance to - this is expected)");
            }
            sb.AppendLine();

            DecisionController.Instance.autoAdvance = wasAutoAdvance;

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Forces a completely fresh test setup (clears and repopulates everything)
        /// </summary>
        private void ForceNewTestSetup()
        {
            // Clear everything
            MatchQueueManager.Instance.ClearQueue();
            DecisionController.Instance.ResetSession();
            MatchListController.Instance.ClearSelection();

            // Start fresh quest
            ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
            if (client != null)
            {
                QuestManager.Instance.StartQuest(client);
                MatchQueueManager.Instance.PopulateForQuest(QuestManager.Instance.CurrentQuest, 5);
                Debug.Log($"DecisionControllerTester: Fresh setup with '{client.profile.clientName}'");
            }
        }

        /// <summary>
        /// Logs current controller state
        /// </summary>
        public void LogCurrentState()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== DECISION CONTROLLER STATE ===");
            sb.AppendLine();

            sb.AppendLine($"  Auto-Advance: {DecisionController.Instance.autoAdvance}");
            sb.AppendLine($"  All Decided: {DecisionController.Instance.AllDecided}");
            sb.AppendLine();

            LogCurrentStats(sb);

            // Queue info
            if (MatchQueueManager.Instance != null)
            {
                sb.AppendLine("--- QUEUE STATUS ---");
                sb.AppendLine($"  Total: {MatchQueueManager.Instance.Count}");
                sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
                sb.AppendLine($"  Accepted: {MatchQueueManager.Instance.AcceptedCount}");
                sb.AppendLine($"  Rejected: {MatchQueueManager.Instance.RejectedCount}");
                sb.AppendLine();
            }

            // Quest info
            if (QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest)
            {
                sb.AppendLine("--- ACTIVE QUEST ---");
                sb.AppendLine($"  Client: {QuestManager.Instance.CurrentQuest.client?.clientName ?? "Unknown"}");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("--- NO ACTIVE QUEST ---");
                sb.AppendLine();
            }

            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Starts a test quest using assigned client
        /// </summary>
        public void StartTestQuest()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("DecisionControllerTester: QuestManager not found!");
                return;
            }

            ClientProfileSO client = testClient;
            if (client == null)
            {
                client = QuestManager.Instance.GetRandomClient();
            }

            if (client == null)
            {
                Debug.LogError("DecisionControllerTester: No client available for testing");
                return;
            }

            QuestManager.Instance.StartQuest(client);
            MatchQueueManager.Instance.PopulateForQuest(QuestManager.Instance.CurrentQuest, 5);
            MatchListController.Instance.SelectFirst();

            Debug.Log($"DecisionControllerTester: Started quest for '{client.profile.clientName}' with 5 candidates");
        }

        /// <summary>
        /// Clears current quest and queue
        /// </summary>
        public void ClearTestSetup()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ClearQuest();
            }

            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.ClearQueue();
            }

            if (MatchListController.Instance != null)
            {
                MatchListController.Instance.ClearSelection();
            }

            Debug.Log("DecisionControllerTester: Cleared quest and queue");
        }

        private void EnsureTestSetup()
        {
            // Ensure we have a quest
            if (QuestManager.Instance != null && !QuestManager.Instance.HasActiveQuest)
            {
                ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
                if (client != null)
                {
                    QuestManager.Instance.StartQuest(client);
                    Debug.Log($"DecisionControllerTester: Auto-started quest for '{client.profile.clientName}'");
                }
            }

            // Ensure we have a populated queue
            if (MatchQueueManager.Instance != null && MatchQueueManager.Instance.Count == 0)
            {
                if (QuestManager.Instance?.CurrentQuest != null)
                {
                    MatchQueueManager.Instance.PopulateForQuest(QuestManager.Instance.CurrentQuest, 5);
                }
                else
                {
                    MatchQueueManager.Instance.PopulateRandom(5);
                }
                Debug.Log("DecisionControllerTester: Auto-populated queue");
            }
        }

        private void LogCurrentStats(StringBuilder sb)
        {
            sb.AppendLine("--- SESSION STATISTICS ---");
            sb.AppendLine($"  Correct Accepts (TP): {DecisionController.Instance.CorrectAccepts}");
            sb.AppendLine($"  Correct Rejects (TN): {DecisionController.Instance.CorrectRejects}");
            sb.AppendLine($"  False Accepts (FP): {DecisionController.Instance.FalseAccepts}");
            sb.AppendLine($"  False Rejects (FN): {DecisionController.Instance.FalseRejects}");
            sb.AppendLine($"  Total Correct: {DecisionController.Instance.TotalCorrect}");
            sb.AppendLine($"  Total Incorrect: {DecisionController.Instance.TotalIncorrect}");
            sb.AppendLine($"  Accuracy: {DecisionController.Instance.Accuracy:F1}%");
            sb.AppendLine();
        }

        private bool ValidateControllers()
        {
            if (ProfileManager.Instance == null)
            {
                Debug.LogError("DecisionControllerTester: ProfileManager not found!");
                return false;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("DecisionControllerTester: MatchQueueManager not found!");
                return false;
            }

            if (MatchListController.Instance == null)
            {
                Debug.LogError("DecisionControllerTester: MatchListController not found!");
                return false;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogError("DecisionControllerTester: QuestManager not found!");
                return false;
            }

            if (DecisionController.Instance == null)
            {
                Debug.LogError("DecisionControllerTester: DecisionController not found!");
                return false;
            }

            return true;
        }
    }
}
