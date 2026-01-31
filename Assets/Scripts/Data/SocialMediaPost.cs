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

        [Header("Post Metadata")]
        [Tooltip("When was this posted? (relative or absolute)")]
        public string timestamp;

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

        [Tooltip("Categories for organization (personality, hobby, lifestyle, controversial, etc.)")]
        public PostCategory[] categories;
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

    [Serializable]
    public enum PostCategory
    {
        Personality,
        Hobby,
        Lifestyle,
        Political,
        Controversial,
        Wholesome,
        Family,
        Friends,
        Work,
        Travel,
        Food,
        Fitness,
        Pets
    }
}
