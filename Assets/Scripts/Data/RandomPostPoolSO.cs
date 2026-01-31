using UnityEngine;
using System.Collections.Generic;

namespace Maskhot.Data
{
    /// <summary>
    /// ScriptableObject that holds a global pool of random posts.
    /// Posts are selected from this pool based on trait matching with candidates.
    /// </summary>
    [CreateAssetMenu(fileName = "RandomPostPool", menuName = "Maskhot/Random Post Pool")]
    public class RandomPostPoolSO : ScriptableObject
    {
        [Tooltip("Global pool of random posts that can be assigned to any candidate")]
        public List<SocialMediaPost> posts = new List<SocialMediaPost>();
    }
}
