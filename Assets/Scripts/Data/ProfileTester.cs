using UnityEngine;
using Maskhot.Data;

namespace Maskhot
{
    /// <summary>
    /// Simple test script to verify CandidateProfileSO data is set up correctly
    /// Attach to a GameObject and drag in a profile to test
    /// </summary>
    public class ProfileTester : MonoBehaviour
    {
        [Header("Test Data")]
        [Tooltip("Drag a CandidateProfileSO here to test")]
        public CandidateProfileSO testProfile;

        [Header("Settings")]
        [Tooltip("Run test on Start?")]
        public bool testOnStart = true;

        void Start()
        {
            if (testOnStart && testProfile != null)
            {
                TestProfile();
            }
        }

        [ContextMenu("Test Profile")]
        public void TestProfile()
        {
            if (testProfile == null)
            {
                Debug.LogError("No test profile assigned!");
                return;
            }

            Debug.Log("=== TESTING CANDIDATE PROFILE ===");
            Debug.Log($"Profile: {testProfile.name}");
            Debug.Log("");

            // Basic Info
            Debug.Log("--- BASIC INFO ---");
            Debug.Log($"Name: {testProfile.profile.characterName}");
            Debug.Log($"Gender: {testProfile.profile.gender}");
            Debug.Log($"Age: {testProfile.profile.age}");
            Debug.Log($"Bio: {testProfile.profile.bio}");
            Debug.Log($"Archetype: {testProfile.profile.archetype}");
            Debug.Log("");

            // Personality Traits
            Debug.Log("--- PERSONALITY TRAITS ---");
            if (testProfile.profile.personalityTraits != null && testProfile.profile.personalityTraits.Length > 0)
            {
                foreach (var trait in testProfile.profile.personalityTraits)
                {
                    if (trait != null)
                    {
                        Debug.Log($"  - {trait.displayName} (Weight: {trait.matchWeight})");
                    }
                }
            }
            else
            {
                Debug.Log("  (None)");
            }
            Debug.Log("");

            // Interests
            Debug.Log("--- INTERESTS ---");
            if (testProfile.profile.interests != null && testProfile.profile.interests.Length > 0)
            {
                foreach (var interest in testProfile.profile.interests)
                {
                    if (interest != null)
                    {
                        Debug.Log($"  - {interest.displayName} ({interest.category}) [Weight: {interest.matchWeight}]");
                    }
                }
            }
            else
            {
                Debug.Log("  (None)");
            }
            Debug.Log("");

            // Lifestyle Traits
            Debug.Log("--- LIFESTYLE TRAITS ---");
            if (testProfile.profile.lifestyleTraits != null && testProfile.profile.lifestyleTraits.Length > 0)
            {
                foreach (var lifestyle in testProfile.profile.lifestyleTraits)
                {
                    if (lifestyle != null)
                    {
                        Debug.Log($"  - {lifestyle.displayName} ({lifestyle.category}) [Weight: {lifestyle.matchWeight}]");
                    }
                }
            }
            else
            {
                Debug.Log("  (None)");
            }
            Debug.Log("");

            // Guaranteed Posts
            Debug.Log("--- GUARANTEED POSTS ---");
            Debug.Log($"Total Posts: {testProfile.guaranteedPosts.Count}");
            for (int i = 0; i < testProfile.guaranteedPosts.Count; i++)
            {
                var post = testProfile.guaranteedPosts[i];
                Debug.Log($"\nPost {i + 1}:");
                Debug.Log($"  Type: {post.postType}");
                Debug.Log($"  Content: {post.content}");
                Debug.Log($"  Timestamp: {post.timestamp}");
                Debug.Log($"  Engagement: {post.likes} likes, {post.comments} comments");
                Debug.Log($"  Flags: {(post.isGreenFlag ? "Green Flag" : "")} {(post.isRedFlag ? "Red Flag" : "")}");

                // Related traits
                if (post.relatedInterests != null && post.relatedInterests.Length > 0)
                {
                    Debug.Log($"  Related Interests: {string.Join(", ", System.Array.ConvertAll(post.relatedInterests, i => i != null ? i.displayName : "null"))}");
                }
                if (post.relatedPersonalityTraits != null && post.relatedPersonalityTraits.Length > 0)
                {
                    Debug.Log($"  Related Personality: {string.Join(", ", System.Array.ConvertAll(post.relatedPersonalityTraits, p => p != null ? p.displayName : "null"))}");
                }
                if (post.relatedLifestyleTraits != null && post.relatedLifestyleTraits.Length > 0)
                {
                    Debug.Log($"  Related Lifestyle: {string.Join(", ", System.Array.ConvertAll(post.relatedLifestyleTraits, l => l != null ? l.displayName : "null"))}");
                }
            }
            Debug.Log("");

            Debug.Log("=== TEST COMPLETE ===");
        }
    }
}
