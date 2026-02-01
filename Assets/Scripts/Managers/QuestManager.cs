using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Maskhot.Data;

namespace Maskhot.Managers
{
    /// <summary>
    /// Singleton manager that handles quest lifecycle and client data.
    /// Loads clients from Resources, manages the current quest, and fires events for quest state changes.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // Loaded client data
        private ClientProfileSO[] clients;
        private Dictionary<string, ClientProfileSO> clientsByName;

        // Current quest state
        private Quest currentQuest;

        #region Events

        /// <summary>
        /// Fired when a new quest is started.
        /// </summary>
        public event Action<Quest> OnQuestStarted;

        /// <summary>
        /// Fired when the current quest is completed (player finished all decisions).
        /// </summary>
        public event Action<Quest> OnQuestCompleted;

        /// <summary>
        /// Fired when the current quest is cleared without completion.
        /// </summary>
        public event Action OnQuestCleared;

        #endregion

        #region Properties

        /// <summary>
        /// The currently active quest, or null if no quest is active.
        /// </summary>
        public Quest CurrentQuest => currentQuest;

        /// <summary>
        /// Returns true if there is an active quest.
        /// </summary>
        public bool HasActiveQuest => currentQuest != null;

        /// <summary>
        /// Returns the number of loaded clients.
        /// </summary>
        public int ClientCount => clients?.Length ?? 0;

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

            LoadClients();
        }

        #endregion

        #region Client Loading

        private void LoadClients()
        {
            clients = Resources.LoadAll<ClientProfileSO>("GameData/Clients");
            BuildClientLookup();

            Debug.Log($"QuestManager: Loaded {clients.Length} clients");

            if (verboseLogging)
            {
                LogAllClients();
            }
        }

        private void BuildClientLookup()
        {
            clientsByName = new Dictionary<string, ClientProfileSO>();

            foreach (var client in clients)
            {
                if (client != null && client.profile != null)
                {
                    string key = client.profile.clientName;
                    if (!string.IsNullOrEmpty(key) && !clientsByName.ContainsKey(key))
                    {
                        clientsByName[key] = client;
                    }
                }
            }
        }

        private void LogAllClients()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== QUEST MANAGER - LOADED CLIENTS ===");

            foreach (var client in clients)
            {
                if (client != null && client.profile != null)
                {
                    var p = client.profile;
                    string storyTag = client.isStoryClient ? "[STORY]" : "[SIDE]";
                    sb.AppendLine($"  {storyTag} {p.clientName} (Level {client.suggestedLevel})");
                }
            }

            sb.AppendLine("=== END CLIENT LIST ===");
            Debug.Log(sb.ToString());
        }

        #endregion

        #region Quest Management

        /// <summary>
        /// Starts a new quest from a ClientProfileSO.
        /// Uses the client's introduction and match criteria.
        /// </summary>
        public void StartQuest(ClientProfileSO client)
        {
            if (client == null)
            {
                Debug.LogWarning("QuestManager: Cannot start quest with null client");
                return;
            }

            // Clear existing quest without firing event (replacement, not abandonment)
            if (currentQuest != null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"QuestManager: Replacing previous quest");
                }
                currentQuest = null;
            }

            currentQuest = Quest.FromClientProfileSO(client, client.introduction, client.matchCriteria);

            if (verboseLogging)
            {
                Debug.Log($"QuestManager: Started quest for client '{client.profile.clientName}'");
            }

            OnQuestStarted?.Invoke(currentQuest);
        }

        /// <summary>
        /// Starts a new quest from a pre-built Quest object.
        /// Use this for procedurally generated quests.
        /// </summary>
        public void StartQuest(Quest quest)
        {
            if (quest == null)
            {
                Debug.LogWarning("QuestManager: Cannot start null quest");
                return;
            }

            // Clear existing quest without firing event (replacement, not abandonment)
            if (currentQuest != null)
            {
                if (verboseLogging)
                {
                    Debug.Log($"QuestManager: Replacing previous quest");
                }
                currentQuest = null;
            }

            currentQuest = quest;

            string clientName = quest.client?.clientName ?? "Unknown";
            if (verboseLogging)
            {
                Debug.Log($"QuestManager: Started procedural quest for '{clientName}'");
            }

            OnQuestStarted?.Invoke(currentQuest);
        }

        /// <summary>
        /// Marks the current quest as completed.
        /// Call this when the player has finished all decisions for this quest.
        /// </summary>
        public void CompleteQuest()
        {
            if (currentQuest == null)
            {
                Debug.LogWarning("QuestManager: No active quest to complete");
                return;
            }

            var completedQuest = currentQuest;
            currentQuest = null;

            string clientName = completedQuest.client?.clientName ?? "Unknown";
            if (verboseLogging)
            {
                Debug.Log($"QuestManager: Completed quest for '{clientName}'");
            }

            OnQuestCompleted?.Invoke(completedQuest);
        }

        /// <summary>
        /// Clears the current quest without marking it as completed.
        /// Use this when abandoning a quest or resetting state.
        /// </summary>
        public void ClearQuest()
        {
            if (currentQuest == null)
            {
                return;
            }

            string clientName = currentQuest.client?.clientName ?? "Unknown";
            currentQuest = null;

            if (verboseLogging)
            {
                Debug.Log($"QuestManager: Cleared quest for '{clientName}'");
            }

            OnQuestCleared?.Invoke();
        }

        #endregion

        #region Client Access

        /// <summary>
        /// Returns all loaded clients.
        /// </summary>
        public ClientProfileSO[] GetAllClients()
        {
            return clients;
        }

        /// <summary>
        /// Gets a client by name.
        /// </summary>
        public ClientProfileSO GetClientByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (clientsByName.TryGetValue(name, out var client))
            {
                return client;
            }

            return null;
        }

        /// <summary>
        /// Returns all story clients (main narrative characters).
        /// </summary>
        public ClientProfileSO[] GetStoryClients()
        {
            return clients.Where(c => c != null && c.isStoryClient).ToArray();
        }

        /// <summary>
        /// Returns all clients suggested for a specific level.
        /// Level 0 means "any level" and will be included in all queries.
        /// </summary>
        public ClientProfileSO[] GetClientsForLevel(int level)
        {
            return clients.Where(c => c != null && (c.suggestedLevel == level || c.suggestedLevel == 0)).ToArray();
        }

        /// <summary>
        /// Returns a random client.
        /// </summary>
        public ClientProfileSO GetRandomClient()
        {
            if (clients == null || clients.Length == 0)
            {
                return null;
            }

            return clients[UnityEngine.Random.Range(0, clients.Length)];
        }

        /// <summary>
        /// Returns a random story client.
        /// </summary>
        public ClientProfileSO GetRandomStoryClient()
        {
            var storyClients = GetStoryClients();
            if (storyClients == null || storyClients.Length == 0)
            {
                return null;
            }

            return storyClients[UnityEngine.Random.Range(0, storyClients.Length)];
        }

        #endregion
    }
}
