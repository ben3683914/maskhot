using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Maskhot.Data;

namespace Maskhot.Managers
{
    /// <summary>
    /// Singleton manager that loads and serves candidate profiles and trait data.
    /// Auto-loads all data from Resources/GameData/ folder at runtime.
    /// </summary>
    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        [Header("Debug Settings")]
        [Tooltip("When enabled, outputs detailed information about all loaded data to the console")]
        public bool verboseLogging = false;

        // Loaded data arrays
        private CandidateProfileSO[] candidateProfiles;
        private InterestSO[] interests;
        private PersonalityTraitSO[] personalityTraits;
        private LifestyleTraitSO[] lifestyleTraits;
        private NarrativeHintCollectionSO[] narrativeHints;

        // Lookup dictionaries for fast access by name
        private Dictionary<string, CandidateProfileSO> candidatesByName;
        private Dictionary<string, InterestSO> interestsByName;
        private Dictionary<string, PersonalityTraitSO> personalityTraitsByName;
        private Dictionary<string, LifestyleTraitSO> lifestyleTraitsByName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllData();
        }

        private void LoadAllData()
        {
            // Load all ScriptableObjects from Resources folder
            candidateProfiles = Resources.LoadAll<CandidateProfileSO>("GameData/Profiles");
            interests = Resources.LoadAll<InterestSO>("GameData/Traits/Interests");
            personalityTraits = Resources.LoadAll<PersonalityTraitSO>("GameData/Traits/Personality");
            lifestyleTraits = Resources.LoadAll<LifestyleTraitSO>("GameData/Traits/Lifestyle");
            narrativeHints = Resources.LoadAll<NarrativeHintCollectionSO>("GameData/NarrativeHints");

            BuildLookupDictionaries();

            Debug.Log($"ProfileManager: Loaded {candidateProfiles.Length} candidates, " +
                      $"{interests.Length} interests, {personalityTraits.Length} personality traits, " +
                      $"{lifestyleTraits.Length} lifestyle traits, {narrativeHints.Length} narrative hints");

            if (verboseLogging)
            {
                LogAllLoadedData();
            }
        }

        private void LogAllLoadedData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PROFILE MANAGER - VERBOSE DATA DUMP ===");
            sb.AppendLine();

            // Interests
            sb.AppendLine("--- INTERESTS ---");
            sb.AppendLine($"Total: {interests.Length}");
            foreach (var interest in interests)
            {
                if (interest != null)
                {
                    sb.AppendLine($"  - {interest.displayName} ({interest.category}) [Weight: {interest.matchWeight}]");
                    if (interest.relatedInterests != null && interest.relatedInterests.Length > 0)
                    {
                        var relatedNames = interest.relatedInterests
                            .Where(r => r != null)
                            .Select(r => r.displayName);
                        sb.AppendLine($"      Related: {string.Join(", ", relatedNames)}");
                    }
                }
            }
            sb.AppendLine();

            // Personality Traits
            sb.AppendLine("--- PERSONALITY TRAITS ---");
            sb.AppendLine($"Total: {personalityTraits.Length}");
            foreach (var trait in personalityTraits)
            {
                if (trait != null)
                {
                    string positiveFlag = trait.isPositiveTrait ? "+" : "-";
                    sb.AppendLine($"  [{positiveFlag}] {trait.displayName} [Weight: {trait.matchWeight}]");
                    if (trait.oppositeTraits != null && trait.oppositeTraits.Length > 0)
                    {
                        var oppositeNames = trait.oppositeTraits
                            .Where(o => o != null)
                            .Select(o => o.displayName);
                        sb.AppendLine($"      Opposites: {string.Join(", ", oppositeNames)}");
                    }
                    if (trait.complementaryTraits != null && trait.complementaryTraits.Length > 0)
                    {
                        var compNames = trait.complementaryTraits
                            .Where(c => c != null)
                            .Select(c => c.displayName);
                        sb.AppendLine($"      Complements: {string.Join(", ", compNames)}");
                    }
                }
            }
            sb.AppendLine();

            // Lifestyle Traits
            sb.AppendLine("--- LIFESTYLE TRAITS ---");
            sb.AppendLine($"Total: {lifestyleTraits.Length}");
            foreach (var trait in lifestyleTraits)
            {
                if (trait != null)
                {
                    sb.AppendLine($"  - {trait.displayName} ({trait.category}) [Weight: {trait.matchWeight}]");
                    if (trait.conflictingTraits != null && trait.conflictingTraits.Length > 0)
                    {
                        var conflictNames = trait.conflictingTraits
                            .Where(c => c != null)
                            .Select(c => c.displayName);
                        sb.AppendLine($"      Conflicts: {string.Join(", ", conflictNames)}");
                    }
                    if (trait.compatibleTraits != null && trait.compatibleTraits.Length > 0)
                    {
                        var compatNames = trait.compatibleTraits
                            .Where(c => c != null)
                            .Select(c => c.displayName);
                        sb.AppendLine($"      Compatible: {string.Join(", ", compatNames)}");
                    }
                }
            }
            sb.AppendLine();

            // Candidate Profiles
            sb.AppendLine("--- CANDIDATE PROFILES ---");
            sb.AppendLine($"Total: {candidateProfiles.Length}");
            foreach (var candidate in candidateProfiles)
            {
                if (candidate != null && candidate.profile != null)
                {
                    var p = candidate.profile;
                    sb.AppendLine();
                    sb.AppendLine($"  [{p.characterName}]");
                    sb.AppendLine($"    {p.gender}, Age {p.age}, {p.archetype}");
                    sb.AppendLine($"    Bio: {p.bio}");

                    if (p.personalityTraits != null && p.personalityTraits.Length > 0)
                    {
                        var traitNames = p.personalityTraits.Where(t => t != null).Select(t => t.displayName);
                        sb.AppendLine($"    Personality: {string.Join(", ", traitNames)}");
                    }
                    if (p.interests != null && p.interests.Length > 0)
                    {
                        var interestNames = p.interests.Where(i => i != null).Select(i => i.displayName);
                        sb.AppendLine($"    Interests: {string.Join(", ", interestNames)}");
                    }
                    if (p.lifestyleTraits != null && p.lifestyleTraits.Length > 0)
                    {
                        var lifestyleNames = p.lifestyleTraits.Where(l => l != null).Select(l => l.displayName);
                        sb.AppendLine($"    Lifestyle: {string.Join(", ", lifestyleNames)}");
                    }

                    sb.AppendLine($"    Posts: {candidate.guaranteedPosts.Count} guaranteed");
                    for (int i = 0; i < candidate.guaranteedPosts.Count; i++)
                    {
                        var post = candidate.guaranteedPosts[i];
                        string flags = "";
                        if (post.isGreenFlag) flags += " [GREEN]";
                        if (post.isRedFlag) flags += " [RED]";
                        sb.AppendLine($"      Post {i + 1}: {post.postType}{flags} - \"{TruncateString(post.content, 50)}\"");
                    }
                }
            }
            sb.AppendLine();

            // Narrative Hints
            sb.AppendLine("--- NARRATIVE HINTS ---");
            sb.AppendLine($"Total Collections: {narrativeHints.Length}");
            foreach (var hintCollection in narrativeHints)
            {
                if (hintCollection != null)
                {
                    sb.AppendLine($"  [{hintCollection.name}]");
                    if (hintCollection.hints != null && hintCollection.hints.Length > 0)
                    {
                        sb.AppendLine($"    Hints: {string.Join(" | ", hintCollection.hints)}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== END DATA DUMP ===");

            Debug.Log(sb.ToString());
        }

        private string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength) + "...";
        }

        private void BuildLookupDictionaries()
        {
            // Build candidate lookup by character name
            candidatesByName = new Dictionary<string, CandidateProfileSO>();
            foreach (var candidate in candidateProfiles)
            {
                if (candidate != null && candidate.profile != null)
                {
                    string key = candidate.profile.characterName;
                    if (!string.IsNullOrEmpty(key) && !candidatesByName.ContainsKey(key))
                    {
                        candidatesByName[key] = candidate;
                    }
                }
            }

            // Build interest lookup by display name
            interestsByName = new Dictionary<string, InterestSO>();
            foreach (var interest in interests)
            {
                if (interest != null && !string.IsNullOrEmpty(interest.displayName))
                {
                    if (!interestsByName.ContainsKey(interest.displayName))
                    {
                        interestsByName[interest.displayName] = interest;
                    }
                }
            }

            // Build personality trait lookup by display name
            personalityTraitsByName = new Dictionary<string, PersonalityTraitSO>();
            foreach (var trait in personalityTraits)
            {
                if (trait != null && !string.IsNullOrEmpty(trait.displayName))
                {
                    if (!personalityTraitsByName.ContainsKey(trait.displayName))
                    {
                        personalityTraitsByName[trait.displayName] = trait;
                    }
                }
            }

            // Build lifestyle trait lookup by display name
            lifestyleTraitsByName = new Dictionary<string, LifestyleTraitSO>();
            foreach (var trait in lifestyleTraits)
            {
                if (trait != null && !string.IsNullOrEmpty(trait.displayName))
                {
                    if (!lifestyleTraitsByName.ContainsKey(trait.displayName))
                    {
                        lifestyleTraitsByName[trait.displayName] = trait;
                    }
                }
            }
        }

        #region Candidate Profile Access

        /// <summary>
        /// Returns all loaded candidate profiles.
        /// </summary>
        public CandidateProfileSO[] GetAllCandidates()
        {
            return candidateProfiles;
        }

        /// <summary>
        /// Gets a candidate profile by character name.
        /// </summary>
        public CandidateProfileSO GetCandidateByName(string name)
        {
            if (candidatesByName.TryGetValue(name, out var candidate))
            {
                return candidate;
            }
            return null;
        }

        /// <summary>
        /// Returns a random candidate profile.
        /// </summary>
        public CandidateProfileSO GetRandomCandidate()
        {
            if (candidateProfiles == null || candidateProfiles.Length == 0)
            {
                return null;
            }
            return candidateProfiles[Random.Range(0, candidateProfiles.Length)];
        }

        #endregion

        #region Interest Access

        /// <summary>
        /// Returns all loaded interests.
        /// </summary>
        public InterestSO[] GetAllInterests()
        {
            return interests;
        }

        /// <summary>
        /// Gets an interest by display name.
        /// </summary>
        public InterestSO GetInterestByName(string name)
        {
            if (interestsByName.TryGetValue(name, out var interest))
            {
                return interest;
            }
            return null;
        }

        /// <summary>
        /// Returns all interests in a specific category.
        /// </summary>
        public InterestSO[] GetInterestsByCategory(InterestCategory category)
        {
            return interests.Where(i => i != null && i.category == category).ToArray();
        }

        #endregion

        #region Personality Trait Access

        /// <summary>
        /// Returns all loaded personality traits.
        /// </summary>
        public PersonalityTraitSO[] GetAllPersonalityTraits()
        {
            return personalityTraits;
        }

        /// <summary>
        /// Gets a personality trait by display name.
        /// </summary>
        public PersonalityTraitSO GetPersonalityTraitByName(string name)
        {
            if (personalityTraitsByName.TryGetValue(name, out var trait))
            {
                return trait;
            }
            return null;
        }

        /// <summary>
        /// Returns all positive personality traits.
        /// </summary>
        public PersonalityTraitSO[] GetPositivePersonalityTraits()
        {
            return personalityTraits.Where(t => t != null && t.isPositiveTrait).ToArray();
        }

        /// <summary>
        /// Returns all negative personality traits.
        /// </summary>
        public PersonalityTraitSO[] GetNegativePersonalityTraits()
        {
            return personalityTraits.Where(t => t != null && !t.isPositiveTrait).ToArray();
        }

        #endregion

        #region Lifestyle Trait Access

        /// <summary>
        /// Returns all loaded lifestyle traits.
        /// </summary>
        public LifestyleTraitSO[] GetAllLifestyleTraits()
        {
            return lifestyleTraits;
        }

        /// <summary>
        /// Gets a lifestyle trait by display name.
        /// </summary>
        public LifestyleTraitSO GetLifestyleTraitByName(string name)
        {
            if (lifestyleTraitsByName.TryGetValue(name, out var trait))
            {
                return trait;
            }
            return null;
        }

        /// <summary>
        /// Returns all lifestyle traits in a specific category.
        /// </summary>
        public LifestyleTraitSO[] GetLifestyleTraitsByCategory(LifestyleCategory category)
        {
            return lifestyleTraits.Where(t => t != null && t.category == category).ToArray();
        }

        #endregion

        #region Narrative Hints Access

        /// <summary>
        /// Returns all loaded narrative hint collections.
        /// </summary>
        public NarrativeHintCollectionSO[] GetAllNarrativeHints()
        {
            return narrativeHints;
        }

        /// <summary>
        /// Finds narrative hint collections that reference the given trait.
        /// Works with InterestSO, PersonalityTraitSO, or LifestyleTraitSO.
        /// </summary>
        public NarrativeHintCollectionSO[] GetHintsForTrait(ScriptableObject trait)
        {
            if (trait == null || narrativeHints == null)
            {
                return new NarrativeHintCollectionSO[0];
            }

            var results = new List<NarrativeHintCollectionSO>();

            foreach (var hintCollection in narrativeHints)
            {
                if (hintCollection == null) continue;

                bool found = false;

                // Check if trait is an Interest
                if (trait is InterestSO interest && hintCollection.relatedInterests != null)
                {
                    found = hintCollection.relatedInterests.Contains(interest);
                }
                // Check if trait is a PersonalityTrait
                else if (trait is PersonalityTraitSO personality && hintCollection.relatedPersonalityTraits != null)
                {
                    found = hintCollection.relatedPersonalityTraits.Contains(personality);
                }
                // Check if trait is a LifestyleTrait
                else if (trait is LifestyleTraitSO lifestyle && hintCollection.relatedLifestyleTraits != null)
                {
                    found = hintCollection.relatedLifestyleTraits.Contains(lifestyle);
                }

                if (found)
                {
                    results.Add(hintCollection);
                }
            }

            return results.ToArray();
        }

        #endregion
    }
}
