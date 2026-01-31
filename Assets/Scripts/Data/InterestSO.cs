using UnityEngine;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject representing an interest/hobby
    /// Used for matching logic and UI display
    /// </summary>
    [CreateAssetMenu(fileName = "NewInterest", menuName = "Maskhot/Traits/Interest")]
    public class InterestSO : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Display name shown in UI")]
        public string displayName;

        [Tooltip("Icon for this interest (optional)")]
        public Sprite icon;

        [Tooltip("Brief description")]
        [TextArea(2, 3)]
        public string description;

        [Header("Categorization")]
        [Tooltip("Category this interest belongs to")]
        public InterestCategory category;

        [Header("Matching Logic")]
        [Tooltip("How important this interest is for matching (higher = more important)")]
        [Range(1, 10)]
        public int matchWeight = 5;

        [Tooltip("Interests that are closely related to this one")]
        public InterestSO[] relatedInterests;
    }

    public enum InterestCategory
    {
        Outdoor,
        Indoor,
        Creative,
        Athletic,
        Social,
        Intellectual,
        Entertainment,
        Food,
        Travel,
        Technology,
        Animals,
        Other
    }
}
