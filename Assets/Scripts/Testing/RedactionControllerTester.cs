using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify RedactionController functionality.
    ///
    /// Setup:
    /// 1. Attach to a GameObject alongside ProfileManager, PostPoolManager,
    ///    MatchQueueManager, RedactionManager, and RedactionController
    /// 2. Optionally assign a test candidate
    /// 3. Enter Play Mode
    ///
    /// How to Run: Click the buttons in the Inspector (enabled during Play Mode)
    ///
    /// What to Verify:
    /// - Guaranteed posts are never redacted (IsRedacted returns false)
    /// - Random posts start redacted (IsRedacted returns true)
    /// - GetDisplayText returns blocks for redacted, content for visible
    /// - TryUnredact reveals content and fires event
    /// - Counts are calculated correctly
    /// </summary>
    public class RedactionControllerTester : MonoBehaviour
    {
        [Header("Test Targets")]
        [Tooltip("Candidate to use for testing (optional - will use random if not set)")]
        public CandidateProfileSO testCandidate;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        private bool unredactEventReceived = false;
        private CandidateProfileSO lastEventCandidate = null;
        private SocialMediaPost lastEventPost = null;
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
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (RedactionController.Instance != null)
            {
                RedactionController.Instance.OnPostUnredacted += HandlePostUnredacted;
                RedactionController.Instance.OnRedactionReset += HandleRedactionReset;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RedactionController.Instance != null)
            {
                RedactionController.Instance.OnPostUnredacted -= HandlePostUnredacted;
                RedactionController.Instance.OnRedactionReset -= HandleRedactionReset;
            }
        }

        private void HandlePostUnredacted(CandidateProfileSO candidate, SocialMediaPost post)
        {
            unredactEventReceived = true;
            lastEventCandidate = candidate;
            lastEventPost = post;

            if (verboseOutput)
            {
                Debug.Log($"RedactionControllerTester: OnPostUnredacted fired for {candidate?.profile.characterName}");
            }
        }

        private void HandleRedactionReset()
        {
            resetEventReceived = true;

            if (verboseOutput)
            {
                Debug.Log("RedactionControllerTester: OnRedactionReset fired");
            }
        }

        /// <summary>
        /// Tests that guaranteed posts are never redacted
        /// </summary>
        public void TestGuaranteedPosts()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION CONTROLLER TESTER: Guaranteed Posts ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine($"  Guaranteed posts: {candidate.guaranteedPosts.Count}");
            sb.AppendLine();

            sb.AppendLine("--- GUARANTEED POST REDACTION STATUS ---");

            bool allCorrect = true;
            foreach (var post in candidate.guaranteedPosts)
            {
                bool isRedacted = RedactionController.Instance.IsRedacted(candidate, post);
                bool isGuaranteed = RedactionController.Instance.IsGuaranteedPost(candidate, post);

                sb.AppendLine($"  \"{TruncateContent(post.content)}\"");
                sb.AppendLine($"    IsGuaranteed: {isGuaranteed}");
                sb.AppendLine($"    IsRedacted: {isRedacted}");

                if (isRedacted)
                {
                    sb.AppendLine($"    ERROR: Guaranteed post should NOT be redacted!");
                    allCorrect = false;
                }
                else
                {
                    sb.AppendLine($"    PASS: Correctly visible");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"--- RESULT: {(allCorrect ? "ALL PASSED" : "SOME FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests that random posts start redacted
        /// </summary>
        public void TestRandomPostsRedacted()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION CONTROLLER TESTER: Random Posts Redacted ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            // Reset to ensure clean state
            RedactionManager.Instance.ResetAll();

            var allPosts = candidate.GetPostsForPlaythrough();

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine($"  Total posts: {allPosts.Count}");
            sb.AppendLine($"  Guaranteed: {candidate.guaranteedPosts.Count}");
            sb.AppendLine($"  Random: {allPosts.Count - candidate.guaranteedPosts.Count}");
            sb.AppendLine();

            sb.AppendLine("--- RANDOM POST REDACTION STATUS ---");

            int randomCount = 0;
            int correctlyRedacted = 0;

            foreach (var post in allPosts)
            {
                bool isGuaranteed = RedactionController.Instance.IsGuaranteedPost(candidate, post);
                if (isGuaranteed) continue;

                randomCount++;
                bool isRedacted = RedactionController.Instance.IsRedacted(candidate, post);

                if (isRedacted)
                {
                    correctlyRedacted++;
                    if (verboseOutput)
                    {
                        sb.AppendLine($"  PASS: \"{TruncateContent(post.content)}\" - correctly redacted");
                    }
                }
                else
                {
                    sb.AppendLine($"  FAIL: \"{TruncateContent(post.content)}\" - should be redacted!");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"--- RESULT: {correctlyRedacted}/{randomCount} random posts correctly redacted ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests GetDisplayText returns correct content
        /// </summary>
        public void TestGetDisplayText()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION CONTROLLER TESTER: GetDisplayText ===");
            sb.AppendLine();

            var candidate = GetTestCandidate();
            if (candidate == null)
            {
                sb.AppendLine("  ERROR: No candidate available for testing");
                Debug.Log(sb.ToString());
                return;
            }

            // Reset to ensure clean state
            RedactionManager.Instance.ResetAll();

            var allPosts = candidate.GetPostsForPlaythrough();

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine();

            sb.AppendLine("--- GUARANTEED POST DISPLAY TEXT ---");
            foreach (var post in candidate.guaranteedPosts)
            {
                string displayText = RedactionController.Instance.GetDisplayText(candidate, post);
                bool showsRealContent = displayText == post.content;
                sb.AppendLine($"  Content: \"{TruncateContent(post.content)}\"");
                sb.AppendLine($"  Display: \"{TruncateContent(displayText)}\"");
                sb.AppendLine($"  Shows real content: {showsRealContent} {(showsRealContent ? "PASS" : "FAIL")}");
                sb.AppendLine();
            }

            sb.AppendLine("--- RANDOM POST DISPLAY TEXT ---");
            foreach (var post in allPosts)
            {
                if (RedactionController.Instance.IsGuaranteedPost(candidate, post)) continue;

                string displayText = RedactionController.Instance.GetDisplayText(candidate, post);
                bool hasBlocks = displayText.Contains("█");
                sb.AppendLine($"  Content: \"{TruncateContent(post.content)}\"");
                sb.AppendLine($"  Display: \"{TruncateContent(displayText)}\"");
                sb.AppendLine($"  Shows blocks: {hasBlocks} {(hasBlocks ? "PASS" : "FAIL")}");
                sb.AppendLine();
                break; // Just test one for brevity
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests TryUnredact reveals content and fires event
        /// </summary>
        public void TestTryUnredact()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION CONTROLLER TESTER: TryUnredact ===");
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

            // Find a random (non-guaranteed) post
            SocialMediaPost randomPost = null;
            foreach (var post in allPosts)
            {
                if (!RedactionController.Instance.IsGuaranteedPost(candidate, post))
                {
                    randomPost = post;
                    break;
                }
            }

            if (randomPost == null)
            {
                sb.AppendLine("  ERROR: No random posts available to test");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine($"  Post content: \"{TruncateContent(randomPost.content)}\"");
            sb.AppendLine();

            // Check before state
            sb.AppendLine("--- BEFORE UNREDACT ---");
            bool beforeRedacted = RedactionController.Instance.IsRedacted(candidate, randomPost);
            string beforeDisplay = RedactionController.Instance.GetDisplayText(candidate, randomPost);
            sb.AppendLine($"  IsRedacted: {beforeRedacted}");
            sb.AppendLine($"  Display: \"{TruncateContent(beforeDisplay)}\"");
            sb.AppendLine();

            // Unredact
            unredactEventReceived = false;
            lastEventCandidate = null;
            lastEventPost = null;
            bool result = RedactionController.Instance.TryUnredact(candidate, randomPost);

            sb.AppendLine("--- AFTER UNREDACT ---");
            bool afterRedacted = RedactionController.Instance.IsRedacted(candidate, randomPost);
            string afterDisplay = RedactionController.Instance.GetDisplayText(candidate, randomPost);
            sb.AppendLine($"  TryUnredact returned: {result}");
            sb.AppendLine($"  IsRedacted: {afterRedacted}");
            sb.AppendLine($"  Display: \"{TruncateContent(afterDisplay)}\"");
            sb.AppendLine($"  Event fired: {unredactEventReceived}");
            sb.AppendLine($"  Event candidate matches: {lastEventCandidate == candidate}");
            sb.AppendLine($"  Event post matches: {lastEventPost == randomPost}");
            sb.AppendLine($"  Shows real content: {afterDisplay == randomPost.content}");
            sb.AppendLine();

            // Verify
            bool passed = result && !afterRedacted && afterDisplay == randomPost.content && unredactEventReceived;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests counts are calculated correctly
        /// </summary>
        public void TestCounts()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION CONTROLLER TESTER: Counts ===");
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

            int total = RedactionController.Instance.GetTotalCount(candidate);
            int guaranteed = RedactionController.Instance.GetGuaranteedCount(candidate);
            int randomExpected = total - guaranteed;

            sb.AppendLine($"  Candidate: {candidate.profile.characterName}");
            sb.AppendLine($"  Total posts: {total}");
            sb.AppendLine($"  Guaranteed: {guaranteed}");
            sb.AppendLine($"  Random (expected): {randomExpected}");
            sb.AppendLine();

            sb.AppendLine("--- INITIAL STATE ---");
            int redactedCount = RedactionController.Instance.GetRedactedCount(candidate);
            int visibleCount = RedactionController.Instance.GetVisibleCount(candidate);
            sb.AppendLine($"  Redacted: {redactedCount} (expected: {randomExpected})");
            sb.AppendLine($"  Visible: {visibleCount} (expected: {guaranteed})");
            sb.AppendLine();

            // Unredact one random post
            var allPosts = candidate.GetPostsForPlaythrough();
            foreach (var post in allPosts)
            {
                if (!RedactionController.Instance.IsGuaranteedPost(candidate, post))
                {
                    RedactionController.Instance.TryUnredact(candidate, post);
                    break;
                }
            }

            sb.AppendLine("--- AFTER UNREDACTING ONE ---");
            redactedCount = RedactionController.Instance.GetRedactedCount(candidate);
            visibleCount = RedactionController.Instance.GetVisibleCount(candidate);
            sb.AppendLine($"  Redacted: {redactedCount} (expected: {randomExpected - 1})");
            sb.AppendLine($"  Visible: {visibleCount} (expected: {guaranteed + 1})");
            sb.AppendLine();

            bool passed = redactedCount == randomExpected - 1 && visibleCount == guaranteed + 1;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs current controller state for all candidates in queue
        /// </summary>
        public void LogCurrentState()
        {
            if (!ValidateControllers()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== REDACTION CONTROLLER STATE ===");
            sb.AppendLine();

            if (MatchQueueManager.Instance == null || MatchQueueManager.Instance.Count == 0)
            {
                sb.AppendLine("  No candidates in queue");
                sb.AppendLine();
                sb.AppendLine("=== END STATE ===");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"  Candidates in queue: {MatchQueueManager.Instance.Count}");
            sb.AppendLine();

            foreach (var candidate in MatchQueueManager.Instance.Queue)
            {
                int total = RedactionController.Instance.GetTotalCount(candidate);
                int guaranteed = RedactionController.Instance.GetGuaranteedCount(candidate);
                int redacted = RedactionController.Instance.GetRedactedCount(candidate);
                int visible = RedactionController.Instance.GetVisibleCount(candidate);

                sb.AppendLine($"  {candidate.profile.characterName}:");
                sb.AppendLine($"    Total posts: {total}");
                sb.AppendLine($"    Guaranteed: {guaranteed}");
                sb.AppendLine($"    Redacted: {redacted}");
                sb.AppendLine($"    Visible: {visible}");
                sb.AppendLine();
            }

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
                Debug.LogError("RedactionControllerTester: MatchQueueManager not found!");
                return;
            }

            MatchQueueManager.Instance.PopulateRandom(3);
            Debug.Log("RedactionControllerTester: Populated queue with 3 random candidates");
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
            Debug.Log("RedactionControllerTester: Queue cleared");
        }

        private CandidateProfileSO GetTestCandidate()
        {
            if (testCandidate != null)
            {
                return testCandidate;
            }

            if (MatchQueueManager.Instance != null && MatchQueueManager.Instance.Count > 0)
            {
                return MatchQueueManager.Instance.GetCandidateAt(0);
            }

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

        private bool ValidateControllers()
        {
            if (ProfileManager.Instance == null)
            {
                Debug.LogError("RedactionControllerTester: ProfileManager not found!");
                return false;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("RedactionControllerTester: MatchQueueManager not found!");
                return false;
            }

            if (RedactionManager.Instance == null)
            {
                Debug.LogError("RedactionControllerTester: RedactionManager not found!");
                return false;
            }

            if (RedactionController.Instance == null)
            {
                Debug.LogError("RedactionControllerTester: RedactionController not found!");
                return false;
            }

            return true;
        }
    }
}
