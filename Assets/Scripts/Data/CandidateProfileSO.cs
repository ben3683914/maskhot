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

        [Tooltip("Maximum number of random posts to add (in addition to guaranteed posts)")]
        [Range(0, 20)]
        public int randomPostCount = 5;

        [Header("Randomization Rules")]
        [Tooltip("Specific tags that random posts must match (optional, leave empty for any)")]
        public string[] randomPostTagFilter;

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
