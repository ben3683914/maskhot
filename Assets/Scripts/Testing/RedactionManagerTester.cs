using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify RedactionManager functionality.
    ///
    /// Setup:
    /// 1. Attach to a GameObject alongside ProfileManager, PostPoolManager,
    ///    MatchQueueManager, and RedactionManager
    /// 2. Optionally assign a test candidate
    /// 3. Enter Play Mode
    ///
    /// How to Run: Click the buttons in the Inspector (enabled during Play Mode)
    ///
    /// What to Verify:
    /// - IsUnredacted returns false for new posts
    /// - MarkUnredacted adds posts to the set and fires event
    /// - ResetAll clears all state and fires event
    /// - UnredactedCount property is accurate
    /// </summary>
    public class RedactionManagerTester : MonoBehaviour
    {
        [Header("Test Targets")]
        [Tooltip("Candidate to use for testing (optional - will use random if not set)")]
        public CandidateProfileSO testCandidate;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        private bool unredactEventReceived = false;
        private SocialMediaPost lastUnredactedPost = null;
        private bool resetEventReceived = false;

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
            // Re-subscribe in case manager wasn't ready
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (RedactionManager.Instance != null)
            {
                RedactionManager.Instance.OnPostUnredacted += HandlePostUnredacted;
                RedactionManager.Instance.OnRedactionReset += HandleRedactionReset;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RedactionManager.Instance != null)
            {
                RedactionManager.Instance.OnPostUnredacted -= HandlePostUnredacted;
                RedactionManager.Instance.OnRedactionReset -= HandleRedactionReset;
            }
        }

        private void HandlePostUnredacted(SocialMediaPost post)
        {
            unredactEventReceived = true;
            lastUnredactedPost = post;

            if (verboseOutput)
            {
                Debug.Log($"RedactionManagerTester: OnPostUnredacted fired");
            }
        }

        private void HandleRedactionReset()
        {
            resetEventReceived = true;

            if (verboseOutput)
            {
                Debug.Log("RedactionManagerTester: OnRedactionReset fired");
            }
        }

        /// <summary>
        /// Tests IsUnredacted returns false for new posts
        /// </summary>
        public void TestIsUnredacted()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION MANAGER TESTER: IsUnredacted ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            // Reset first to ensure clean state
            RedactionManager.Instance.ResetAll();

            var allPosts = candidate.GetPostsForPlaythrough();

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine($"  Total posts: {allPosts.Count}");
            sb.AppendLine();

            sb.AppendLine("--- CHECKING INITIAL STATE ---");
            int unredactedCount = 0;
            foreach (var post in allPosts)
            {
                bool isUnredacted = RedactionManager.Instance.IsUnredacted(post);
                if (isUnredacted)
                {
                    unredactedCount++;
                    sb.AppendLine($"  UNEXPECTED: Post is unredacted: \"{TruncateContent(post.content)}\"");
                }
            }

            if (unredactedCount == 0)
            {
                sb.AppendLine($"  PASS: All {allPosts.Count} posts return IsUnredacted=false");
            }
            else
            {
                sb.AppendLine($"  FAIL: {unredactedCount} posts unexpectedly unredacted");
            }
            sb.AppendLine();

            sb.AppendLine($"  UnredactedCount property: {RedactionManager.Instance.UnredactedCount}");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests MarkUnredacted adds post and fires event
        /// </summary>
        public void TestMarkUnredacted()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION MANAGER TESTER: MarkUnredacted ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            // Reset first
            RedactionManager.Instance.ResetAll();

            var allPosts = candidate.GetPostsForPlaythrough();
            if (allPosts.Count == 0)
            {
                sb.AppendLine("  ERROR: No posts available to test");
                Debug.Log(sb.ToString());
                return;
            }

            var testPost = allPosts[0];

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine($"  Test post: \"{TruncateContent(testPost.content)}\"");
            sb.AppendLine();

            // Check before state
            sb.AppendLine("--- BEFORE MARK ---");
            bool beforeState = RedactionManager.Instance.IsUnredacted(testPost);
            int beforeCount = RedactionManager.Instance.UnredactedCount;
            sb.AppendLine($"  IsUnredacted: {beforeState}");
            sb.AppendLine($"  UnredactedCount: {beforeCount}");
            sb.AppendLine();

            // Mark as unredacted
            unredactEventReceived = false;
            lastUnredactedPost = null;
            bool result = RedactionManager.Instance.MarkUnredacted(testPost);

            sb.AppendLine("--- AFTER MARK ---");
            bool afterState = RedactionManager.Instance.IsUnredacted(testPost);
            int afterCount = RedactionManager.Instance.UnredactedCount;
            sb.AppendLine($"  MarkUnredacted returned: {result}");
            sb.AppendLine($"  IsUnredacted: {afterState}");
            sb.AppendLine($"  UnredactedCount: {afterCount}");
            sb.AppendLine($"  Event fired: {unredactEventReceived}");
            sb.AppendLine($"  Event post matches: {lastUnredactedPost == testPost}");
            sb.AppendLine();

            // Verify
            bool passed = result && afterState && afterCount == 1 && unredactEventReceived && lastUnredactedPost == testPost;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests that MarkUnredacted returns false for already unredacted post
        /// </summary>
        public void TestMarkUnredactedTwice()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION MANAGER TESTER: MarkUnredacted Twice ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            // Reset first
            RedactionManager.Instance.ResetAll();

            var allPosts = candidate.GetPostsForPlaythrough();
            if (allPosts.Count == 0)
            {
                sb.AppendLine("  ERROR: No posts available to test");
                Debug.Log(sb.ToString());
                return;
            }

            var testPost = allPosts[0];

            sb.AppendLine($"  Test post: \"{TruncateContent(testPost.content)}\"");
            sb.AppendLine();

            // First mark
            bool firstResult = RedactionManager.Instance.MarkUnredacted(testPost);
            sb.AppendLine($"  First MarkUnredacted: {firstResult} (expected: true)");

            // Second mark
            unredactEventReceived = false;
            bool secondResult = RedactionManager.Instance.MarkUnredacted(testPost);
            sb.AppendLine($"  Second MarkUnredacted: {secondResult} (expected: false)");
            sb.AppendLine($"  Event fired on second call: {unredactEventReceived} (expected: false)");
            sb.AppendLine($"  UnredactedCount: {RedactionManager.Instance.UnredactedCount} (expected: 1)");
            sb.AppendLine();

            bool passed = firstResult && !secondResult && !unredactEventReceived && RedactionManager.Instance.UnredactedCount == 1;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests ResetAll clears state and fires event
        /// </summary>
        public void TestResetAll()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION MANAGER TESTER: ResetAll ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            // Reset first, then add some unredacted posts
            RedactionManager.Instance.ResetAll();

            var allPosts = candidate.GetPostsForPlaythrough();
            int toUnredact = Mathf.Min(3, allPosts.Count);
            for (int i = 0; i < toUnredact; i++)
            {
                RedactionManager.Instance.MarkUnredacted(allPosts[i]);
            }

            sb.AppendLine($"  Unredacted {toUnredact} posts");
            sb.AppendLine($"  UnredactedCount before reset: {RedactionManager.Instance.UnredactedCount}");
            sb.AppendLine();

            // Reset
            resetEventReceived = false;
            RedactionManager.Instance.ResetAll();

            sb.AppendLine("--- AFTER RESET ---");
            sb.AppendLine($"  UnredactedCount: {RedactionManager.Instance.UnredactedCount}");
            sb.AppendLine($"  Reset event fired: {resetEventReceived}");
            sb.AppendLine();

            // Verify posts are no longer unredacted
            int stillUnredacted = 0;
            for (int i = 0; i < toUnredact; i++)
            {
                if (RedactionManager.Instance.IsUnredacted(allPosts[i]))
                {
                    stillUnredacted++;
                }
            }
            sb.AppendLine($"  Posts still unredacted: {stillUnredacted} (expected: 0)");
            sb.AppendLine();

            bool passed = RedactionManager.Instance.UnredactedCount == 0 && resetEventReceived && stillUnredacted == 0;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs current manager state
        /// </summary>
        public void LogCurrentState()
        {
            if (!ValidateManagers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION MANAGER STATE ===");
            sb.AppendLine();

            sb.AppendLine($"  UnredactedCount: {RedactionManager.Instance.UnredactedCount}");
            sb.AppendLine();

            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Populates queue for testing
        /// </summary>
        public void PopulateTestQueue()
        {
            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("RedactionManagerTester: MatchQueueManager not found!");
                return;
            }

            MatchQueueManager.Instance.PopulateRandom(3);
            Debug.Log("RedactionManagerTester: Populated queue with 3 random candidates");
        }

        /// <summary>
        /// Clears the queue
        /// </summary>
        public void ClearQueue()
        {
            if (MatchQueueManager.Instance != null)
            {
                MatchQueueManager.Instance.ClearQueue();
            }
            Debug.Log("RedactionManagerTester: Queue cleared");
        }

        private CandidateProfileSO GetTestCandidate()
        {
            if (testCandidate != null)
            {
                return testCandidate;
            }

            // Try to get from queue
            if (MatchQueueManager.Instance != null && MatchQueueManager.Instance.Count > 0)
            {
                return MatchQueueManager.Instance.GetCandidateAt(0);
            }

            // Try to get any candidate
            if (ProfileManager.Instance != null)
            {
                var all = ProfileManager.Instance.GetAllCandidates();
                if (all != null && all.Length > 0)
                {
                    return all[0];
                }
            }

            return null;
        }

        private string TruncateContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "(empty)";
            return content.Length > 40 ? content.Substring(0, 37) + "..." : content;
        }

        private bool ValidateManagers()
        {
            if (ProfileManager.Instance == null)
            {
                Debug.LogError("RedactionManagerTester: ProfileManager not found!");
                return false;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("RedactionManagerTester: MatchQueueManager not found!");
                return false;
            }

            if (RedactionManager.Instance == null)
            {
                Debug.LogError("RedactionManagerTester: RedactionManager not found!");
                return false;
            }

            return true;
        }
    }
}
