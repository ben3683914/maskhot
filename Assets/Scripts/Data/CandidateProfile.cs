using UnityEngine;
using System;

namespace Maskhot.Data
{
    /// <summary>
    /// Core profile data for a candidate (potential match)
    /// This data is consistent across playthroughs
    /// </summary>
    [Serializable]
    public class CandidateProfile
    {
        [Header("Basic Info")]
        [Tooltip("Character's name")]
        public string characterName;

        [Tooltip("Gender")]
        public Gender gender;

        [Tooltip("Age")]
        [Range(18, 50)]
        public int age;

        [Tooltip("Profile picture")]
        public Sprite profilePicture;

        [Tooltip("Bio/description")]
        [TextArea(2, 4)]
        public string bio;

        [Header("Core Characteristics (Always Consistent)")]
        [Tooltip("Personality traits (e.g., Outgoing, Introverted, Adventurous)")]
        public PersonalityTraitSO[] personalityTraits;

        [Tooltip("Core interests and hobbies")]
        public InterestSO[] interests;

        [Tooltip("Lifestyle indicators (e.g., Outdoorsy, Homebody, Party-goer)")]
        public LifestyleTraitSO[] lifestyleTraits;

        [Header("Matching Metadata")]
        [Tooltip("Overall personality archetype (for quest matching)")]
        public PersonalityArchetype archetype;
    }

    [Serializable]
    public enum Gender
    {
        Male,
        Female,
        NonBinary
    }

    [Serializable]
    public enum PersonalityArchetype
    {
        Adventurous,
        Intellectual,
        Creative,
        Athletic,
        Homebody,
        Social,
        Career_Focused,
        Free_Spirit,
        Traditional,
        Quirky
    }
}
