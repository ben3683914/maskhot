using UnityEngine;
using System;
using System.Collections;
using Maskhot.Data;
using Maskhot.Controllers;

namespace Maskhot.Managers
{
    /// <summary>
    /// Game state enum for tracking session progression.
    /// </summary>
    public enum GameState
    {
        /// <summary>No session active (main menu).</summary>
        Idle,
        /// <summary>Player is actively reviewing candidates.</summary>
        InQuest,
        /// <summary>Quest completed, transitioning to next.</summary>
        BetweenQuests,
        /// <summary>Session ended (all quests complete).</summary>
        GameOver
    }

    /// <summary>
    /// Result data for a completed session.
    /// </summary>
    public struct SessionResult
    {
        public int TotalQuests;
        public int QuestsWon;
        public int QuestsLost;
        public float OverallAccuracy;
    }

    /// <summary>
    /// Singleton manager that orchestrates game sessions.
    /// Coordinates quest progression, tracks session state, and manages game flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Session Settings")]
        [Tooltip("Number of candidates per quest (0 = use MatchQueueManager default)")]
        public int candidatesPerQuest = 0;

        [Tooltip("Delay in seconds before auto-advancing to next quest")]
        public float questTransitionDelay = 1f;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        // Session state
        private GameState currentState = GameState.Idle;
        private ClientProfileSO[] sessionClients;
        private int currentQuestIndex = 0;
        private int questsWon = 0;
        private int questsLost = 0;
        private float totalAccuracy = 0f;

        // Coroutine tracking
        private Coroutine transitionCoroutine;

        #region Events

        /// <summary>
        /// Fired when game state changes.
        /// </summary>
        public event Action<GameState> OnStateChanged;

        /// <summary>
        /// Fired when a new game session starts.
        /// </summary>
        public event Action OnSessionStarted;

        /// <summary>
        /// Fired when a quest in the session is won (good accuracy).
        /// Parameters: (questIndex, accuracy)
        /// </summary>
        public event Action<int, float> OnQuestWon;

        /// <summary>
        /// Fired when a quest in the session is lost (poor accuracy).
        /// Parameters: (questIndex, accuracy)
        /// </summary>
        public event Action<int, float> OnQuestLost;

        /// <summary>
        /// Fired when the entire session ends.
        /// </summary>
        public event Action<SessionResult> OnSessionEnded;

        #endregion

        #region Properties

        /// <summary>
        /// Current game state.
        /// </summary>
        public GameState CurrentState => currentState;

        /// <summary>
        /// Current quest index (0-based).
        /// </summary>
        public int CurrentQuestIndex => currentQuestIndex;

        /// <summary>
        /// Total number of quests in the session.
        /// </summary>
        public int TotalQuestsInSession => sessionClients?.Length ?? 0;

        /// <summary>
        /// Number of quests completed (won + lost).
        /// </summary>
        public int QuestsCompleted => questsWon + questsLost;

        /// <summary>
        /// Number of quests won.
        /// </summary>
        public int QuestsWon => questsWon;

        /// <summary>
        /// Number of quests lost.
        /// </summary>
        public int QuestsLost => questsLost;

        /// <summary>
        /// True if there are more quests to play.
        /// </summary>
        public bool HasMoreQuests => currentQuestIndex < TotalQuestsInSession;

        /// <summary>
        /// Current quest (from QuestManager).
        /// </summary>
        public Quest CurrentQuest => QuestManager.Instance?.CurrentQuest;

        /// <summary>
        /// True if there is an active quest.
        /// </summary>
        public bool HasActiveQuest => QuestManager.Instance?.HasActiveQuest ?? false;

        /// <summary>
        /// Average accuracy across completed quests.
        /// </summary>
        public float AverageAccuracy
        {
            get
            {
                if (QuestsCompleted == 0) return 0f;
                return totalAccuracy / QuestsCompleted;
            }
        }

        #endregion

        #region Unity Lifecycle

        [Header("Startup")]
        [Tooltip("If true, automatically starts a session on Awake. Disable for menu-driven start.")]
        public bool autoStartSession = false;

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
                Debug.Log("GameManager: Initialized");
            }

            if (autoStartSession)
            {
                StartSession();
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            // Re-subscribe in Start in case controllers weren't ready in OnEnable
            UnsubscribeFromEvents();
            SubscribeToEvents();

            if (verboseLogging)
            {
                Debug.Log($"GameManager.Start: DecisionController.Instance = {(DecisionController.Instance != null ? "OK" : "NULL")}");
                Debug.Log($"GameManager.Start: QuestManager.Instance = {(QuestManager.Instance != null ? "OK" : "NULL")}");
            }
        }

        /// <summary>
        /// Called when a new session starts to ensure event subscriptions are current.
        /// Call this after all managers are initialized.
        /// </summary>
        public void EnsureEventSubscriptions()
        {
            UnsubscribeFromEvents();
            SubscribeToEvents();

            if (verboseLogging)
            {
                Debug.Log("GameManager: Event subscriptions refreshed");
            }
        }

        private void SubscribeToEvents()
        {
            if (DecisionController.Instance != null)
            {
                DecisionController.Instance.OnAllDecisionsComplete += HandleAllDecisionsComplete;
                if (verboseLogging)
                {
                    Debug.Log("GameManager: Subscribed to DecisionController.OnAllDecisionsComplete");
                }
            }
            else if (verboseLogging)
            {
                Debug.LogWarning("GameManager: DecisionController.Instance is null, cannot subscribe to OnAllDecisionsComplete");
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (DecisionController.Instance != null)
            {
                DecisionController.Instance.OnAllDecisionsComplete -= HandleAllDecisionsComplete;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            }
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Starts a new session with all available clients.
        /// </summary>
        public void StartSession()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("GameManager: QuestManager not available");
                return;
            }

            var clients = QuestManager.Instance.GetAllClients();
            StartSessionInternal(clients);
        }

        /// <summary>
        /// Starts a new session with specific clients.
        /// </summary>
        public void StartSession(ClientProfileSO[] clients)
        {
            if (clients == null || clients.Length == 0)
            {
                Debug.LogWarning("GameManager: Cannot start session with empty client list");
                return;
            }

            StartSessionInternal(clients);
        }

        /// <summary>
        /// Starts a new session with a specified number of random clients.
        /// </summary>
        public void StartSession(int questCount)
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("GameManager: QuestManager not available");
                return;
            }

            var allClients = QuestManager.Instance.GetAllClients();
            if (allClients == null || allClients.Length == 0)
            {
                Debug.LogWarning("GameManager: No clients available");
                return;
            }

            // Take up to questCount clients (or all if fewer available)
            int count = Mathf.Min(questCount, allClients.Length);
            var selectedClients = new ClientProfileSO[count];

            // Shuffle and select
            var shuffled = new System.Collections.Generic.List<ClientProfileSO>(allClients);
            for (int i = 0; i < count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, shuffled.Count);
                selectedClients[i] = shuffled[randomIndex];
                shuffled.RemoveAt(randomIndex);
            }

            StartSessionInternal(selectedClients);
        }

        private void StartSessionInternal(ClientProfileSO[] clients)
        {
            // Cancel any pending transition
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            // Ensure event subscriptions are current (controllers should be ready by now)
            EnsureEventSubscriptions();

            // Reset session state
            sessionClients = clients;
            currentQuestIndex = 0;
            questsWon = 0;
            questsLost = 0;
            totalAccuracy = 0f;

            if (verboseLogging)
            {
                Debug.Log($"GameManager: Starting session with {clients.Length} quests");
                foreach (var client in clients)
                {
                    Debug.Log($"  - {client.profile.clientName}");
                }
            }

            SetState(GameState.BetweenQuests);
            OnSessionStarted?.Invoke();

            // Auto-start first quest after delay
            transitionCoroutine = StartCoroutine(TransitionToNextQuest());
        }

        /// <summary>
        /// Returns to idle state (e.g., back to menu).
        /// </summary>
        public void ReturnToIdle()
        {
            // Cancel any pending transition
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            // Clear current quest if any
            if (QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest)
            {
                QuestManager.Instance.ClearQuest();
            }

            SetState(GameState.Idle);

            if (verboseLogging)
            {
                Debug.Log("GameManager: Returned to Idle");
            }
        }

        /// <summary>
        /// Fully resets the session state.
        /// </summary>
        public void ResetSession()
        {
            // Cancel any pending transition
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            // Clear current quest
            if (QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest)
            {
                QuestManager.Instance.ClearQuest();
            }

            // Reset all state
            sessionClients = null;
            currentQuestIndex = 0;
            questsWon = 0;
            questsLost = 0;
            totalAccuracy = 0f;

            SetState(GameState.Idle);

            if (verboseLogging)
            {
                Debug.Log("GameManager: Session reset");
            }
        }

        #endregion

        #region Quest Progression

        /// <summary>
        /// Begins the next quest in the session.
        /// Called automatically after transition delay, or manually if needed.
        /// </summary>
        public void BeginNextQuest()
        {
            if (currentState != GameState.BetweenQuests && currentState != GameState.Idle)
            {
                if (verboseLogging)
                {
                    Debug.Log($"GameManager: Cannot begin quest in state {currentState}");
                }
                return;
            }

            if (!HasMoreQuests)
            {
                if (verboseLogging)
                {
                    Debug.Log("GameManager: No more quests available");
                }
                EndSession();
                return;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("GameManager: QuestManager not available");
                return;
            }

            // Get next client
            var client = sessionClients[currentQuestIndex];

            if (verboseLogging)
            {
                Debug.Log($"GameManager: Beginning quest {currentQuestIndex + 1}/{TotalQuestsInSession} for '{client.profile.clientName}'");
            }

            // Start the quest
            QuestManager.Instance.StartQuest(client);

            // Populate the queue
            if (MatchQueueManager.Instance != null)
            {
                var quest = QuestManager.Instance.CurrentQuest;
                if (candidatesPerQuest > 0)
                {
                    MatchQueueManager.Instance.PopulateForQuest(quest, candidatesPerQuest);
                }
                else
                {
                    MatchQueueManager.Instance.PopulateForQuest(quest);
                }
            }

            SetState(GameState.InQuest);
        }

        /// <summary>
        /// Skips the transition delay and immediately starts the next quest.
        /// </summary>
        public void SkipDelay()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            if (currentState == GameState.BetweenQuests)
            {
                if (HasMoreQuests)
                {
                    BeginNextQuest();
                }
                else
                {
                    EndSession();
                }
            }
        }

        private IEnumerator TransitionToNextQuest()
        {
            yield return new WaitForSeconds(questTransitionDelay);

            transitionCoroutine = null;

            if (verboseLogging)
            {
                Debug.Log($"GameManager.TransitionToNextQuest: HasMoreQuests = {HasMoreQuests}, currentQuestIndex = {currentQuestIndex}, TotalQuestsInSession = {TotalQuestsInSession}");
            }

            if (HasMoreQuests)
            {
                BeginNextQuest();
            }
            else
            {
                EndSession();
            }
        }

        #endregion

        #region Event Handlers

        private void HandleQuestStarted(Quest quest)
        {
            // Sync state when any quest starts (even if not started through GameManager)
            if (currentState != GameState.InQuest)
            {
                SetState(GameState.InQuest);

                if (verboseLogging)
                {
                    string clientName = quest?.client?.clientName ?? "Unknown";
                    Debug.Log($"GameManager: Synced to InQuest state for '{clientName}'");
                }
            }
        }

        private void HandleAllDecisionsComplete()
        {
            if (verboseLogging)
            {
                Debug.Log($"GameManager.HandleAllDecisionsComplete: currentState = {currentState}");
            }

            if (currentState != GameState.InQuest)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning($"GameManager: HandleAllDecisionsComplete ignored - not in InQuest state (current: {currentState})");
                }
                return;
            }

            // Determine win/loss based on whether a correct match was found
            // Win = found a correct match (TruePositive)
            // Loss = exhausted all candidates without finding a correct match
            bool foundCorrectMatch = false;
            float accuracy = 0f;

            if (DecisionController.Instance != null)
            {
                foundCorrectMatch = DecisionController.Instance.CorrectAccepts > 0;
                accuracy = DecisionController.Instance.Accuracy;
            }

            if (foundCorrectMatch)
            {
                questsWon++;
                if (verboseLogging)
                {
                    Debug.Log($"GameManager: Quest WON - found correct match! (accuracy: {accuracy:F1}%)");
                }
                OnQuestWon?.Invoke(currentQuestIndex, accuracy);
            }
            else
            {
                questsLost++;
                if (verboseLogging)
                {
                    Debug.Log($"GameManager: Quest LOST - no correct match found (accuracy: {accuracy:F1}%)");
                }
                OnQuestLost?.Invoke(currentQuestIndex, accuracy);
            }

            // Track for average
            totalAccuracy += accuracy;

            // Complete the quest in QuestManager
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CompleteQuest();
            }

            // Move to next quest
            currentQuestIndex++;

            if (verboseLogging)
            {
                Debug.Log($"GameManager: Quest {currentQuestIndex}/{TotalQuestsInSession} complete. HasMoreQuests: {HasMoreQuests}");
            }

            // Transition
            SetState(GameState.BetweenQuests);

            // Start auto-advance coroutine
            if (verboseLogging)
            {
                Debug.Log($"GameManager: Starting transition coroutine (delay: {questTransitionDelay}s)");
            }
            transitionCoroutine = StartCoroutine(TransitionToNextQuest());
        }

        #endregion

        #region Private Helpers

        private void SetState(GameState newState)
        {
            if (currentState == newState) return;

            var previousState = currentState;
            currentState = newState;

            if (verboseLogging)
            {
                Debug.Log($"GameManager: State changed from {previousState} to {newState}");
            }

            OnStateChanged?.Invoke(newState);
        }

        private void EndSession()
        {
            var result = new SessionResult
            {
                TotalQuests = TotalQuestsInSession,
                QuestsWon = questsWon,
                QuestsLost = questsLost,
                OverallAccuracy = AverageAccuracy
            };

            if (verboseLogging)
            {
                Debug.Log($"GameManager: Session ended - Won: {result.QuestsWon}, Lost: {result.QuestsLost}, Avg Accuracy: {result.OverallAccuracy:F1}%");
            }

            SetState(GameState.GameOver);
            OnSessionEnded?.Invoke(result);
        }

        #endregion
    }
}
