using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify MatchListController functionality.
    /// Add this to a GameObject along with ProfileManager, MatchQueueManager, and MatchListController.
    /// Use the Context Menu to run tests.
    /// Replaces the deprecated SocialFeedTester.
    /// </summary>
    public class MatchListTester : MonoBehaviour
    {
        [Header("Test Targets")]
        [Tooltip("Specific candidates to use for testing")]
        public CandidateProfileSO[] testCandidates;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        private bool eventReceived = false;
        private CandidateProfileSO lastEventCandidate = null;

        private void OnEnable()
        {
            if (MatchListController.Instance != null)
            {
                MatchListController.Instance.OnSelectionChanged += HandleSelectionChanged;
                MatchListController.Instance.OnQueueUpdated += HandleQueueUpdated;
            }
        }

        private void OnDisable()
        {
            if (MatchListController.Instance != null)
            {
                MatchListController.Instance.OnSelectionChanged -= HandleSelectionChanged;
                MatchListController.Instance.OnQueueUpdated -= HandleQueueUpdated;
            }
        }

        private void Start()
        {
            // Re-subscribe in Start in case controller wasn't ready
            if (MatchListController.Instance != null)
            {
                MatchListController.Instance.OnSelectionChanged -= HandleSelectionChanged;
                MatchListController.Instance.OnSelectionChanged += HandleSelectionChanged;
                MatchListController.Instance.OnQueueUpdated -= HandleQueueUpdated;
                MatchListController.Instance.OnQueueUpdated += HandleQueueUpdated;
            }
        }

        private void HandleSelectionChanged(CandidateProfileSO candidate)
        {
            eventReceived = true;
            lastEventCandidate = candidate;

            if (verboseOutput)
            {
                string name = candidate != null ? candidate.profile.characterName : "null";
                Debug.Log($"MatchListTester: OnSelectionChanged fired - {name}");
            }
        }

        private void HandleQueueUpdated()
        {
            if (verboseOutput)
            {
                Debug.Log("MatchListTester: OnQueueUpdated fired");
            }
        }

        /// <summary>
        /// Tests selection by index
        /// </summary>
        [ContextMenu("Test Selection By Index")]
        public void TestSelectionByIndex()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST TESTER: Selection By Index ===");
            sb.AppendLine();

            EnsureQueuePopulated();

            sb.AppendLine("--- TESTING INDEX SELECTION ---");

            // Select index 0
            eventReceived = false;
            bool success = MatchListController.Instance.SelectByIndex(0);
            sb.AppendLine($"  SelectByIndex(0): {success}");
            sb.AppendLine($"    Event fired: {eventReceived}");
            sb.AppendLine($"    CurrentCandidate: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"}");
            sb.AppendLine($"    CurrentIndex: {MatchListController.Instance.CurrentIndex}");
            sb.AppendLine();

            // Select index 1 (if available)
            if (MatchQueueManager.Instance.Count > 1)
            {
                eventReceived = false;
                success = MatchListController.Instance.SelectByIndex(1);
                sb.AppendLine($"  SelectByIndex(1): {success}");
                sb.AppendLine($"    Event fired: {eventReceived}");
                sb.AppendLine($"    CurrentCandidate: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"}");
                sb.AppendLine();
            }

            // Select invalid index
            eventReceived = false;
            success = MatchListController.Instance.SelectByIndex(999);
            sb.AppendLine($"  SelectByIndex(999): {success}");
            sb.AppendLine($"    Event fired: {eventReceived}");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests navigation (next/previous)
        /// </summary>
        [ContextMenu("Test Navigation")]
        public void TestNavigation()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST TESTER: Navigation ===");
            sb.AppendLine();

            EnsureQueuePopulated();

            sb.AppendLine("--- INITIAL STATE ---");
            sb.AppendLine($"  Queue size: {MatchQueueManager.Instance.Count}");
            sb.AppendLine();

            // Start at first
            MatchListController.Instance.SelectFirst();
            sb.AppendLine($"  SelectFirst(): {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"} (index {MatchListController.Instance.CurrentIndex})");
            sb.AppendLine($"    HasNext: {MatchListController.Instance.HasNext}");
            sb.AppendLine($"    HasPrevious: {MatchListController.Instance.HasPrevious}");
            sb.AppendLine();

            // Navigate forward
            sb.AppendLine("--- NAVIGATING FORWARD ---");
            int maxSteps = Mathf.Min(3, MatchQueueManager.Instance.Count - 1);
            for (int i = 0; i < maxSteps; i++)
            {
                bool success = MatchListController.Instance.SelectNext();
                sb.AppendLine($"  SelectNext(): {success} -> {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"} (index {MatchListController.Instance.CurrentIndex})");
            }
            sb.AppendLine();

            // Navigate backward
            sb.AppendLine("--- NAVIGATING BACKWARD ---");
            for (int i = 0; i < maxSteps; i++)
            {
                bool success = MatchListController.Instance.SelectPrevious();
                sb.AppendLine($"  SelectPrevious(): {success} -> {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"} (index {MatchListController.Instance.CurrentIndex})");
            }
            sb.AppendLine();

            // Try to go before first
            sb.AppendLine("--- BOUNDARY TEST ---");
            MatchListController.Instance.SelectFirst();
            bool canGoBefore = MatchListController.Instance.SelectPrevious();
            sb.AppendLine($"  SelectPrevious() at start: {canGoBefore}");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests SelectNextPending functionality
        /// </summary>
        [ContextMenu("Test Select Next Pending")]
        public void TestSelectNextPending()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST TESTER: Select Next Pending ===");
            sb.AppendLine();

            EnsureQueuePopulated();

            // Mark first two as decided
            var queue = MatchQueueManager.Instance.Queue;
            if (queue.Count >= 3)
            {
                MatchQueueManager.Instance.Accept(queue[0]);
                MatchQueueManager.Instance.Reject(queue[1]);
                sb.AppendLine($"  Accepted: {queue[0].profile.characterName}");
                sb.AppendLine($"  Rejected: {queue[1].profile.characterName}");
                sb.AppendLine($"  Pending: {MatchQueueManager.Instance.PendingCount} remaining");
                sb.AppendLine();
            }

            // Select first (decided)
            MatchListController.Instance.SelectFirst();
            sb.AppendLine($"  Current: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"} (index {MatchListController.Instance.CurrentIndex})");

            // Select next pending
            bool success = MatchListController.Instance.SelectNextPending();
            sb.AppendLine($"  SelectNextPending(): {success}");
            sb.AppendLine($"  Now at: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"} (index {MatchListController.Instance.CurrentIndex})");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests CurrentPosts property
        /// </summary>
        [ContextMenu("Test Current Posts")]
        public void TestCurrentPosts()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST TESTER: Current Posts ===");
            sb.AppendLine();

            EnsureQueuePopulated();

            MatchListController.Instance.SelectFirst();
            var candidate = MatchListController.Instance.CurrentCandidate;

            if (candidate == null)
            {
                sb.AppendLine("  No candidate selected");
            }
            else
            {
                sb.AppendLine($"  Candidate: {candidate.profile.characterName}");

                var posts = MatchListController.Instance.CurrentPosts;
                sb.AppendLine($"  Post count: {posts.Count}");

                // Always show first 3 posts
                if (posts.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- FIRST 3 POSTS ---");
                    int toShow = Mathf.Min(3, posts.Count);
                    for (int i = 0; i < toShow; i++)
                    {
                        var post = posts[i];
                        string preview = post.content.Length > 50
                            ? post.content.Substring(0, 50) + "..."
                            : post.content;
                        string flags = "";
                        if (post.isGreenFlag) flags += " [GREEN]";
                        if (post.isRedFlag) flags += " [RED]";
                        sb.AppendLine($"  [{i}] {post.postType}{flags}");
                        sb.AppendLine($"      \"{preview}\"");
                        sb.AppendLine($"      {post.likes} likes, {post.comments} comments, {post.daysSincePosted}d ago");
                    }
                    if (posts.Count > toShow)
                    {
                        sb.AppendLine($"  ... and {posts.Count - toShow} more");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests clearing selection
        /// </summary>
        [ContextMenu("Test Clear Selection")]
        public void TestClearSelection()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST TESTER: Clear Selection ===");
            sb.AppendLine();

            EnsureQueuePopulated();

            // Select something first
            MatchListController.Instance.SelectFirst();
            sb.AppendLine($"  Before clear: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"}");
            sb.AppendLine($"  HasSelection: {MatchListController.Instance.HasSelection}");
            sb.AppendLine();

            // Clear
            eventReceived = false;
            MatchListController.Instance.ClearSelection();
            sb.AppendLine($"  After clear: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"}");
            sb.AppendLine($"  HasSelection: {MatchListController.Instance.HasSelection}");
            sb.AppendLine($"  CurrentIndex: {MatchListController.Instance.CurrentIndex}");
            sb.AppendLine($"  Event fired: {eventReceived}");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests Queue, Count, PendingCount, and GetDecision properties
        /// </summary>
        [ContextMenu("Test Queue Properties")]
        public void TestQueueProperties()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST TESTER: Queue Properties ===");
            sb.AppendLine();

            EnsureQueuePopulated();

            sb.AppendLine("--- QUEUE ACCESS ---");
            var queue = MatchListController.Instance.Queue;
            sb.AppendLine($"  Queue.Count: {queue.Count}");
            sb.AppendLine($"  Count property: {MatchListController.Instance.Count}");
            sb.AppendLine($"  PendingCount: {MatchListController.Instance.PendingCount}");
            sb.AppendLine();

            // Show first 3 candidates
            sb.AppendLine("--- FIRST 3 CANDIDATES ---");
            int toShow = Mathf.Min(3, queue.Count);
            for (int i = 0; i < toShow; i++)
            {
                var candidate = queue[i];
                var decision = MatchListController.Instance.GetDecision(candidate);
                sb.AppendLine($"  [{i}] {candidate.profile.characterName} - {decision}");
            }
            sb.AppendLine();

            // Make some decisions and verify GetDecision
            if (queue.Count >= 2)
            {
                sb.AppendLine("--- AFTER DECISIONS ---");
                MatchQueueManager.Instance.Accept(queue[0]);
                MatchQueueManager.Instance.Reject(queue[1]);

                sb.AppendLine($"  {queue[0].profile.characterName}: {MatchListController.Instance.GetDecision(queue[0])}");
                sb.AppendLine($"  {queue[1].profile.characterName}: {MatchListController.Instance.GetDecision(queue[1])}");
                sb.AppendLine($"  PendingCount now: {MatchListController.Instance.PendingCount}");
                sb.AppendLine();
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs current controller state
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MATCH LIST CONTROLLER STATE ===");
            sb.AppendLine();

            sb.AppendLine("--- QUEUE ---");
            sb.AppendLine($"  Count: {MatchListController.Instance.Count}");
            sb.AppendLine($"  PendingCount: {MatchListController.Instance.PendingCount}");
            sb.AppendLine();

            sb.AppendLine("--- SELECTION ---");
            sb.AppendLine($"  HasSelection: {MatchListController.Instance.HasSelection}");
            sb.AppendLine($"  CurrentCandidate: {MatchListController.Instance.CurrentCandidate?.profile.characterName ?? "null"}");
            sb.AppendLine($"  CurrentIndex: {MatchListController.Instance.CurrentIndex}");
            sb.AppendLine($"  HasNext: {MatchListController.Instance.HasNext}");
            sb.AppendLine($"  HasPrevious: {MatchListController.Instance.HasPrevious}");

            var posts = MatchListController.Instance.CurrentPosts;
            sb.AppendLine($"  CurrentPosts count: {posts.Count}");

            // Show first 3 posts
            if (posts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- FIRST 3 POSTS ---");
                int toShow = Mathf.Min(3, posts.Count);
                for (int i = 0; i < toShow; i++)
                {
                    var post = posts[i];
                    string preview = post.content.Length > 50
                        ? post.content.Substring(0, 50) + "..."
                        : post.content;
                    string flags = "";
                    if (post.isGreenFlag) flags += " [GREEN]";
                    if (post.isRedFlag) flags += " [RED]";
                    sb.AppendLine($"  [{i}] {post.postType}{flags}");
                    sb.AppendLine($"      \"{preview}\"");
                    sb.AppendLine($"      {post.likes} likes, {post.comments} comments, {post.daysSincePosted}d ago");
                }
                if (posts.Count > toShow)
                {
                    sb.AppendLine($"  ... and {posts.Count - toShow} more");
                }
            }
            sb.AppendLine();

            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        private void EnsureQueuePopulated()
        {
            if (MatchQueueManager.Instance.Count == 0)
            {
                MatchQueueManager.Instance.PopulateRandom(5);
                Debug.Log("MatchListTester: Auto-populated queue with 5 candidates");
            }
        }

        private bool ValidateControllers()
        {
            if (ProfileManager.Instance == null)
            {
                Debug.LogError("MatchListTester: ProfileManager not found!");
                return false;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("MatchListTester: MatchQueueManager not found!");
                return false;
            }

            if (MatchListController.Instance == null)
            {
                Debug.LogError("MatchListTester: MatchListController not found!");
                return false;
            }

            return true;
        }
    }
}
