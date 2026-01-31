using UnityEngine;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject representing a personality trait
    /// Used for matching logic and character definition
    /// </summary>
    [CreateAssetMenu(fileName = "NewPersonalityTrait", menuName = "Maskhot/Traits/Personality Trait")]
    public class PersonalityTraitSO : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Display name shown in UI (e.g., 'Outgoing', 'Introverted')")]
        public string displayName;

        [Tooltip("Icon for this trait (optional)")]
        public Sprite icon;

        [Tooltip("Brief description of this trait")]
        [TextArea(2, 3)]
        public string description;

        [Header("Trait Properties")]
        [Tooltip("Is this generally considered a positive trait?")]
        public bool isPositiveTrait = true;

        [Tooltip("Traits that are opposite/incompatible with this one")]
        public PersonalityTraitSO[] oppositeTraits;

        [Tooltip("Traits that complement this one well")]
        public PersonalityTraitSO[] complementaryTraits;

        [Header("Matching Logic")]
        [Tooltip("Weight for matching calculations")]
        [Range(1, 10)]
        public int matchWeight = 5;
    }
}
