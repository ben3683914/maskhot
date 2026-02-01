using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify QuestController functionality.
    /// Add this to a GameObject along with QuestManager and QuestController.
    ///
    /// Testing Instructions:
    /// 1. Add QuestManager, QuestController, and QuestControllerTester to a GameObject
    /// 2. Enter Play Mode
    /// 3. Click test buttons in the Inspector
    /// 4. Run "Test Event Subscription" first to verify controller reacts to QuestManager
    /// 5. Then run "Test Cached Data" to verify hints are cached properly
    /// 6. Check console output for results
    /// </summary>
    public class QuestControllerTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Specific client to use for tests (leave empty to use random)")]
        public ClientProfileSO testClient;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = true;

        private int questChangedCount;

        /// <summary>
        /// Tests that QuestController receives events from QuestManager
        /// </summary>
        [ContextMenu("Test Event Subscription")]
        public void TestEventSubscription()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST CONTROLLER TESTER: Event Subscription ===");
            sb.AppendLine();

            // Get a client
            ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
            if (client == null)
            {
                sb.AppendLine("ERROR: No client available");
                Debug.Log(sb.ToString());
                return;
            }

            // Subscribe to controller event
            questChangedCount = 0;
            QuestController.Instance.OnQuestChanged += HandleQuestChanged;

            sb.AppendLine("--- STEP 1: Start Quest ---");
            QuestManager.Instance.StartQuest(client);
            sb.AppendLine($"  HasActiveQuest: {QuestController.Instance.HasActiveQuest}");
            sb.AppendLine($"  OnQuestChanged fired: {questChangedCount}");
            sb.AppendLine();

            sb.AppendLine("--- STEP 2: Complete Quest ---");
            QuestManager.Instance.CompleteQuest();
            sb.AppendLine($"  HasActiveQuest: {QuestController.Instance.HasActiveQuest}");
            sb.AppendLine($"  OnQuestChanged fired: {questChangedCount}");
            sb.AppendLine();

            sb.AppendLine("--- STEP 3: Start and Clear ---");
            QuestManager.Instance.StartQuest(client);
            QuestManager.Instance.ClearQuest();
            sb.AppendLine($"  HasActiveQuest: {QuestController.Instance.HasActiveQuest}");
            sb.AppendLine($"  OnQuestChanged fired: {questChangedCount}");
            sb.AppendLine();

            // Unsubscribe
            QuestController.Instance.OnQuestChanged -= HandleQuestChanged;

            sb.AppendLine("--- VERIFICATION ---");
            bool eventsOk = questChangedCount == 4; // start, complete, start, clear
            sb.AppendLine($"  Total events (expected 4): {questChangedCount} {(eventsOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests that requirements are cached with consistent hints
        /// </summary>
        [ContextMenu("Test Cached Data")]
        public void TestCachedData()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST CONTROLLER TESTER: Cached Data ===");
            sb.AppendLine();

            // Get a client
            ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
            if (client == null)
            {
                sb.AppendLine("ERROR: No client available");
                Debug.Log(sb.ToString());
                return;
            }

            // Start quest
            QuestManager.Instance.StartQuest(client);

            sb.AppendLine($"--- CLIENT INFO ---");
            sb.AppendLine($"  Name: {QuestController.Instance.ClientName}");
            sb.AppendLine($"  Introduction: {TruncateString(QuestController.Instance.ClientIntroduction, 60)}");
            sb.AppendLine();

            sb.AppendLine($"--- BASIC CRITERIA ---");
            sb.AppendLine($"  Age Range: {QuestController.Instance.MinAge}-{QuestController.Instance.MaxAge}");
            sb.AppendLine($"  Max Red Flags: {QuestController.Instance.MaxRedFlags}");
            sb.AppendLine($"  Min Green Flags: {QuestController.Instance.MinGreenFlags}");

            var genders = QuestController.Instance.AcceptableGenders;
            if (genders.Length > 0)
            {
                sb.AppendLine($"  Acceptable Genders: {string.Join(", ", genders)}");
            }
            sb.AppendLine();

            sb.AppendLine($"--- REQUIREMENTS ({QuestController.Instance.Requirements.Length}) ---");
            foreach (var req in QuestController.Instance.Requirements)
            {
                string levelTag = req.level.ToString().ToUpper();
                sb.AppendLine($"  [{levelTag}] \"{req.hint}\"");
            }
            sb.AppendLine();

            sb.AppendLine($"--- DEALBREAKERS ({QuestController.Instance.Dealbreakers.Length}) ---");
            if (QuestController.Instance.Dealbreakers.Length > 0)
            {
                foreach (var db in QuestController.Instance.Dealbreakers)
                {
                    sb.AppendLine($"  - {db}");
                }
            }
            else
            {
                sb.AppendLine("  (none)");
            }
            sb.AppendLine();

            // Verify hints are consistent
            sb.AppendLine("--- CACHE CONSISTENCY TEST ---");
            var firstRead = QuestController.Instance.Requirements;
            var secondRead = QuestController.Instance.Requirements;

            bool consistent = true;
            if (firstRead.Length != secondRead.Length)
            {
                consistent = false;
            }
            else
            {
                for (int i = 0; i < firstRead.Length; i++)
                {
                    if (firstRead[i].hint != secondRead[i].hint)
                    {
                        consistent = false;
                        break;
                    }
                }
            }
            sb.AppendLine($"  Hints consistent across reads: {(consistent ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests that cache is cleared when quest ends
        /// </summary>
        [ContextMenu("Test Cache Clearing")]
        public void TestCacheClearing()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST CONTROLLER TESTER: Cache Clearing ===");
            sb.AppendLine();

            // Get a client
            ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
            if (client == null)
            {
                sb.AppendLine("ERROR: No client available");
                Debug.Log(sb.ToString());
                return;
            }

            // Start quest
            QuestManager.Instance.StartQuest(client);

            sb.AppendLine("--- BEFORE CLEAR ---");
            sb.AppendLine($"  HasActiveQuest: {QuestController.Instance.HasActiveQuest}");
            sb.AppendLine($"  ClientName: {QuestController.Instance.ClientName}");
            sb.AppendLine($"  Requirements: {QuestController.Instance.Requirements.Length}");
            sb.AppendLine($"  Dealbreakers: {QuestController.Instance.Dealbreakers.Length}");
            sb.AppendLine();

            // Clear quest
            QuestManager.Instance.ClearQuest();

            sb.AppendLine("--- AFTER CLEAR ---");
            sb.AppendLine($"  HasActiveQuest: {QuestController.Instance.HasActiveQuest}");
            sb.AppendLine($"  ClientName: \"{QuestController.Instance.ClientName}\"");
            sb.AppendLine($"  Requirements: {QuestController.Instance.Requirements.Length}");
            sb.AppendLine($"  Dealbreakers: {QuestController.Instance.Dealbreakers.Length}");
            sb.AppendLine();

            sb.AppendLine("--- VERIFICATION ---");
            bool hasActiveOk = !QuestController.Instance.HasActiveQuest;
            bool nameOk = string.IsNullOrEmpty(QuestController.Instance.ClientName);
            bool reqsOk = QuestController.Instance.Requirements.Length == 0;
            bool dbOk = QuestController.Instance.Dealbreakers.Length == 0;

            sb.AppendLine($"  No active quest: {(hasActiveOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Name empty: {(nameOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Requirements cleared: {(reqsOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Dealbreakers cleared: {(dbOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs the current state of QuestController
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST CONTROLLER STATE ===");
            sb.AppendLine();

            sb.AppendLine($"  HasActiveQuest: {QuestController.Instance.HasActiveQuest}");

            if (QuestController.Instance.HasActiveQuest)
            {
                sb.AppendLine();
                sb.AppendLine($"--- CLIENT ---");
                sb.AppendLine($"  Name: {QuestController.Instance.ClientName}");
                sb.AppendLine($"  Introduction: {TruncateString(QuestController.Instance.ClientIntroduction, 80)}");
                sb.AppendLine();

                sb.AppendLine($"--- CRITERIA ---");
                sb.AppendLine($"  Age: {QuestController.Instance.MinAge}-{QuestController.Instance.MaxAge}");
                sb.AppendLine($"  Red Flags: max {QuestController.Instance.MaxRedFlags}");
                sb.AppendLine($"  Green Flags: min {QuestController.Instance.MinGreenFlags}");
                sb.AppendLine();

                sb.AppendLine($"--- REQUIREMENTS ({QuestController.Instance.Requirements.Length}) ---");
                foreach (var req in QuestController.Instance.Requirements)
                {
                    sb.AppendLine($"  [{req.level}] \"{req.hint}\"");
                }

                if (QuestController.Instance.Dealbreakers.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"--- DEALBREAKERS ---");
                    sb.AppendLine($"  {string.Join(", ", QuestController.Instance.Dealbreakers)}");
                }
            }
            else
            {
                sb.AppendLine("  (no active quest)");
            }

            sb.AppendLine();
            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Starts a test quest via QuestManager
        /// </summary>
        [ContextMenu("Start Test Quest")]
        public void StartTestQuest()
        {
            if (!ValidateControllers()) return;

            ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
            if (client == null)
            {
                Debug.LogError("QuestControllerTester: No client available");
                return;
            }

            QuestManager.Instance.StartQuest(client);
            Debug.Log($"QuestControllerTester: Started quest for '{client.profile.clientName}'");
        }

        /// <summary>
        /// Clears the current quest via QuestManager
        /// </summary>
        [ContextMenu("Clear Quest")]
        public void ClearQuest()
        {
            if (!ValidateControllers()) return;

            QuestManager.Instance.ClearQuest();
            Debug.Log("QuestControllerTester: Quest cleared");
        }

        private void HandleQuestChanged()
        {
            questChangedCount++;
        }

        private bool ValidateControllers()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("QuestControllerTester: QuestManager not found!");
                return false;
            }

            if (QuestController.Instance == null)
            {
                Debug.LogError("QuestControllerTester: QuestController not found!");
                return false;
            }

            return true;
        }

        private string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return "(empty)";
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength) + "...";
        }
    }
}
