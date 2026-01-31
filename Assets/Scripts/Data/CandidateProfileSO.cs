using UnityEngine;
using System.Collections.Generic;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject that holds a complete candidate profile
    /// Combines static profile data with guaranteed posts and randomization rules
    /// </summary>
    [CreateAssetMenu(fileName = "NewCandidateProfile", menuName = "Maskhot/Candidate Profile")]
    public class CandidateProfileSO : ScriptableObject
    {
        [Header("Static Profile Data")]
        [Tooltip("Core profile information (always consistent)")]
        public CandidateProfile profile;

        [Header("Social Media Posts")]
        [Tooltip("Posts that ALWAYS appear for this character")]
        public List<SocialMediaPost> guaranteedPosts = new List<SocialMediaPost>();

        [Header("Random Post Settings")]
        [Tooltip("Minimum number of random posts to add")]
        [Range(0, 20)]
        public int randomPostMin = 2;

        [Tooltip("Maximum number of random posts to add")]
        [Range(0, 20)]
        public int randomPostMax = 5;

        [Header("Social Metrics")]
        [Tooltip("Minimum friends count (affects post engagement)")]
        public int friendsCountMin = 100;

        [Tooltip("Maximum friends count (affects post engagement)")]
        public int friendsCountMax = 500;

        [Header("Debug")]
        [Tooltip("Unique ID for this profile (auto-generated)")]
        public string profileId;

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(profileId))
            {
                profileId = System.Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Gets all posts for this profile (guaranteed + random selection)
        /// This will be called by the ProfileManager at runtime
        /// </summary>
        public List<SocialMediaPost> GetPostsForPlaythrough()
        {
            // TODO: Implement random post selection from pool
            // For now, just return guaranteed posts
            return new List<SocialMediaPost>(guaranteedPosts);
        }
    }
}
