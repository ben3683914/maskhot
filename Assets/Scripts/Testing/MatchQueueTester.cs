using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify MatchQueueManager functionality.
    /// Add this to a GameObject along with ProfileManager and MatchQueueManager.
    /// Use the Context Menu to run tests.
    /// </summary>
    public class MatchQueueTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Number of candidates to populate in test queue")]
        public int testQueueSize = 5;

        [Tooltip("Client to use for quest-based population tests")]
        public ClientProfileSO testClient;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        /// <summary>
        /// Tests random queue population
        /// </summary>
        [ContextMenu("Test Random Population")]
        public void TestRandomPopulation()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH QUEUE TESTER: Random Population ===");
            sb.AppendLine();

            MatchQueueManager.Instance.PopulateRandom(testQueueSize);

            sb.AppendLine($"--- QUEUE STATUS ---");
            sb.AppendLine($"  Count: {MatchQueueManager.Instance.Count}");
            sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
            sb.AppendLine($"  Accepted: {MatchQueueManager.Instance.AcceptedCount}");
            sb.AppendLine($"  Rejected: {MatchQueueManager.Instance.RejectedCount}");
            sb.AppendLine();

            if (verboseOutput)
            {
                sb.AppendLine("--- CANDIDATES IN QUEUE ---");
                var queue = MatchQueueManager.Instance.Queue;
                for (int i = 0; i < queue.Count; i++)
                {
                    var candidate = queue[i];
                    var decision = MatchQueueManager.Instance.GetDecision(candidate);
                    sb.AppendLine($"  [{i}] {candidate.profile.characterName} - {decision}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests quest-based queue population
        /// </summary>
        [ContextMenu("Test Quest Population")]
        public void TestQuestPopulation()
        {
            if (!ValidateManagers()) return;

            if (testClient == null)
            {
                Debug.LogWarning("MatchQueueTester: No testClient assigned, using random population");
                TestRandomPopulation();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH QUEUE TESTER: Quest Population ===");
            sb.AppendLine();

            sb.AppendLine($"--- CLIENT ---");
            sb.AppendLine($"  Name: {testClient.profile.clientName}");
            sb.AppendLine();

            // Create quest from client
            var quest = Quest.FromClientProfileSO(testClient, testClient.introduction, testClient.matchCriteria);

            MatchQueueManager.Instance.PopulateForQuest(quest, testQueueSize);

            sb.AppendLine($"--- QUEUE STATUS ---");
            sb.AppendLine($"  Count: {MatchQueueManager.Instance.Count}");
            sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
            sb.AppendLine();

            if (verboseOutput)
            {
                sb.AppendLine("--- CANDIDATES WITH MATCH STATUS ---");
                var queue = MatchQueueManager.Instance.Queue;
                for (int i = 0; i < queue.Count; i++)
                {
                    var candidate = queue[i];
                    var result = Maskhot.Matching.MatchEvaluator.Evaluate(candidate, quest.matchCriteria);
                    string matchStatus = result.IsMatch ? "MATCH" : "NO MATCH";
                    sb.AppendLine($"  [{i}] {candidate.profile.characterName} - {matchStatus} (score: {result.Score:F2})");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests decision tracking
        /// </summary>
        [ContextMenu("Test Decision Tracking")]
        public void TestDecisionTracking()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH QUEUE TESTER: Decision Tracking ===");
            sb.AppendLine();

            // Populate if empty
            if (MatchQueueManager.Instance.Count == 0)
            {
                MatchQueueManager.Instance.PopulateRandom(testQueueSize);
                sb.AppendLine($"(Populated queue with {testQueueSize} candidates)");
                sb.AppendLine();
            }

            var queue = MatchQueueManager.Instance.Queue;
            if (queue.Count < 3)
            {
                sb.AppendLine("ERROR: Need at least 3 candidates for decision test");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine("--- INITIAL STATE ---");
            sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
            sb.AppendLine($"  Accepted: {MatchQueueManager.Instance.AcceptedCount}");
            sb.AppendLine($"  Rejected: {MatchQueueManager.Instance.RejectedCount}");
            sb.AppendLine();

            // Accept first, reject second, leave third pending
            var first = queue[0];
            var second = queue[1];
            var third = queue[2];

            MatchQueueManager.Instance.Accept(first);
            sb.AppendLine($"  Accepted: {first.profile.characterName}");

            MatchQueueManager.Instance.Reject(second);
            sb.AppendLine($"  Rejected: {second.profile.characterName}");

            sb.AppendLine($"  Left pending: {third.profile.characterName}");
            sb.AppendLine();

            sb.AppendLine("--- AFTER DECISIONS ---");
            sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
            sb.AppendLine($"  Accepted: {MatchQueueManager.Instance.AcceptedCount}");
            sb.AppendLine($"  Rejected: {MatchQueueManager.Instance.RejectedCount}");
            sb.AppendLine($"  All Decided: {MatchQueueManager.Instance.AllDecided}");
            sb.AppendLine();

            // Verify individual decisions
            sb.AppendLine("--- DECISION VERIFICATION ---");
            sb.AppendLine($"  {first.profile.characterName}: {MatchQueueManager.Instance.GetDecision(first)}");
            sb.AppendLine($"  {second.profile.characterName}: {MatchQueueManager.Instance.GetDecision(second)}");
            sb.AppendLine($"  {third.profile.characterName}: {MatchQueueManager.Instance.GetDecision(third)}");
            sb.AppendLine();

            // Test reset
            MatchQueueManager.Instance.ResetDecision(first);
            sb.AppendLine($"  Reset {first.profile.characterName}: {MatchQueueManager.Instance.GetDecision(first)}");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests query methods
        /// </summary>
        [ContextMenu("Test Query Methods")]
        public void TestQueryMethods()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH QUEUE TESTER: Query Methods ===");
            sb.AppendLine();

            // Populate if empty
            if (MatchQueueManager.Instance.Count == 0)
            {
                MatchQueueManager.Instance.PopulateRandom(testQueueSize);
                sb.AppendLine($"(Populated queue with {testQueueSize} candidates)");
                sb.AppendLine();
            }

            var queue = MatchQueueManager.Instance.Queue;

            sb.AppendLine("--- GetCandidateAt ---");
            for (int i = 0; i < Mathf.Min(3, queue.Count); i++)
            {
                var candidate = MatchQueueManager.Instance.GetCandidateAt(i);
                sb.AppendLine($"  [{i}]: {candidate?.profile.characterName ?? "null"}");
            }
            sb.AppendLine();

            sb.AppendLine("--- GetIndexOf ---");
            if (queue.Count > 0)
            {
                var testCandidate = queue[0];
                int index = MatchQueueManager.Instance.GetIndexOf(testCandidate);
                sb.AppendLine($"  {testCandidate.profile.characterName} is at index {index}");
            }
            sb.AppendLine();

            sb.AppendLine("--- IsInQueue ---");
            if (queue.Count > 0)
            {
                var inQueue = queue[0];
                sb.AppendLine($"  {inQueue.profile.characterName}: {MatchQueueManager.Instance.IsInQueue(inQueue)}");
            }
            sb.AppendLine($"  null: {MatchQueueManager.Instance.IsInQueue(null)}");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Clears the queue
        /// </summary>
        [ContextMenu("Clear Queue")]
        public void ClearQueue()
        {
            if (!ValidateManagers()) return;

            MatchQueueManager.Instance.ClearQueue();
            Debug.Log("MatchQueueTester: Queue cleared");
        }

        /// <summary>
        /// Logs current queue state
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH QUEUE STATE ===");
            sb.AppendLine();

            sb.AppendLine($"  HasCandidates: {MatchQueueManager.Instance.HasCandidates}");
            sb.AppendLine($"  Count: {MatchQueueManager.Instance.Count}");
            sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount}");
            sb.AppendLine($"  Accepted: {MatchQueueManager.Instance.AcceptedCount}");
            sb.AppendLine($"  Rejected: {MatchQueueManager.Instance.RejectedCount}");
            sb.AppendLine($"  AllDecided: {MatchQueueManager.Instance.AllDecided}");
            sb.AppendLine();

            var queue = MatchQueueManager.Instance.Queue;
            if (queue.Count > 0)
            {
                sb.AppendLine("--- QUEUE ---");
                for (int i = 0; i < queue.Count; i++)
                {
                    var candidate = queue[i];
                    var decision = MatchQueueManager.Instance.GetDecision(candidate);
                    sb.AppendLine($"  [{i}] {candidate.profile.characterName} - {decision}");
                }
            }
            else
            {
                sb.AppendLine("  (queue is empty)");
            }

            sb.AppendLine();
            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        private bool ValidateManagers()
        {
            if (ProfileManager.Instance == null)
            {
                Debug.LogError("MatchQueueTester: ProfileManager not found!");
                return false;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("MatchQueueTester: MatchQueueManager not found!");
                return false;
            }

            return true;
        }
    }
}
