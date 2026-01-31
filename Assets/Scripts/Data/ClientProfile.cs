using UnityEngine;
using System;

namespace Maskhot.Data
{
    /// <summary>
    /// Core profile data for a client (person asking for a match)
    /// Can be used in ScriptableObjects (curated) or created at runtime (procedural)
    /// </summary>
    [Serializable]
    public class ClientProfile
    {
        [Header("Basic Info")]
        [Tooltip("Client's name")]
        public string clientName;

        [Tooltip("Gender")]
        public Gender gender;

        [Tooltip("Age")]
        [Range(18, 50)]
        public int age;

        [Tooltip("Profile picture/avatar")]
        public Sprite profilePicture;

        [Tooltip("Relationship to player (friend, coworker, roommate, etc.)")]
        [TextArea(1, 2)]
        public string relationship;

        [Tooltip("Backstory or context for this client")]
        [TextArea(2, 4)]
        public string backstory;

        [Header("Client's Own Traits")]
        [Tooltip("Client's personality traits (what they're like)")]
        public PersonalityTraitSO[] personalityTraits;

        [Tooltip("Client's interests and hobbies")]
        public InterestSO[] interests;

        [Tooltip("Client's lifestyle indicators")]
        public LifestyleTraitSO[] lifestyleTraits;

        [Header("Metadata")]
        [Tooltip("Overall personality archetype")]
        public PersonalityArchetype archetype;
    }
}
