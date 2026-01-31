# Random Post System - Implementation Design

## Status: APPROVED - Ready for Implementation

## Overview
System that assigns random posts from a global pool to candidates, with trait-based weighted matching and session-based uniqueness tracking.

---

## Design Decisions (Confirmed)

| Decision | Choice |
|----------|--------|
| Post pool source | Global pool (`RandomPosts.json`) |
| Uniqueness | Post can only be used once per quest session |
| Trait matching | 90% weighted by trait overlap, 10% wild card |
| Engagement | Generated from friends count range |
| Days since posted | Generated at runtime to interleave |
| Reset timing | When starting new quest/level |
| Post types | Photo + TextOnly only (configurable weights) |

---

## Files to Modify

### 1. `Assets/Scripts/Data/CandidateProfileSO.cs`

**Current:**
```csharp
public int randomPostCount = 5;
public string[] randomPostTagFilter;
```

**Change to:**
```csharp
[Header("Random Post Settings")]
[Tooltip("Minimum random posts to add")]
public int randomPostMin = 2;

[Tooltip("Maximum random posts to add")]
public int randomPostMax = 5;

[Header("Social Metrics")]
[Tooltip("Minimum friends count (affects engagement)")]
public int friendsCountMin = 100;

[Tooltip("Maximum friends count (affects engagement)")]
public int friendsCountMax = 500;
```

Remove `randomPostTagFilter` entirely.

---

### 2. `Assets/Scripts/Editor/ScriptableObjectImporter.cs`

**Add to CandidateData class:**
```csharp
public int[] randomPostRange;      // [min, max]
public int[] friendsCountRange;    // [min, max]
// Remove: public int randomPostCount;
// Remove: public string[] randomPostTagFilter;
```

**Add new method `ImportRandomPosts()`:**
- Load from `JSONData/RandomPosts.json`
- Create `RandomPostPoolSO` asset at `Assets/Resources/GameData/PostPool/RandomPostPool.asset`
- Resolve trait references for each post

**Update `ImportAllData()` order:**
1. Import Interests
2. Import Personality Traits
3. Import Lifestyle Traits
4. Resolve trait references
5. **Import Random Posts** ← NEW
6. Import Candidates

**Update `ImportCandidates()`:**
```csharp
asset.randomPostMin = candidateData.randomPostRange[0];
asset.randomPostMax = candidateData.randomPostRange[1];
asset.friendsCountMin = candidateData.friendsCountRange[0];
asset.friendsCountMax = candidateData.friendsCountRange[1];
```

**Add new data class:**
```csharp
[System.Serializable]
public class RandomPostDataList
{
    public PostData[] posts;
}
```

**Update folder creation:**
```csharp
CreateFolderIfNeeded("Assets/Resources/GameData/PostPool");
```

---

### 3. `JSONData/Candidates.json`

**Update each candidate:**

Before:
```json
{
  "randomPostCount": 5,
  "randomPostTagFilter": []
}
```

After:
```json
{
  "randomPostRange": [2, 5],
  "friendsCountRange": [100, 500]
}
```

---

## Files to Create

### 4. `Assets/Scripts/Data/RandomPostPoolSO.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace Maskhot.Data
{
    [CreateAssetMenu(fileName = "RandomPostPool", menuName = "Maskhot/Random Post Pool")]
    public class RandomPostPoolSO : ScriptableObject
    {
        [Tooltip("Global pool of random posts")]
        public List<SocialMediaPost> posts = new List<SocialMediaPost>();
    }
}
```

---

### 5. `JSONData/RandomPosts.json`

Same structure as guaranteed posts in Candidates.json:
```json
{
  "posts": [
    {
      "postType": "Photo",
      "content": "Beautiful day for a walk in the park!",
      "relatedInterests": ["Hiking", "Photography"],
      "relatedPersonalityTraits": ["Optimistic"],
      "relatedLifestyleTraits": ["FitnessFocused"],
      "isGreenFlag": true,
      "isRedFlag": false
    },
    {
      "postType": "TextOnly",
      "content": "Just finished an amazing book. Any recommendations?",
      "relatedInterests": ["Reading"],
      "relatedPersonalityTraits": ["Thoughtful"],
      "relatedLifestyleTraits": [],
      "isGreenFlag": false,
      "isRedFlag": false
    }
  ]
}
```

---

### 6. `Assets/Scripts/Managers/PostPoolManager.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Maskhot.Data;

namespace Maskhot.Managers
{
    public class PostPoolManager : MonoBehaviour
    {
        public static PostPoolManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Chance for a post to ignore trait matching (wild card)")]
        [Range(0f, 1f)] public float wildCardChance = 0.1f;

        [Tooltip("Weight for Photo posts in selection")]
        [Range(0f, 1f)] public float photoWeight = 0.7f;

        [Header("Debug")]
        public bool verboseLogging = false;

        // Runtime state
        private RandomPostPoolSO postPool;
        private HashSet<int> usedPostIndices = new HashSet<int>();

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

        private void LoadPostPool()
        {
            postPool = Resources.Load<RandomPostPoolSO>("GameData/PostPool/RandomPostPool");
            if (postPool == null)
            {
                Debug.LogWarning("PostPoolManager: RandomPostPool not found!");
            }
            else
            {
                Debug.Log($"PostPoolManager: Loaded {postPool.posts.Count} random posts");
            }
        }

        /// <summary>
        /// Call when starting a new quest to reset the pool
        /// </summary>
        public void ResetPool()
        {
            usedPostIndices.Clear();
            if (verboseLogging)
                Debug.Log("PostPoolManager: Pool reset");
        }

        /// <summary>
        /// Get random posts for a candidate
        /// </summary>
        public List<SocialMediaPost> GetRandomPostsForCandidate(CandidateProfileSO candidate)
        {
            var result = new List<SocialMediaPost>();

            if (postPool == null || postPool.posts.Count == 0)
                return result;

            int postCount = Random.Range(candidate.randomPostMin, candidate.randomPostMax + 1);
            int friendsCount = Random.Range(candidate.friendsCountMin, candidate.friendsCountMax + 1);

            for (int i = 0; i < postCount; i++)
            {
                bool isWildCard = Random.value < wildCardChance;
                var post = SelectPost(candidate, isWildCard);

                if (post != null)
                {
                    // Clone the post to avoid modifying the original
                    var clonedPost = ClonePost(post);
                    GenerateEngagement(clonedPost, friendsCount);
                    clonedPost.daysSincePosted = GenerateDaysSincePosted(
                        candidate.guaranteedPosts, i, postCount);
                    result.Add(clonedPost);
                }
            }

            if (verboseLogging)
                Debug.Log($"PostPoolManager: Generated {result.Count} posts for {candidate.profile.characterName}");

            return result;
        }

        private SocialMediaPost SelectPost(CandidateProfileSO candidate, bool isWildCard)
        {
            // Get available posts (not used, correct type)
            var available = new List<(int index, SocialMediaPost post, int score)>();

            for (int i = 0; i < postPool.posts.Count; i++)
            {
                if (usedPostIndices.Contains(i))
                    continue;

                var post = postPool.posts[i];

                // Filter by type (Photo or TextOnly only)
                if (post.postType != PostType.Photo && post.postType != PostType.TextOnly)
                    continue;

                int score = isWildCard ? 1 : CalculateTraitMatchScore(post, candidate);
                available.Add((i, post, score));
            }

            if (available.Count == 0)
                return null;

            // Apply type weighting
            float textWeight = 1f - photoWeight;
            var weighted = available.Select(a => {
                float typeWeight = a.post.postType == PostType.Photo ? photoWeight : textWeight;
                return (a.index, a.post, weight: a.score * typeWeight);
            }).ToList();

            // Weighted random selection
            float totalWeight = weighted.Sum(w => w.weight);
            float roll = Random.value * totalWeight;
            float cumulative = 0;

            foreach (var item in weighted)
            {
                cumulative += item.weight;
                if (roll <= cumulative)
                {
                    usedPostIndices.Add(item.index);
                    return item.post;
                }
            }

            // Fallback
            var fallback = weighted.First();
            usedPostIndices.Add(fallback.index);
            return fallback.post;
        }

        private int CalculateTraitMatchScore(SocialMediaPost post, CandidateProfileSO candidate)
        {
            int score = 1; // Base score

            var profile = candidate.profile;

            // Check interest overlap
            if (post.relatedInterests != null && profile.interests != null)
            {
                foreach (var interest in post.relatedInterests)
                {
                    if (profile.interests.Contains(interest))
                        score += interest.matchWeight;
                }
            }

            // Check personality trait overlap
            if (post.relatedPersonalityTraits != null && profile.personalityTraits != null)
            {
                foreach (var trait in post.relatedPersonalityTraits)
                {
                    if (profile.personalityTraits.Contains(trait))
                        score += trait.matchWeight;
                }
            }

            // Check lifestyle trait overlap
            if (post.relatedLifestyleTraits != null && profile.lifestyleTraits != null)
            {
                foreach (var trait in post.relatedLifestyleTraits)
                {
                    if (profile.lifestyleTraits.Contains(trait))
                        score += trait.matchWeight;
                }
            }

            return score;
        }

        private void GenerateEngagement(SocialMediaPost post, int friendsCount)
        {
            // Likes: 10-30% of friends
            float likeRate = Random.Range(0.1f, 0.3f);
            post.likes = Mathf.RoundToInt(friendsCount * likeRate);

            // Comments: 1-5% of friends
            float commentRate = Random.Range(0.01f, 0.05f);
            post.comments = Mathf.RoundToInt(friendsCount * commentRate);
        }

        private int GenerateDaysSincePosted(List<SocialMediaPost> guaranteedPosts, int index, int totalRandomPosts)
        {
            // Spread random posts across 1-30 days
            // Try to interleave with guaranteed posts
            int maxDays = 30;
            int minDays = 1;

            // Simple approach: distribute evenly across the range
            float step = (float)(maxDays - minDays) / (totalRandomPosts + 1);
            return Mathf.RoundToInt(minDays + step * (index + 1));
        }

        private SocialMediaPost ClonePost(SocialMediaPost original)
        {
            return new SocialMediaPost
            {
                postType = original.postType,
                content = original.content,
                postImage = original.postImage,
                relatedInterests = original.relatedInterests,
                relatedPersonalityTraits = original.relatedPersonalityTraits,
                relatedLifestyleTraits = original.relatedLifestyleTraits,
                isRedFlag = original.isRedFlag,
                isGreenFlag = original.isGreenFlag
                // likes, comments, daysSincePosted will be generated
            };
        }
    }
}
```

---

## Implementation Order

1. ☐ Create `RandomPostPoolSO.cs`
2. ☐ Create `RandomPosts.json` with 15-20 sample posts
3. ☐ Update `CandidateProfileSO.cs` with new fields
4. ☐ Update `ScriptableObjectImporter.cs`:
   - Update CandidateData class
   - Add ImportRandomPosts method
   - Update ImportCandidates to use new fields
   - Add folder creation for PostPool
5. ☐ Update `Candidates.json` with new range format
6. ☐ Create `PostPoolManager.cs`
7. ☐ Update `PROJECT_SETUP.md` and `JSONData/README.md`
8. ☐ Test: Import data, verify pool created
9. ☐ Test: Runtime post generation

---

## Verification Steps

1. Delete old assets, run import
2. Check `Assets/Resources/GameData/PostPool/RandomPostPool.asset` exists
3. Add PostPoolManager to scene
4. Enable verbose logging
5. Test `GetRandomPostsForCandidate()` returns posts with engagement
6. Test uniqueness across multiple candidates
7. Test `ResetPool()` makes posts available again
