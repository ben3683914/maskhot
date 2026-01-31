using UnityEngine;
using System;

namespace Maskhot.Data
{
    /// <summary>
    /// Requirements for matching a candidate to a client
    /// Combines abstract narrative hints with concrete trait requirements
    /// Can be used in ScriptableObjects (curated) or created at runtime (procedural)
    /// </summary>
    [Serializable]
    public class MatchCriteria
    {
        [Header("Basic Preferences")]
        [Tooltip("Acceptable genders for the match")]
        public Gender[] acceptableGenders;

        [Tooltip("Minimum acceptable age")]
        [Range(18, 50)]
        public int minAge = 18;

        [Tooltip("Maximum acceptable age")]
        [Range(18, 50)]
        public int maxAge = 50;

        [Header("Trait Requirements")]
        [Tooltip("Specific trait requirements (abstract hints + concrete traits)")]
        public TraitRequirement[] traitRequirements;

        [Header("Dealbreakers")]
        [Tooltip("Personality traits that are automatic rejections")]
        public PersonalityTraitSO[] dealbreakerPersonalityTraits;

        [Tooltip("Interests that are automatic rejections")]
        public InterestSO[] dealbreakerInterests;

        [Tooltip("Lifestyle traits that are automatic rejections")]
        public LifestyleTraitSO[] dealbreakerLifestyleTraits;

        [Header("Red Flag Tolerance")]
        [Tooltip("Maximum number of red flag posts allowed")]
        [Range(0, 10)]
        public int maxRedFlags = 2;

        [Tooltip("Minimum number of green flag posts required")]
        [Range(0, 10)]
        public int minGreenFlags = 0;

        [Header("Scoring Weights")]
        [Tooltip("How much to weight personality trait matches (0-1)")]
        [Range(0f, 1f)]
        public float personalityWeight = 0.33f;

        [Tooltip("How much to weight interest matches (0-1)")]
        [Range(0f, 1f)]
        public float interestsWeight = 0.33f;

        [Tooltip("How much to weight lifestyle trait matches (0-1)")]
        [Range(0f, 1f)]
        public float lifestyleWeight = 0.34f;
    }

    /// <summary>
    /// A single trait requirement for a quest
    /// Maps abstract narrative hints to concrete trait requirements
    /// </summary>
    [Serializable]
    public class TraitRequirement
    {
        [Header("Player-Facing Hints")]
        [Tooltip("Narrative hint collection - one hint will be randomly selected at runtime")]
        public NarrativeHintCollectionSO narrativeHints;

        [Header("Backend Requirements")]
        [Tooltip("At least ONE of these interests satisfies this requirement")]
        public InterestSO[] acceptableInterests;

        [Tooltip("At least ONE of these personality traits satisfies this requirement")]
        public PersonalityTraitSO[] acceptablePersonalityTraits;

        [Tooltip("At least ONE of these lifestyle traits satisfies this requirement")]
        public LifestyleTraitSO[] acceptableLifestyleTraits;

        [Header("Requirement Level")]
        [Tooltip("How important is this requirement?")]
        public RequirementLevel level = RequirementLevel.Preferred;
    }

    /// <summary>
    /// How critical a requirement is for the match
    /// </summary>
    [Serializable]
    public enum RequirementLevel
    {
        Required,    // Must have - candidate fails without this
        Preferred,   // Nice to have - bonus points if matched
        Avoid        // Should not have - penalty points if matched
    }
}
