using UnityEngine;
using System;
using System.Collections.Generic;
using Maskhot.Data;
using Maskhot.Managers;

namespace Maskhot.Controllers
{
    /// <summary>
    /// A display-ready requirement with a cached narrative hint.
    /// </summary>
    [Serializable]
    public struct DisplayRequirement
    {
        public string hint;
        public RequirementLevel level;

        public DisplayRequirement(string hint, RequirementLevel level)
        {
            this.hint = hint;
            this.level = level;
        }
    }

    /// <summary>
    /// Singleton controller that provides a UI-facing interface for quest state and criteria.
    /// Subscribes to QuestManager events and caches display-ready data.
    /// UI should use this instead of accessing QuestManager directly.
    /// </summary>
    public class QuestController : MonoBehaviour
    {
        public static QuestController Instance { get; private set; }

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // Cached display data (set when quest starts)
        private DisplayRequirement[] cachedRequirements;
        private string[] cachedDealbreakers;

        #region Events

        /// <summary>
        /// Fired when the quest state changes (started, completed, or cleared).
        /// UI should re-read properties after this event.
        /// </summary>
        public event Action OnQuestChanged;

        #endregion

        #region Properties

        /// <summary>
        /// The currently active quest, or null if no quest is active.
        /// </summary>
        public Quest CurrentQuest => QuestManager.Instance?.CurrentQuest;

        /// <summary>
        /// Returns true if there is an active quest.
        /// </summary>
        public bool HasActiveQuest => QuestManager.Instance?.HasActiveQuest ?? false;

        /// <summary>
        /// The name of the current client, or empty if no quest.
        /// </summary>
        public string ClientName
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.client?.clientName ?? string.Empty;
            }
        }

        /// <summary>
        /// The introduction text from the current client, or empty if no quest.
        /// </summary>
        public string ClientIntroduction
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.introductionText ?? string.Empty;
            }
        }

        /// <summary>
        /// Display-ready requirements with cached narrative hints.
        /// Hints are randomly selected once when quest starts and stay consistent.
        /// Returns empty array if no quest.
        /// </summary>
        public DisplayRequirement[] Requirements => cachedRequirements ?? Array.Empty<DisplayRequirement>();

        /// <summary>
        /// Display names of dealbreaker traits.
        /// Returns empty array if no quest or no dealbreakers.
        /// </summary>
        public string[] Dealbreakers => cachedDealbreakers ?? Array.Empty<string>();

        /// <summary>
        /// Minimum acceptable age for the current quest.
        /// Returns 0 if no quest.
        /// </summary>
        public int MinAge
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.matchCriteria?.minAge ?? 0;
            }
        }

        /// <summary>
        /// Maximum acceptable age for the current quest.
        /// Returns 0 if no quest.
        /// </summary>
        public int MaxAge
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.matchCriteria?.maxAge ?? 0;
            }
        }

        /// <summary>
        /// Acceptable genders for the current quest.
        /// Returns empty array if no quest.
        /// </summary>
        public Gender[] AcceptableGenders
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.matchCriteria?.acceptableGenders ?? Array.Empty<Gender>();
            }
        }

        /// <summary>
        /// Maximum red flags allowed for the current quest.
        /// Returns 0 if no quest.
        /// </summary>
        public int MaxRedFlags
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.matchCriteria?.maxRedFlags ?? 0;
            }
        }

        /// <summary>
        /// Minimum green flags required for the current quest.
        /// Returns 0 if no quest.
        /// </summary>
        public int MinGreenFlags
        {
            get
            {
                var quest = CurrentQuest;
                return quest?.matchCriteria?.minGreenFlags ?? 0;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (verboseLogging)
            {
                Debug.Log("QuestController: Initialized");
            }
        }

        private void OnEnable()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
                QuestManager.Instance.OnQuestCleared += HandleQuestCleared;
            }
        }

        private void OnDisable()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
                QuestManager.Instance.OnQuestCleared -= HandleQuestCleared;
            }
        }

        private void Start()
        {
            // Re-subscribe in Start in case QuestManager wasn't ready in OnEnable
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
                QuestManager.Instance.OnQuestCleared -= HandleQuestCleared;

                QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
                QuestManager.Instance.OnQuestCleared += HandleQuestCleared;
            }

            // If quest already active (e.g., controller added after quest started), cache data
            if (HasActiveQuest)
            {
                CacheQuestData(CurrentQuest);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleQuestStarted(Quest quest)
        {
            CacheQuestData(quest);

            if (verboseLogging)
            {
                Debug.Log($"QuestController: Quest started for '{ClientName}'");
                Debug.Log($"  Requirements: {cachedRequirements?.Length ?? 0}");
                Debug.Log($"  Dealbreakers: {cachedDealbreakers?.Length ?? 0}");
            }

            OnQuestChanged?.Invoke();
        }

        private void HandleQuestCompleted(Quest quest)
        {
            ClearCachedData();

            if (verboseLogging)
            {
                Debug.Log($"QuestController: Quest completed");
            }

            OnQuestChanged?.Invoke();
        }

        private void HandleQuestCleared()
        {
            ClearCachedData();

            if (verboseLogging)
            {
                Debug.Log($"QuestController: Quest cleared");
            }

            OnQuestChanged?.Invoke();
        }

        #endregion

        #region Cache Management

        private void CacheQuestData(Quest quest)
        {
            if (quest == null || quest.matchCriteria == null)
            {
                ClearCachedData();
                return;
            }

            CacheRequirements(quest.matchCriteria);
            CacheDealbreakers(quest.matchCriteria);
        }

        private void CacheRequirements(MatchCriteria criteria)
        {
            if (criteria.traitRequirements == null || criteria.traitRequirements.Length == 0)
            {
                cachedRequirements = Array.Empty<DisplayRequirement>();
                return;
            }

            var requirements = new List<DisplayRequirement>();

            foreach (var req in criteria.traitRequirements)
            {
                string hint;

                if (req.narrativeHints != null && req.narrativeHints.hints != null && req.narrativeHints.hints.Length > 0)
                {
                    // Select a random hint and cache it
                    hint = req.narrativeHints.GetRandomHint();
                }
                else
                {
                    // Fallback: try to generate a hint from trait names
                    hint = GenerateFallbackHint(req);
                }

                requirements.Add(new DisplayRequirement(hint, req.level));
            }

            cachedRequirements = requirements.ToArray();
        }

        private string GenerateFallbackHint(TraitRequirement req)
        {
            // Try to get a display name from any acceptable trait
            if (req.acceptableInterests != null && req.acceptableInterests.Length > 0 && req.acceptableInterests[0] != null)
            {
                return $"interested in {req.acceptableInterests[0].displayName}";
            }

            if (req.acceptablePersonalityTraits != null && req.acceptablePersonalityTraits.Length > 0 && req.acceptablePersonalityTraits[0] != null)
            {
                return $"someone who is {req.acceptablePersonalityTraits[0].displayName}";
            }

            if (req.acceptableLifestyleTraits != null && req.acceptableLifestyleTraits.Length > 0 && req.acceptableLifestyleTraits[0] != null)
            {
                return $"has {req.acceptableLifestyleTraits[0].displayName} lifestyle";
            }

            return "(unknown requirement)";
        }

        private void CacheDealbreakers(MatchCriteria criteria)
        {
            var dealbreakers = new List<string>();

            if (criteria.dealbreakerPersonalityTraits != null)
            {
                foreach (var trait in criteria.dealbreakerPersonalityTraits)
                {
                    if (trait != null)
                    {
                        dealbreakers.Add(trait.displayName);
                    }
                }
            }

            if (criteria.dealbreakerInterests != null)
            {
                foreach (var interest in criteria.dealbreakerInterests)
                {
                    if (interest != null)
                    {
                        dealbreakers.Add(interest.displayName);
                    }
                }
            }

            if (criteria.dealbreakerLifestyleTraits != null)
            {
                foreach (var trait in criteria.dealbreakerLifestyleTraits)
                {
                    if (trait != null)
                    {
                        dealbreakers.Add(trait.displayName);
                    }
                }
            }

            cachedDealbreakers = dealbreakers.ToArray();
        }

        private void ClearCachedData()
        {
            cachedRequirements = null;
            cachedDealbreakers = null;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Forces a refresh of cached data from the current quest.
        /// Normally not needed, but can be used if quest data changes mid-session.
        /// </summary>
        public void RefreshCache()
        {
            if (HasActiveQuest)
            {
                CacheQuestData(CurrentQuest);

                if (verboseLogging)
                {
                    Debug.Log("QuestController: Cache refreshed");
                }
            }
            else
            {
                ClearCachedData();
            }
        }

        #endregion
    }
}
