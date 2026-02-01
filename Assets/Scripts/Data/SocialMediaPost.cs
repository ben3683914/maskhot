using UnityEngine;
using System;

namespace Maskhot.Data
{
    /// <summary>
    /// Represents a single social media post (photo, text, story, etc.)
    /// Can be assigned directly to a profile or pulled from a randomized pool
    /// </summary>
    [Serializable]
    public class SocialMediaPost
    {
        [Header("Post Content")]
        [Tooltip("Type of post (photo, text, video, story, etc.)")]
        public PostType postType;

        [Tooltip("Main text content of the post")]
        [TextArea(3, 6)]
        public string content;

        [Tooltip("Image/photo for this post (if applicable)")]
        public Sprite postImage;

        private static Sprite s_BeachImage;
        private static Sprite s_DogImage;
        private static Sprite s_SantoriniImage;

        /// <summary>
        /// Returns the post image for Photo posts only. Returns null for other post types.
        /// Intelligently selects an image based on post content/traits, falling back to Beach.
        /// </summary>
        public Sprite DisplayImage
        {
            get
            {
                if (postType != PostType.Photo)
                    return null;

                if (postImage != null)
                    return postImage;

                // Check content and traits for appropriate image
                string lowerContent = content?.ToLowerInvariant() ?? "";

                // Check for dog-related content
                if (ContainsDogKeywords(lowerContent) || HasDogRelatedTraits())
                {
                    if (s_DogImage == null)
                        s_DogImage = Resources.Load<Sprite>("Sprites/Posts/Dog");
                    return s_DogImage;
                }

                // Check for travel/sunset-related content
                if (ContainsTravelKeywords(lowerContent) || HasTravelRelatedTraits())
                {
                    if (s_SantoriniImage == null)
                        s_SantoriniImage = Resources.Load<Sprite>("Sprites/Posts/Santorini sunset");
                    return s_SantoriniImage;
                }

                // Default to random image (deterministic based on content)
                return GetRandomDefaultImage();
            }
        }

        private bool ContainsDogKeywords(string lowerContent)
        {
            return lowerContent.Contains("dog") ||
                   lowerContent.Contains("puppy") ||
                   lowerContent.Contains("pup") ||
                   lowerContent.Contains("canine") ||
                   lowerContent.Contains("pupper") ||
                   lowerContent.Contains("doggo");
        }

        private bool ContainsTravelKeywords(string lowerContent)
        {
            return lowerContent.Contains("travel") ||
                   lowerContent.Contains("sunset") ||
                   lowerContent.Contains("sunrise") ||
                   lowerContent.Contains("santorini") ||
                   lowerContent.Contains("greece") ||
                   lowerContent.Contains("vacation") ||
                   lowerContent.Contains("holiday") ||
                   lowerContent.Contains("trip") ||
                   lowerContent.Contains("abroad") ||
                   lowerContent.Contains("adventure");
        }

        private bool HasDogRelatedTraits()
        {
            if (relatedInterests != null)
            {
                foreach (var interest in relatedInterests)
                {
                    if (interest != null)
                    {
                        // Check category
                        if (interest.category == InterestCategory.Animals)
                            return true;

                        // Check display name
                        string name = interest.displayName?.ToLowerInvariant() ?? "";
                        if (name.Contains("dog") || name.Contains("pet") || name.Contains("animal"))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool HasTravelRelatedTraits()
        {
            if (relatedInterests != null)
            {
                foreach (var interest in relatedInterests)
                {
                    if (interest != null)
                    {
                        // Check category
                        if (interest.category == InterestCategory.Travel || interest.category == InterestCategory.Outdoor)
                            return true;

                        // Check display name
                        string name = interest.displayName?.ToLowerInvariant() ?? "";
                        if (name.Contains("travel") || name.Contains("adventure") || name.Contains("outdoor"))
                            return true;
                    }
                }
            }
            return false;
        }

        private Sprite GetRandomDefaultImage()
        {
            // Load all images if needed
            if (s_BeachImage == null)
                s_BeachImage = Resources.Load<Sprite>("Sprites/Posts/Beach");
            if (s_DogImage == null)
                s_DogImage = Resources.Load<Sprite>("Sprites/Posts/Dog");
            if (s_SantoriniImage == null)
                s_SantoriniImage = Resources.Load<Sprite>("Sprites/Posts/Santorini sunset");

            // Use content hash for deterministic "random" selection
            int hash = content?.GetHashCode() ?? 0;
            int index = Mathf.Abs(hash) % 3;

            return index switch
            {
                0 => s_BeachImage,
                1 => s_DogImage,
                _ => s_SantoriniImage
            };
        }

        /// <summary>
        /// Whether this post should display an image (Photo type only).
        /// </summary>
        public bool ShowImage => postType == PostType.Photo;

        [Header("Post Metadata")]
        [Tooltip("Days since posted (1 = yesterday, 7 = a week ago, etc.) - used for sorting")]
        public int daysSincePosted;

        [Tooltip("Number of likes")]
        public int likes;

        [Tooltip("Number of comments")]
        public int comments;

        [Header("Matching Logic - Trait Associations")]
        [Tooltip("Interests this post relates to (for random post matching)")]
        public InterestSO[] relatedInterests;

        [Tooltip("Personality traits this post demonstrates (for random post matching)")]
        public PersonalityTraitSO[] relatedPersonalityTraits;

        [Tooltip("Lifestyle traits this post reflects (for random post matching)")]
        public LifestyleTraitSO[] relatedLifestyleTraits;

        [Header("Post Flags")]
        [Tooltip("Is this a red flag post?")]
        public bool isRedFlag;

        [Tooltip("Is this a green flag post?")]
        public bool isGreenFlag;

        /// <summary>
        /// Generates a redacted version of this post's content.
        /// Replaces non-whitespace characters with block characters (█),
        /// preserving spaces and newlines for that "government document" look.
        /// </summary>
        /// <returns>The redacted text string</returns>
        public string GetRedactedContent()
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            var result = new char[content.Length];
            for (int i = 0; i < content.Length; i++)
            {
                // Preserve whitespace (spaces, newlines, tabs), redact everything else
                result[i] = char.IsWhiteSpace(content[i]) ? content[i] : '█';
            }
            return new string(result);
        }
    }

    [Serializable]
    public enum PostType
    {
        Photo,
        TextOnly,
        Video,
        Story,
        SharedPost,
        Poll
    }
}
