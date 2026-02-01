using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify QuestManager functionality.
    /// Add this to a GameObject along with QuestManager.
    ///
    /// Testing Instructions:
    /// 1. Add QuestManager and QuestManagerTester to a GameObject in your scene
    /// 2. Enter Play Mode
    /// 3. Click test buttons in the Inspector
    /// 4. Run tests in order: "Test Client Loading" first to verify data loads
    /// 5. Then test quest lifecycle with "Test Start Quest" and "Test Quest Lifecycle"
    /// 6. Check console output for results - look for any warnings or errors
    /// </summary>
    public class QuestManagerTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Specific client to use for quest tests (leave empty to use first available)")]
        public ClientProfileSO testClient;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = true;

        private bool eventFired;
        private string lastEventType;

        /// <summary>
        /// Tests that clients are loaded correctly from Resources
        /// </summary>
        [ContextMenu("Test Client Loading")]
        public void TestClientLoading()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST MANAGER TESTER: Client Loading ===");
            sb.AppendLine();

            var allClients = QuestManager.Instance.GetAllClients();
            sb.AppendLine($"--- LOADED CLIENTS ---");
            sb.AppendLine($"  Total: {allClients?.Length ?? 0}");
            sb.AppendLine();

            if (allClients != null && allClients.Length > 0)
            {
                if (verboseOutput)
                {
                    foreach (var client in allClients)
                    {
                        if (client != null && client.profile != null)
                        {
                            string storyTag = client.isStoryClient ? "[STORY]" : "[SIDE]";
                            sb.AppendLine($"  {storyTag} {client.profile.clientName} (Level {client.suggestedLevel})");
                        }
                    }
                    sb.AppendLine();
                }

                // Test story clients
                var storyClients = QuestManager.Instance.GetStoryClients();
                sb.AppendLine($"  Story Clients: {storyClients.Length}");

                // Test level filtering
                var level1Clients = QuestManager.Instance.GetClientsForLevel(1);
                sb.AppendLine($"  Level 1 Clients: {level1Clients.Length}");

                // Test name lookup
                if (allClients.Length > 0 && allClients[0].profile != null)
                {
                    string testName = allClients[0].profile.clientName;
                    var found = QuestManager.Instance.GetClientByName(testName);
                    sb.AppendLine($"  Lookup '{testName}': {(found != null ? "FOUND" : "NOT FOUND")}");
                }

                // Test random client
                var random = QuestManager.Instance.GetRandomClient();
                sb.AppendLine($"  Random Client: {random?.profile?.clientName ?? "null"}");
            }
            else
            {
                sb.AppendLine("  WARNING: No clients loaded!");
            }

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests starting a quest from a ClientProfileSO
        /// </summary>
        [ContextMenu("Test Start Quest")]
        public void TestStartQuest()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST MANAGER TESTER: Start Quest ===");
            sb.AppendLine();

            // Get a client to test with
            ClientProfileSO client = testClient;
            if (client == null)
            {
                client = QuestManager.Instance.GetRandomClient();
            }

            if (client == null)
            {
                sb.AppendLine("ERROR: No client available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"--- STARTING QUEST ---");
            sb.AppendLine($"  Client: {client.profile.clientName}");
            sb.AppendLine();

            // Subscribe to event
            eventFired = false;
            lastEventType = "";
            QuestManager.Instance.OnQuestStarted += HandleQuestStarted;

            // Start quest
            QuestManager.Instance.StartQuest(client);

            // Unsubscribe
            QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;

            sb.AppendLine($"--- RESULT ---");
            sb.AppendLine($"  HasActiveQuest: {QuestManager.Instance.HasActiveQuest}");
            sb.AppendLine($"  OnQuestStarted fired: {eventFired}");

            if (QuestManager.Instance.CurrentQuest != null)
            {
                var quest = QuestManager.Instance.CurrentQuest;
                sb.AppendLine();
                sb.AppendLine($"--- CURRENT QUEST ---");
                sb.AppendLine($"  Client: {quest.client?.clientName ?? "null"}");
                sb.AppendLine($"  Introduction: {TruncateString(quest.introductionText, 60)}");
                sb.AppendLine($"  Has Criteria: {quest.matchCriteria != null}");

                if (verboseOutput && quest.matchCriteria != null)
                {
                    var mc = quest.matchCriteria;
                    sb.AppendLine($"  Age Range: {mc.minAge}-{mc.maxAge}");
                    sb.AppendLine($"  Requirements: {mc.traitRequirements?.Length ?? 0}");
                    sb.AppendLine($"  Max Red Flags: {mc.maxRedFlags}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests the full quest lifecycle: start, complete, clear
        /// </summary>
        [ContextMenu("Test Quest Lifecycle")]
        public void TestQuestLifecycle()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST MANAGER TESTER: Quest Lifecycle ===");
            sb.AppendLine();

            // Get a client
            ClientProfileSO client = testClient ?? QuestManager.Instance.GetRandomClient();
            if (client == null)
            {
                sb.AppendLine("ERROR: No client available");
                Debug.Log(sb.ToString());
                return;
            }

            // Subscribe to all events
            int startedCount = 0;
            int completedCount = 0;
            int clearedCount = 0;

            QuestManager.Instance.OnQuestStarted += (q) => startedCount++;
            QuestManager.Instance.OnQuestCompleted += (q) => completedCount++;
            QuestManager.Instance.OnQuestCleared += () => clearedCount++;

            // Test 1: Start quest
            sb.AppendLine("--- STEP 1: Start Quest ---");
            QuestManager.Instance.StartQuest(client);
            sb.AppendLine($"  HasActiveQuest: {QuestManager.Instance.HasActiveQuest}");
            sb.AppendLine($"  Events - Started: {startedCount}, Completed: {completedCount}, Cleared: {clearedCount}");
            sb.AppendLine();

            // Test 2: Complete quest
            sb.AppendLine("--- STEP 2: Complete Quest ---");
            QuestManager.Instance.CompleteQuest();
            sb.AppendLine($"  HasActiveQuest: {QuestManager.Instance.HasActiveQuest}");
            sb.AppendLine($"  Events - Started: {startedCount}, Completed: {completedCount}, Cleared: {clearedCount}");
            sb.AppendLine();

            // Test 3: Start another quest
            sb.AppendLine("--- STEP 3: Start Another Quest ---");
            QuestManager.Instance.StartQuest(client);
            sb.AppendLine($"  HasActiveQuest: {QuestManager.Instance.HasActiveQuest}");
            sb.AppendLine($"  Events - Started: {startedCount}, Completed: {completedCount}, Cleared: {clearedCount}");
            sb.AppendLine();

            // Test 4: Clear quest (abandon)
            sb.AppendLine("--- STEP 4: Clear Quest (abandon) ---");
            QuestManager.Instance.ClearQuest();
            sb.AppendLine($"  HasActiveQuest: {QuestManager.Instance.HasActiveQuest}");
            sb.AppendLine($"  Events - Started: {startedCount}, Completed: {completedCount}, Cleared: {clearedCount}");
            sb.AppendLine();

            // Verify expected counts
            sb.AppendLine("--- VERIFICATION ---");
            bool startedOk = startedCount == 2;
            bool completedOk = completedCount == 1;
            bool clearedOk = clearedCount == 1;

            sb.AppendLine($"  Started events (expected 2): {startedCount} {(startedOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Completed events (expected 1): {completedCount} {(completedOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Cleared events (expected 1): {clearedCount} {(clearedOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests starting a quest and populating the match queue together
        /// </summary>
        [ContextMenu("Test Quest with Queue Population")]
        public void TestQuestWithQueuePopulation()
        {
            if (!ValidateManager()) return;

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("QuestManagerTester: MatchQueueManager not found! Add it to the scene.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST MANAGER TESTER: Quest with Queue ===");
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
            sb.AppendLine($"--- STARTING QUEST ---");
            sb.AppendLine($"  Client: {client.profile.clientName}");
            QuestManager.Instance.StartQuest(client);
            sb.AppendLine();

            // Populate queue for this quest
            sb.AppendLine($"--- POPULATING QUEUE ---");
            MatchQueueManager.Instance.PopulateForQuest(QuestManager.Instance.CurrentQuest, 5);
            sb.AppendLine($"  Queue Size: {MatchQueueManager.Instance.Count}");
            sb.AppendLine();

            // Show candidates with match status
            if (verboseOutput)
            {
                sb.AppendLine($"--- CANDIDATES ---");
                var queue = MatchQueueManager.Instance.Queue;
                var criteria = QuestManager.Instance.CurrentQuest.matchCriteria;

                foreach (var candidate in queue)
                {
                    var result = Maskhot.Matching.MatchEvaluator.Evaluate(candidate, criteria);
                    string matchTag = result.IsMatch ? "[MATCH]" : "[NO]";
                    sb.AppendLine($"  {matchTag} {candidate.profile.characterName} (score: {result.Score:F2})");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs the current quest state
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== QUEST MANAGER STATE ===");
            sb.AppendLine();

            sb.AppendLine($"  HasActiveQuest: {QuestManager.Instance.HasActiveQuest}");
            sb.AppendLine($"  ClientCount: {QuestManager.Instance.ClientCount}");
            sb.AppendLine();

            if (QuestManager.Instance.CurrentQuest != null)
            {
                var quest = QuestManager.Instance.CurrentQuest;
                sb.AppendLine($"--- CURRENT QUEST ---");
                sb.AppendLine($"  Client: {quest.client?.clientName ?? "null"}");
                sb.AppendLine($"  Introduction: {TruncateString(quest.introductionText, 80)}");

                if (quest.matchCriteria != null)
                {
                    var mc = quest.matchCriteria;
                    sb.AppendLine();
                    sb.AppendLine($"--- MATCH CRITERIA ---");
                    sb.AppendLine($"  Age: {mc.minAge}-{mc.maxAge}");
                    sb.AppendLine($"  Requirements: {mc.traitRequirements?.Length ?? 0}");
                    sb.AppendLine($"  Red Flag Tolerance: {mc.maxRedFlags}");
                    sb.AppendLine($"  Green Flag Minimum: {mc.minGreenFlags}");
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
        /// Clears the current quest
        /// </summary>
        [ContextMenu("Clear Current Quest")]
        public void ClearCurrentQuest()
        {
            if (!ValidateManager()) return;

            QuestManager.Instance.ClearQuest();
            Debug.Log("QuestManagerTester: Quest cleared");
        }

        private void HandleQuestStarted(Quest quest)
        {
            eventFired = true;
            lastEventType = "Started";
        }

        private bool ValidateManager()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("QuestManagerTester: QuestManager not found! Add it to the scene.");
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
