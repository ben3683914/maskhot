using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Maskhot.Data;

namespace Maskhot.Managers
{
    /// <summary>
    /// Singleton manager that handles random post selection from a global pool.
    /// Posts are selected based on trait matching with candidates, with uniqueness
    /// tracking to ensure no post is used twice in a session.
    /// </summary>
    public class PostPoolManager : MonoBehaviour
    {
        public static PostPoolManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Chance for a post to be selected randomly (ignoring trait matching)")]
        [Range(0f, 1f)]
        public float wildCardChance = 0.1f;

        [Tooltip("Weight for Photo posts in selection (vs TextOnly)")]
        [Range(0f, 1f)]
        public float photoWeight = 0.1f;

        [Tooltip("Enable detailed logging of post selection")]
        public bool verboseLogging = false;

        [Header("Engagement Generation")]
        [Tooltip("Base engagement multiplier (likes = friendsCount * multiplier * random factor)")]
        [Range(0.01f, 0.5f)]
        public float baseEngagementMultiplier = 0.1f;

        [Tooltip("Comment ratio relative to likes")]
        [Range(0.01f, 0.3f)]
        public float commentToLikeRatio = 0.1f;

        // Runtime state
        private RandomPostPoolSO postPool;
        private HashSet<int> usedPostIndices = new HashSet<int>();
        private System.Random rng = new System.Random();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPostPool();
        }

        /// <summary>
        /// Loads the RandomPostPool from the Resources folder.
        /// </summary>
        private void LoadPostPool()
        {
            postPool = Resources.Load<RandomPostPoolSO>("GameData/PostPool/RandomPostPool");

            if (postPool == null)
            {
                Debug.LogWarning("PostPoolManager: RandomPostPool not found at Resources/GameData/PostPool/RandomPostPool. Random posts will not be available.");
                return;
            }

            if (verboseLogging)
            {
                Debug.Log($"PostPoolManager: Loaded {postPool.posts.Count} posts from pool");
            }
        }

        /// <summary>
        /// Resets the used post tracking. Call this when starting a new quest/level.
        /// </summary>
        public void ResetPool()
        {
            usedPostIndices.Clear();

            if (verboseLogging)
            {
                Debug.Log("PostPoolManager: Pool reset - all posts available again");
            }
        }

        /// <summary>
        /// Returns the number of posts still available in the pool.
        /// </summary>
        public int AvailablePostCount
        {
            get
            {
                if (postPool == null) return 0;
                return postPool.posts.Count - usedPostIndices.Count;
            }
        }

        /// <summary>
        /// Gets random posts for a candidate based on their traits and configured ranges.
        /// </summary>
        /// <param name="candidate">The candidate profile to generate posts for</param>
        /// <returns>List of SocialMediaPost with generated engagement and timestamps</returns>
        public List<SocialMediaPost> GetRandomPostsForCandidate(CandidateProfileSO candidate)
        {
            if (postPool == null || postPool.posts.Count == 0)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("PostPoolManager: No post pool available");
                }
                return new List<SocialMediaPost>();
            }

            // Calculate how many posts to generate
            int postCount = CalculatePostCount(candidate);

            // Generate friends count for engagement calculation
            int friendsCount = GenerateFriendsCount(candidate);

            if (verboseLogging)
            {
                Debug.Log($"PostPoolManager: Generating {postCount} posts for {candidate.profile.characterName} (friends: {friendsCount})");
            }

            List<SocialMediaPost> selectedPosts = new List<SocialMediaPost>();

            // Get the maximum days since posted from guaranteed posts to interleave properly
            int maxGuaranteedDays = candidate.guaranteedPosts.Count > 0
                ? candidate.guaranteedPosts.Max(p => p.daysSincePosted)
                : 7;

            for (int i = 0; i < postCount; i++)
            {
                if (AvailablePostCount == 0)
                {
                    if (verboseLogging)
                    {
                        Debug.LogWarning("PostPoolManager: Pool exhausted, cannot select more posts");
                    }
                    break;
                }

                // Determine if this is a wild card selection
                bool isWildCard = rng.NextDouble() < wildCardChance;

                // Select a post
                SocialMediaPost post = SelectPost(candidate, isWildCard);

                if (post != null)
                {
                    // Clone the post so we can modify it without affecting the pool
                    SocialMediaPost clonedPost = ClonePost(post);

                    // Generate engagement
                    GenerateEngagement(clonedPost, friendsCount);

                    // Generate days since posted
                    clonedPost.daysSincePosted = GenerateDaysSincePosted(maxGuaranteedDays, i, postCount);

                    selectedPosts.Add(clonedPost);

                    if (verboseLogging)
                    {
                        string selectionType = isWildCard ? "WILD CARD" : "trait-matched";
                        Debug.Log($"  [{i + 1}] {selectionType}: \"{TruncateContent(clonedPost.content)}\" " +
                                  $"(likes: {clonedPost.likes}, days: {clonedPost.daysSincePosted})");
                    }
                }
            }

            return selectedPosts;
        }

        /// <summary>
        /// Calculates how many random posts to generate for a candidate.
        /// </summary>
        private int CalculatePostCount(CandidateProfileSO candidate)
        {
            int min = Mathf.Max(0, candidate.randomPostMin);
            int max = Mathf.Max(min, candidate.randomPostMax);

            // Don't request more than available
            max = Mathf.Min(max, AvailablePostCount);
            min = Mathf.Min(min, max);

            return rng.Next(min, max + 1);
        }

        /// <summary>
        /// Generates a friends count for this candidate's session.
        /// </summary>
        private int GenerateFriendsCount(CandidateProfileSO candidate)
        {
            int min = Mathf.Max(1, candidate.friendsCountMin);
            int max = Mathf.Max(min, candidate.friendsCountMax);
            return rng.Next(min, max + 1);
        }

        /// <summary>
        /// Selects a post from the pool, either randomly or based on trait matching.
        /// </summary>
        private SocialMediaPost SelectPost(CandidateProfileSO candidate, bool isWildCard)
        {
            // Get available posts (not yet used)
            var availableIndices = Enumerable.Range(0, postPool.posts.Count)
                .Where(i => !usedPostIndices.Contains(i))
                .ToList();

            if (availableIndices.Count == 0)
            {
                return null;
            }

            // Filter by post type (Photo or TextOnly only)
            var filteredIndices = availableIndices
                .Where(i => postPool.posts[i].postType == PostType.Photo ||
                           postPool.posts[i].postType == PostType.TextOnly)
                .ToList();

            if (filteredIndices.Count == 0)
            {
                // Fall back to all available if no Photo/TextOnly posts
                filteredIndices = availableIndices;
            }

            // Apply post type weighting
            filteredIndices = ApplyPostTypeWeighting(filteredIndices);

            int selectedIndex;

            if (isWildCard)
            {
                // Wild card: random selection from available
                selectedIndex = filteredIndices[rng.Next(filteredIndices.Count)];
            }
            else
            {
                // Trait-matched: weighted selection based on trait overlap
                selectedIndex = SelectByTraitMatch(filteredIndices, candidate);
            }

            // Mark as used
            usedPostIndices.Add(selectedIndex);

            return postPool.posts[selectedIndex];
        }

        /// <summary>
        /// Applies post type weighting to favor Photo or TextOnly posts.
        /// </summary>
        private List<int> ApplyPostTypeWeighting(List<int> indices)
        {
            // Separate by type
            var photoIndices = indices.Where(i => postPool.posts[i].postType == PostType.Photo).ToList();
            var textIndices = indices.Where(i => postPool.posts[i].postType == PostType.TextOnly).ToList();
            var otherIndices = indices.Where(i =>
                postPool.posts[i].postType != PostType.Photo &&
                postPool.posts[i].postType != PostType.TextOnly).ToList();

            // If one type is empty, use the other
            if (photoIndices.Count == 0) return textIndices.Count > 0 ? textIndices : otherIndices;
            if (textIndices.Count == 0) return photoIndices;

            // Weighted selection between photo and text
            if (rng.NextDouble() < photoWeight)
            {
                return photoIndices;
            }
            else
            {
                return textIndices;
            }
        }

        /// <summary>
        /// Selects a post based on trait matching score with the candidate.
        /// </summary>
        private int SelectByTraitMatch(List<int> indices, CandidateProfileSO candidate)
        {
            if (indices.Count == 0) return -1;

            // Calculate scores for each available post
            var scoredPosts = indices
                .Select(i => new { Index = i, Score = CalculateTraitMatchScore(postPool.posts[i], candidate) })
                .ToList();

            // Get max score for normalization
            int maxScore = scoredPosts.Max(p => p.Score);

            if (maxScore == 0)
            {
                // No trait matches, pick randomly
                return indices[rng.Next(indices.Count)];
            }

            // Weighted random selection based on scores
            // Add 1 to all scores so even 0-score posts have some chance
            int totalWeight = scoredPosts.Sum(p => p.Score + 1);
            int randomValue = rng.Next(totalWeight);

            int cumulative = 0;
            foreach (var sp in scoredPosts)
            {
                cumulative += sp.Score + 1;
                if (randomValue < cumulative)
                {
                    return sp.Index;
                }
            }

            // Fallback
            return indices[rng.Next(indices.Count)];
        }

        /// <summary>
        /// Calculates how well a post matches a candidate's traits.
        /// </summary>
        private int CalculateTraitMatchScore(SocialMediaPost post, CandidateProfileSO candidate)
        {
            int score = 0;
            var profile = candidate.profile;

            // Check interest matches
            if (post.relatedInterests != null && profile.interests != null)
            {
                foreach (var interest in post.relatedInterests)
                {
                    if (interest != null && profile.interests.Contains(interest))
                    {
                        score += interest.matchWeight;
                    }
                }
            }

            // Check personality trait matches
            if (post.relatedPersonalityTraits != null && profile.personalityTraits != null)
            {
                foreach (var trait in post.relatedPersonalityTraits)
                {
                    if (trait != null && profile.personalityTraits.Contains(trait))
                    {
                        score += trait.matchWeight;
                    }
                }
            }

            // Check lifestyle trait matches
            if (post.relatedLifestyleTraits != null && profile.lifestyleTraits != null)
            {
                foreach (var trait in post.relatedLifestyleTraits)
                {
                    if (trait != null && profile.lifestyleTraits.Contains(trait))
                    {
                        score += trait.matchWeight;
                    }
                }
            }

            return score;
        }

        /// <summary>
        /// Generates engagement (likes/comments) for a post based on friends count.
        /// </summary>
        private void GenerateEngagement(SocialMediaPost post, int friendsCount)
        {
            // Base likes calculation with randomness
            float randomFactor = 0.5f + (float)rng.NextDouble(); // 0.5 to 1.5
            int baseLikes = Mathf.RoundToInt(friendsCount * baseEngagementMultiplier * randomFactor);

            // Green flag posts get more engagement
            if (post.isGreenFlag)
            {
                baseLikes = Mathf.RoundToInt(baseLikes * 1.3f);
            }

            // Red flag posts might get more or less (polarizing)
            if (post.isRedFlag)
            {
                float redFlagMultiplier = 0.5f + (float)rng.NextDouble() * 1.5f; // 0.5 to 2.0
                baseLikes = Mathf.RoundToInt(baseLikes * redFlagMultiplier);
            }

            // Photo posts get more engagement than text
            if (post.postType == PostType.Photo)
            {
                baseLikes = Mathf.RoundToInt(baseLikes * 1.2f);
            }

            post.likes = Mathf.Max(1, baseLikes);

            // Comments based on likes
            float commentRandomFactor = 0.5f + (float)rng.NextDouble();
            post.comments = Mathf.Max(0, Mathf.RoundToInt(post.likes * commentToLikeRatio * commentRandomFactor));
        }

        /// <summary>
        /// Generates a daysSincePosted value to interleave with guaranteed posts.
        /// </summary>
        private int GenerateDaysSincePosted(int maxGuaranteedDays, int index, int totalCount)
        {
            // Spread posts across a reasonable time range
            // Random posts can be older than guaranteed posts (up to 30 days)
            int maxDays = Mathf.Max(maxGuaranteedDays + 14, 30);

            // Distribute posts somewhat evenly with randomness
            float basePosition = (float)(index + 1) / (totalCount + 1);
            int baseDays = Mathf.RoundToInt(basePosition * maxDays);

            // Add some randomness (+/- 3 days)
            int randomOffset = rng.Next(-3, 4);
            int days = Mathf.Clamp(baseDays + randomOffset, 1, maxDays);

            return days;
        }

        /// <summary>
        /// Creates a copy of a post so we can modify it without affecting the pool.
        /// </summary>
        private SocialMediaPost ClonePost(SocialMediaPost original)
        {
            return new SocialMediaPost
            {
                postType = original.postType,
                content = original.content,
                postImage = original.postImage,
                daysSincePosted = original.daysSincePosted,
                likes = original.likes,
                comments = original.comments,
                relatedInterests = original.relatedInterests,
                relatedPersonalityTraits = original.relatedPersonalityTraits,
                relatedLifestyleTraits = original.relatedLifestyleTraits,
                isRedFlag = original.isRedFlag,
                isGreenFlag = original.isGreenFlag
            };
        }

        /// <summary>
        /// Truncates content for logging purposes.
        /// </summary>
        private string TruncateContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "(no content)";
            return content.Length > 50 ? content.Substring(0, 47) + "..." : content;
        }
    }
}
