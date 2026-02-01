using UnityEngine;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject that holds a curated client profile
    /// Used for hand-crafted story characters (Emma, Marcus, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "NewClientProfile", menuName = "Maskhot/Client Profile")]
    public class ClientProfileSO : ScriptableObject
    {
        [Header("Client Profile Data")]
        [Tooltip("Core client information")]
        public ClientProfile profile;

        [Header("Quest Metadata")]
        [Tooltip("Is this client part of the main story?")]
        public bool isStoryClient = true;

        [Tooltip("When does this client appear? (level/day number, 0 = any)")]
        public int suggestedLevel = 0;

        [Header("Quest Content")]
        [Tooltip("What the client says when introducing their request")]
        [TextArea(3, 6)]
        public string introduction;

        [Tooltip("What the client is looking for in a match")]
        public MatchCriteria matchCriteria;
    }
}
