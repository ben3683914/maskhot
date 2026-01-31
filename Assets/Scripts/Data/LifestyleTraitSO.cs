using UnityEngine;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject representing a lifestyle indicator
    /// Describes how a person lives their day-to-day life
    /// </summary>
    [CreateAssetMenu(fileName = "NewLifestyleTrait", menuName = "Maskhot/Traits/Lifestyle Trait")]
    public class LifestyleTraitSO : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Display name (e.g., 'Homebody', 'Night Owl', 'Early Riser')")]
        public string displayName;

        [Tooltip("Icon for this lifestyle trait (optional)")]
        public Sprite icon;

        [Tooltip("Brief description")]
        [TextArea(2, 3)]
        public string description;

        [Header("Categorization")]
        [Tooltip("Category of lifestyle trait")]
        public LifestyleCategory category;

        [Header("Matching Logic")]
        [Tooltip("How important this is for compatibility")]
        [Range(1, 10)]
        public int matchWeight = 5;

        [Tooltip("Lifestyle traits that conflict with this one")]
        public LifestyleTraitSO[] conflictingTraits;

        [Tooltip("Lifestyle traits that work well with this one")]
        public LifestyleTraitSO[] compatibleTraits;
    }

    public enum LifestyleCategory
    {
        Activity_Level,      // Active, Sedentary, Moderate
        Social_Preference,   // Social Butterfly, Homebody, Balanced
        Schedule,           // Night Owl, Early Riser, Flexible
        Living_Situation,   // Urban, Suburban, Rural
        Work_Life,          // Workaholic, Balanced, Relaxed
        Health_Wellness,    // Fitness Focused, Health Conscious, Casual
        Other
    }
}
