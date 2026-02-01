using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Maskhot.Managers;

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
        [Range(0, 100)]
        public int randomPostMin = 2;

        [Tooltip("Maximum number of random posts to add")]
        [Range(0, 100)]
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

        // Cached default sprites (loaded once)
        private static Sprite cachedMaleSprite;
        private static Sprite cachedFemaleSprite;

        // Cached posts for current playthrough (persists until reset)
        [System.NonSerialized]
        private List<SocialMediaPost> cachedPlaythroughPosts;

        /// <summary>
        /// Gets the profile picture for this candidate.
        /// If no picture is assigned, returns a gender-based default.
        /// NonBinary candidates get a 50/50 random selection between male/female.
        /// </summary>
        public Sprite GetProfilePicture()
        {
            // Return assigned picture if available
            if (profile.profilePicture != null)
            {
                return profile.profilePicture;
            }

            // Load and cache default sprites if needed
            if (cachedMaleSprite == null)
            {
                cachedMaleSprite = Resources.Load<Sprite>("Sprites/Profiles/male");
            }
            if (cachedFemaleSprite == null)
            {
                cachedFemaleSprite = Resources.Load<Sprite>("Sprites/Profiles/female");
            }

            // Return gender-based default
            switch (profile.gender)
            {
                case Gender.Male:
                    return cachedMaleSprite;
                case Gender.Female:
                    return cachedFemaleSprite;
                case Gender.NonBinary:
                    // 50/50 random selection for NonBinary
                    return Random.value < 0.5f ? cachedMaleSprite : cachedFemaleSprite;
                default:
                    return cachedMaleSprite;
            }
        }

        /// <summary>
        /// Gets all posts for this profile (guaranteed + random selection)
        /// Combines guaranteed posts with trait-matched random posts from the pool
        /// Posts are sorted by daysSincePosted (most recent first)
        /// Results are cached - call ResetPlaythroughPosts() to clear for a new session.
        /// </summary>
        public List<SocialMediaPost> GetPostsForPlaythrough()
        {
            // Return cached posts if already generated this session
            if (cachedPlaythroughPosts != null)
            {
                return cachedPlaythroughPosts;
            }

            List<SocialMediaPost> allPosts = new List<SocialMediaPost>(guaranteedPosts);

            // Get random posts from the pool if PostPoolManager is available
            if (PostPoolManager.Instance != null)
            {
                List<SocialMediaPost> randomPosts = PostPoolManager.Instance.GetRandomPostsForCandidate(this);
                allPosts.AddRange(randomPosts);
            }

            // Sort by daysSincePosted (most recent first)
            cachedPlaythroughPosts = allPosts.OrderBy(p => p.daysSincePosted).ToList();

            return cachedPlaythroughPosts;
        }

        /// <summary>
        /// Clears the cached posts for this candidate.
        /// Call when starting a new quest/session to get fresh random posts.
        /// </summary>
        public void ResetPlaythroughPosts()
        {
            cachedPlaythroughPosts = null;
        }
    }
}
