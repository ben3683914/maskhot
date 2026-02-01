using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Maskhot.Data;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify SocialFeedController functionality.
    /// Add this to a GameObject along with SocialFeedController.
    /// Use the Context Menu or Inspector buttons to run tests.
    /// </summary>
    public class SocialFeedTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Primary candidate to test")]
        public CandidateProfileSO primaryCandidate;

        [Tooltip("Secondary candidate for switching tests")]
        public CandidateProfileSO secondaryCandidate;

        [Tooltip("Enable detailed logging for posts")]
        public bool verbosePostLogging = true;

        [Header("Batch Testing")]
        [Tooltip("Candidates for batch testing")]
        public CandidateProfileSO[] candidatesToTest;

        // Track event firing
        private int eventFiredCount = 0;
        private CandidateProfileSO lastEventCandidate;

        private void OnEnable()
        {
            if (SocialFeedController.Instance != null)
            {
                SocialFeedController.Instance.OnCandidateChanged += OnCandidateChangedHandler;
            }
        }

        private void OnDisable()
        {
            if (SocialFeedController.Instance != null)
            {
                SocialFeedController.Instance.OnCandidateChanged -= OnCandidateChangedHandler;
            }
        }

        private void OnCandidateChangedHandler(CandidateProfileSO candidate)
        {
            eventFiredCount++;
            lastEventCandidate = candidate;
        }

        /// <summary>
        /// Tests setting a specific candidate and viewing their feed
        /// </summary>
        [ContextMenu("Test Set Candidate")]
        public void TestSetCandidate()
        {
            if (!ValidateController()) return;

            if (primaryCandidate == null)
            {
                Debug.LogError("SocialFeedTester: No primary candidate assigned!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌─────────────────────────────────────────────────────────────");
            sb.AppendLine("│ SOCIAL FEED CONTROLLER TEST - Set Candidate");
            sb.AppendLine("├─────────────────────────────────────────────────────────────");

            // Reset event counter
            eventFiredCount = 0;
            lastEventCandidate = null;

            // Set the candidate
            SocialFeedController.Instance.SetCandidate(primaryCandidate);

            // Validate state
            bool hasCandidate = SocialFeedController.Instance.HasCandidate;
            var currentCandidate = SocialFeedController.Instance.CurrentCandidate;
            var posts = SocialFeedController.Instance.CurrentPosts;

            sb.AppendLine($"│ Set candidate: {primaryCandidate.profile.characterName}");
            sb.AppendLine($"│ HasCandidate: {hasCandidate} (expected: True)");
            sb.AppendLine($"│ CurrentCandidate matches: {currentCandidate == primaryCandidate}");
            sb.AppendLine($"│ Event fired: {eventFiredCount} time(s)");
            sb.AppendLine($"│ Event candidate matches: {lastEventCandidate == primaryCandidate}");
            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ POSTS: {posts.Count} total");

            if (verbosePostLogging)
            {
                AppendPostDetails(sb, posts);
            }

            // Validation summary
            bool allPassed = hasCandidate &&
                             currentCandidate == primaryCandidate &&
                             eventFiredCount == 1 &&
                             lastEventCandidate == primaryCandidate;

            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ RESULT: {(allPassed ? "✓ PASSED" : "✗ FAILED")}");
            sb.AppendLine("└─────────────────────────────────────────────────────────────");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests clearing the feed
        /// </summary>
        [ContextMenu("Test Clear Feed")]
        public void TestClearFeed()
        {
            if (!ValidateController()) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌─────────────────────────────────────────────────────────────");
            sb.AppendLine("│ SOCIAL FEED CONTROLLER TEST - Clear Feed");
            sb.AppendLine("├─────────────────────────────────────────────────────────────");

            // First set a candidate if we have one
            if (primaryCandidate != null)
            {
                SocialFeedController.Instance.SetCandidate(primaryCandidate);
                sb.AppendLine($"│ Initial: Set {primaryCandidate.profile.characterName}");
            }

            // Reset event counter
            eventFiredCount = 0;
            lastEventCandidate = primaryCandidate; // Set to non-null to verify it changes

            // Clear the feed
            SocialFeedController.Instance.ClearFeed();

            // Validate state
            bool hasCandidate = SocialFeedController.Instance.HasCandidate;
            var currentCandidate = SocialFeedController.Instance.CurrentCandidate;
            var posts = SocialFeedController.Instance.CurrentPosts;

            sb.AppendLine($"│ After ClearFeed():");
            sb.AppendLine($"│ HasCandidate: {hasCandidate} (expected: False)");
            sb.AppendLine($"│ CurrentCandidate is null: {currentCandidate == null}");
            sb.AppendLine($"│ CurrentPosts count: {posts.Count} (expected: 0)");
            sb.AppendLine($"│ Event fired: {eventFiredCount} time(s)");
            sb.AppendLine($"│ Event candidate is null: {lastEventCandidate == null}");

            // Validation summary
            bool allPassed = !hasCandidate &&
                             currentCandidate == null &&
                             posts.Count == 0 &&
                             eventFiredCount == 1 &&
                             lastEventCandidate == null;

            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ RESULT: {(allPassed ? "✓ PASSED" : "✗ FAILED")}");
            sb.AppendLine("└─────────────────────────────────────────────────────────────");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests switching between candidates
        /// </summary>
        [ContextMenu("Test Switch Candidates")]
        public void TestSwitchCandidates()
        {
            if (!ValidateController()) return;

            if (primaryCandidate == null || secondaryCandidate == null)
            {
                Debug.LogError("SocialFeedTester: Both primary and secondary candidates must be assigned!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌─────────────────────────────────────────────────────────────");
            sb.AppendLine("│ SOCIAL FEED CONTROLLER TEST - Switch Candidates");
            sb.AppendLine("├─────────────────────────────────────────────────────────────");

            // Reset event counter
            eventFiredCount = 0;

            // Set primary candidate
            SocialFeedController.Instance.SetCandidate(primaryCandidate);
            var primaryPosts = SocialFeedController.Instance.CurrentPosts;
            int primaryPostCount = primaryPosts.Count;

            sb.AppendLine($"│ Set PRIMARY: {primaryCandidate.profile.characterName}");
            sb.AppendLine($"│   Posts: {primaryPostCount}");
            sb.AppendLine($"│   Events fired so far: {eventFiredCount}");

            // Set secondary candidate
            SocialFeedController.Instance.SetCandidate(secondaryCandidate);
            var secondaryPosts = SocialFeedController.Instance.CurrentPosts;
            int secondaryPostCount = secondaryPosts.Count;

            sb.AppendLine($"│ Set SECONDARY: {secondaryCandidate.profile.characterName}");
            sb.AppendLine($"│   Posts: {secondaryPostCount}");
            sb.AppendLine($"│   Events fired so far: {eventFiredCount}");

            // Switch back to primary
            SocialFeedController.Instance.SetCandidate(primaryCandidate);
            var primaryPostsAgain = SocialFeedController.Instance.CurrentPosts;

            sb.AppendLine($"│ Switch back to PRIMARY: {primaryCandidate.profile.characterName}");
            sb.AppendLine($"│   Posts: {primaryPostsAgain.Count}");
            sb.AppendLine($"│   Posts cached correctly: {primaryPostsAgain.Count == primaryPostCount}");
            sb.AppendLine($"│   Events fired so far: {eventFiredCount}");

            // Try setting same candidate again (should not fire event)
            int eventsBeforeDuplicate = eventFiredCount;
            SocialFeedController.Instance.SetCandidate(primaryCandidate);

            sb.AppendLine($"│ Set same candidate again:");
            sb.AppendLine($"│   Event skipped (no change): {eventFiredCount == eventsBeforeDuplicate}");

            // Validation summary
            bool postsMatch = primaryPostsAgain.Count == primaryPostCount;
            bool eventCountCorrect = eventFiredCount == 3; // primary, secondary, primary again
            bool duplicateSkipped = eventFiredCount == eventsBeforeDuplicate;

            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ RESULT: {(postsMatch && eventCountCorrect && duplicateSkipped ? "✓ PASSED" : "✗ FAILED")}");
            sb.AppendLine("└─────────────────────────────────────────────────────────────");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests all assigned candidates in sequence
        /// </summary>
        [ContextMenu("Test All Assigned Candidates")]
        public void TestAllAssignedCandidates()
        {
            if (!ValidateController()) return;

            if (candidatesToTest == null || candidatesToTest.Length == 0)
            {
                Debug.LogError("SocialFeedTester: No candidates assigned to test array!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"═══ SOCIAL FEED CONTROLLER TEST - {candidatesToTest.Length} CANDIDATES ═══");

            int totalPosts = 0;
            int totalGreen = 0;
            int totalRed = 0;

            foreach (var candidate in candidatesToTest)
            {
                if (candidate == null) continue;

                SocialFeedController.Instance.SetCandidate(candidate);
                var posts = SocialFeedController.Instance.CurrentPosts;

                int greenFlags = 0;
                int redFlags = 0;
                foreach (var post in posts)
                {
                    if (post.isGreenFlag) greenFlags++;
                    if (post.isRedFlag) redFlags++;
                }

                sb.AppendLine($"┌─────────────────────────────────────────────────────────────");
                sb.AppendLine($"│ {candidate.profile.characterName}");
                sb.AppendLine($"│ Posts: {posts.Count} | Green: {greenFlags} | Red: {redFlags}");

                if (verbosePostLogging)
                {
                    AppendPostDetails(sb, posts);
                }

                sb.AppendLine("└─────────────────────────────────────────────────────────────");

                totalPosts += posts.Count;
                totalGreen += greenFlags;
                totalRed += redFlags;
            }

            sb.AppendLine($"═══ COMPLETE: {candidatesToTest.Length} candidates, {totalPosts} posts, {totalGreen} green, {totalRed} red ═══");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Runs all tests in sequence
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            if (!ValidateController()) return;

            Debug.Log("═══ SOCIAL FEED CONTROLLER - RUNNING ALL TESTS ═══");

            // Clear first to start from clean state
            SocialFeedController.Instance.ClearFeed();

            if (primaryCandidate != null)
            {
                TestSetCandidate();
            }
            else
            {
                Debug.LogWarning("SocialFeedTester: Skipping TestSetCandidate - no primary candidate");
            }

            TestClearFeed();

            if (primaryCandidate != null && secondaryCandidate != null)
            {
                TestSwitchCandidates();
            }
            else
            {
                Debug.LogWarning("SocialFeedTester: Skipping TestSwitchCandidates - need both candidates");
            }

            Debug.Log("═══ ALL TESTS COMPLETE ═══");
        }

        /// <summary>
        /// Shows current controller state
        /// </summary>
        [ContextMenu("Show Current State")]
        public void ShowCurrentState()
        {
            if (!ValidateController()) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌─────────────────────────────────────────────────────────────");
            sb.AppendLine("│ SOCIAL FEED CONTROLLER - Current State");
            sb.AppendLine("├─────────────────────────────────────────────────────────────");

            var controller = SocialFeedController.Instance;
            sb.AppendLine($"│ HasCandidate: {controller.HasCandidate}");

            if (controller.HasCandidate)
            {
                var candidate = controller.CurrentCandidate;
                var posts = controller.CurrentPosts;

                sb.AppendLine($"│ CurrentCandidate: {candidate.profile.characterName}");
                sb.AppendLine($"│ Age: {candidate.profile.age} | Gender: {candidate.profile.gender}");
                sb.AppendLine($"│ Posts: {posts.Count}");

                if (verbosePostLogging)
                {
                    AppendPostDetails(sb, posts);
                }
            }
            else
            {
                sb.AppendLine("│ CurrentCandidate: (none)");
                sb.AppendLine("│ Posts: 0");
            }

            sb.AppendLine("└─────────────────────────────────────────────────────────────");

            Debug.Log(sb.ToString());
        }

        private bool ValidateController()
        {
            if (SocialFeedController.Instance == null)
            {
                Debug.LogError("SocialFeedTester: SocialFeedController not found! Make sure it's in the scene.");
                return false;
            }
            return true;
        }

        private void AppendPostDetails(StringBuilder sb, List<SocialMediaPost> posts)
        {
            if (posts.Count == 0) return;

            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            for (int i = 0; i < posts.Count; i++)
            {
                var post = posts[i];
                string flag = post.isGreenFlag ? "[+]" : post.isRedFlag ? "[!]" : "[ ]";
                string preview = post.content.Length > 45
                    ? post.content.Substring(0, 45) + "..."
                    : post.content;

                sb.AppendLine($"│  {flag} {post.daysSincePosted}d - {post.postType}: {preview}");
            }
        }
    }
}
