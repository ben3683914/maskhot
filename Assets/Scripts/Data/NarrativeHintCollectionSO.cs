using UnityEngine;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject that holds a collection of narrative hints for quest requirements
    /// Groups related hints together (e.g., all food-related hints, travel-related hints)
    /// One hint will be randomly selected at runtime to show the player
    /// </summary>
    [CreateAssetMenu(fileName = "NewNarrativeHintCollection", menuName = "Maskhot/Narrative Hint Collection")]
    public class NarrativeHintCollectionSO : ScriptableObject
    {
        [Header("Related Traits")]
        [Tooltip("Interests this hint collection maps to (e.g., Cooking interest for food-related hints)")]
        public InterestSO[] relatedInterests;

        [Tooltip("Personality traits this hint collection maps to (e.g., Creative trait)")]
        public PersonalityTraitSO[] relatedPersonalityTraits;

        [Tooltip("Lifestyle traits this hint collection maps to (e.g., Early Riser for morning hints)")]
        public LifestyleTraitSO[] relatedLifestyleTraits;

        [Header("Hints")]
        [Tooltip("Collection of narrative hints - one will be randomly selected at runtime")]
        [TextArea(1, 2)]
        public string[] hints;

        /// <summary>
        /// Returns a random hint from this collection
        /// </summary>
        public string GetRandomHint()
        {
            if (hints == null || hints.Length == 0)
            {
                Debug.LogWarning($"NarrativeHintCollection '{name}' has no hints!");
                return "???";
            }

            return hints[Random.Range(0, hints.Length)];
        }
    }
}
