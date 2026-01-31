using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify the random post system is working correctly.
    /// Add this to a GameObject along with ProfileManager and PostPoolManager.
    /// Use the Context Menu or Inspector buttons to run tests.
    /// </summary>
    public class RandomPostTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Candidate to test (leave empty to test all)")]
        public CandidateProfileSO specificCandidate;

        [Tooltip("Enable detailed logging for each post")]
        public bool verbosePostLogging = true;

        [Header("Quick Test Buttons")]
        [Tooltip("Drag candidates here for batch testing")]
        public CandidateProfileSO[] candidatesToTest;

        /// <summary>
        /// Tests a specific candidate's post generation
        /// </summary>
        [ContextMenu("Test Specific Candidate")]
        public void TestSpecificCandidate()
        {
            if (specificCandidate == null)
            {
                Debug.LogError("RandomPostTester: No specific candidate assigned!");
                return;
            }

            LogCandidateTest(specificCandidate);
        }

        /// <summary>
        /// Tests all candidates in the candidatesToTest array
        /// </summary>
        [ContextMenu("Test All Assigned Candidates")]
        public void TestAllAssignedCandidates()
        {
            if (candidatesToTest == null || candidatesToTest.Length == 0)
            {
                Debug.LogError("RandomPostTester: No candidates assigned to test array!");
                return;
            }

            Debug.Log($"═══ RANDOM POST TESTER - {candidatesToTest.Length} CANDIDATES ═══");

            int totalPosts = 0;
            int totalGreen = 0;
            int totalRed = 0;

            foreach (var candidate in candidatesToTest)
            {
                if (candidate != null)
                {
                    var (posts, green, red) = LogCandidateTest(candidate);
                    totalPosts += posts;
                    totalGreen += green;
                    totalRed += red;
                }
            }

            Debug.Log($"═══ COMPLETE: {candidatesToTest.Length} candidates, {totalPosts} posts, {totalGreen} green, {totalRed} red ═══");
        }

        /// <summary>
        /// Tests all candidates loaded by ProfileManager
        /// </summary>
        [ContextMenu("Test All Candidates (via ProfileManager)")]
        public void TestAllCandidatesViaProfileManager()
        {
            if (ProfileManager.Instance == null)
            {
                Debug.LogError("RandomPostTester: ProfileManager not found! Make sure it's in the scene.");
                return;
            }

            var allCandidates = ProfileManager.Instance.GetAllCandidates();
            if (allCandidates == null || allCandidates.Length == 0)
            {
                Debug.LogError("RandomPostTester: No candidates loaded in ProfileManager!");
                return;
            }

            // Reset the pool before testing all
            if (PostPoolManager.Instance != null)
            {
                PostPoolManager.Instance.ResetPool();
            }

            Debug.Log($"═══ RANDOM POST TESTER - ALL {allCandidates.Length} CANDIDATES (pool reset) ═══");

            int totalPosts = 0;
            int totalGreen = 0;
            int totalRed = 0;

            foreach (var candidate in allCandidates)
            {
                var (posts, green, red) = LogCandidateTest(candidate);
                totalPosts += posts;
                totalGreen += green;
                totalRed += red;
            }

            Debug.Log($"═══ COMPLETE: {allCandidates.Length} candidates, {totalPosts} posts, {totalGreen} green, {totalRed} red ═══");
        }

        /// <summary>
        /// Resets the post pool (allows posts to be reused)
        /// </summary>
        [ContextMenu("Reset Post Pool")]
        public void ResetPostPool()
        {
            if (PostPoolManager.Instance != null)
            {
                PostPoolManager.Instance.ResetPool();
                Debug.Log("RandomPostTester: Post pool has been reset. Posts can now be reused.");
            }
            else
            {
                Debug.LogError("RandomPostTester: PostPoolManager not found!");
            }
        }

        private (int totalPosts, int greenFlags, int redFlags) LogCandidateTest(CandidateProfileSO candidate)
        {
            StringBuilder sb = new StringBuilder();
            var stats = BuildCandidateTestLogWithStats(candidate, sb);
            Debug.Log(sb.ToString());
            return stats;
        }

        private (int totalPosts, int greenFlags, int redFlags) BuildCandidateTestLogWithStats(CandidateProfileSO candidate, StringBuilder sb)
        {
            var profile = candidate.profile;

            sb.AppendLine($"┌─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ {profile.characterName}");
            sb.AppendLine($"├─────────────────────────────────────────────────────────────");

            // Profile info
            sb.AppendLine($"│ Age: {profile.age} | Gender: {profile.gender} | Archetype: {profile.archetype}");
            sb.AppendLine($"│ Friends: {candidate.friendsCountMin}-{candidate.friendsCountMax}");

            if (profile.interests != null && profile.interests.Length > 0)
            {
                string interests = string.Join(", ", System.Array.ConvertAll(profile.interests, i => i?.displayName ?? "?"));
                sb.AppendLine($"│ Interests: {interests}");
            }

            if (profile.personalityTraits != null && profile.personalityTraits.Length > 0)
            {
                string traits = string.Join(", ", System.Array.ConvertAll(profile.personalityTraits, t => t?.displayName ?? "?"));
                sb.AppendLine($"│ Personality: {traits}");
            }

            if (profile.lifestyleTraits != null && profile.lifestyleTraits.Length > 0)
            {
                string lifestyle = string.Join(", ", System.Array.ConvertAll(profile.lifestyleTraits, l => l?.displayName ?? "?"));
                sb.AppendLine($"│ Lifestyle: {lifestyle}");
            }

            // Get posts
            List<SocialMediaPost> allPosts = candidate.GetPostsForPlaythrough();

            // Count stats
            int guaranteedCount = candidate.guaranteedPosts.Count;
            int randomCount = allPosts.Count - guaranteedCount;
            int greenFlags = 0;
            int redFlags = 0;
            int photoCount = 0;
            int textCount = 0;

            foreach (var post in allPosts)
            {
                if (post.isGreenFlag) greenFlags++;
                if (post.isRedFlag) redFlags++;
                if (post.postType == PostType.Photo) photoCount++;
                if (post.postType == PostType.TextOnly) textCount++;
            }

            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ POSTS: {allPosts.Count} total ({guaranteedCount} guaranteed + {randomCount} random)");
            sb.AppendLine($"│ Range: {candidate.randomPostMin}-{candidate.randomPostMax} | Types: {photoCount} Photo, {textCount} Text");
            sb.AppendLine($"│ Flags: {greenFlags} Green | {redFlags} Red");

            if (verbosePostLogging && allPosts.Count > 0)
            {
                sb.AppendLine($"├─────────────────────────────────────────────────────────────");
                sb.AppendLine($"│ POST DETAILS (sorted by date):");

                for (int i = 0; i < allPosts.Count; i++)
                {
                    var post = allPosts[i];
                    string source = i < guaranteedCount ? "G" : "R";
                    string flag = post.isGreenFlag ? "+" : post.isRedFlag ? "!" : " ";

                    string preview = post.content.Length > 50
                        ? post.content.Substring(0, 50) + "..."
                        : post.content;

                    sb.AppendLine($"│  [{source}][{flag}] {post.daysSincePosted}d - {post.postType}: {preview}");
                    sb.AppendLine($"│       Likes: {post.likes}, Comments: {post.comments}");

                    // Log trait associations for random posts
                    if (i >= guaranteedCount)
                    {
                        string traits = GetPostTraitsString(post);
                        if (!string.IsNullOrEmpty(traits))
                        {
                            sb.AppendLine($"│       Matched: [{traits}]");
                        }
                    }
                }
            }

            sb.AppendLine($"└─────────────────────────────────────────────────────────────");

            return (allPosts.Count, greenFlags, redFlags);
        }

        private string GetPostTraitsString(SocialMediaPost post)
        {
            List<string> traits = new List<string>();

            if (post.relatedInterests != null)
            {
                foreach (var interest in post.relatedInterests)
                {
                    if (interest != null) traits.Add(interest.displayName);
                }
            }

            if (post.relatedPersonalityTraits != null)
            {
                foreach (var trait in post.relatedPersonalityTraits)
                {
                    if (trait != null) traits.Add(trait.displayName);
                }
            }

            if (post.relatedLifestyleTraits != null)
            {
                foreach (var trait in post.relatedLifestyleTraits)
                {
                    if (trait != null) traits.Add(trait.displayName);
                }
            }

            return traits.Count > 0 ? string.Join(", ", traits) : "";
        }
    }
}
