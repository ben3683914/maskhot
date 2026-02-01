using UnityEngine;
using System.Text;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify GameManager functionality.
    /// Add this to a GameObject along with GameManager.
    ///
    /// Testing Instructions:
    /// 1. Add GameManager, QuestManager, MatchQueueManager, DecisionController, MatchListController to scene
    /// 2. Add GameManagerTester to the GameManager GameObject
    /// 3. Enter Play Mode
    /// 4. Click test buttons in the Inspector
    /// 5. Run tests in order:
    ///    a. "Log Current State" - verify initial Idle state
    ///    b. "Test Start Session" - verify session starts and state changes
    ///    c. "Test Begin Quest" - verify quest starts and queue populates
    ///    d. "Test Full Session Flow" - verify complete flow with simulated decisions
    /// 6. Check console output for results - look for any warnings or errors
    /// </summary>
    public class GameManagerTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Number of quests to use for session tests")]
        public int testQuestCount = 2;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = true;

        // Event tracking
        private int stateChangedCount;
        private int sessionStartedCount;
        private int questWonCount;
        private int questLostCount;
        private int sessionEndedCount;
        private GameState lastState;

        /// <summary>
        /// Logs the current GameManager state
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER STATE ===");
            sb.AppendLine();

            var gm = GameManager.Instance;
            sb.AppendLine($"  CurrentState: {gm.CurrentState}");
            sb.AppendLine($"  CurrentQuestIndex: {gm.CurrentQuestIndex}");
            sb.AppendLine($"  TotalQuestsInSession: {gm.TotalQuestsInSession}");
            sb.AppendLine($"  HasMoreQuests: {gm.HasMoreQuests}");
            sb.AppendLine();

            sb.AppendLine($"--- SESSION STATS ---");
            sb.AppendLine($"  QuestsCompleted: {gm.QuestsCompleted}");
            sb.AppendLine($"  QuestsWon: {gm.QuestsWon}");
            sb.AppendLine($"  QuestsLost: {gm.QuestsLost}");
            sb.AppendLine($"  AverageAccuracy: {gm.AverageAccuracy:F1}%");
            sb.AppendLine();

            sb.AppendLine($"--- QUEST STATE ---");
            sb.AppendLine($"  HasActiveQuest: {gm.HasActiveQuest}");
            if (gm.CurrentQuest != null)
            {
                sb.AppendLine($"  CurrentQuest Client: {gm.CurrentQuest.client?.clientName ?? "null"}");
            }

            sb.AppendLine();
            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests starting a session
        /// </summary>
        [ContextMenu("Test Start Session")]
        public void TestStartSession()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER TESTER: Start Session ===");
            sb.AppendLine();

            // Subscribe to events
            ResetEventCounters();
            SubscribeToEvents();

            sb.AppendLine($"--- STARTING SESSION ---");
            sb.AppendLine($"  Requested quest count: {testQuestCount}");
            sb.AppendLine();

            // Start session
            GameManager.Instance.StartSession(testQuestCount);

            sb.AppendLine($"--- RESULT ---");
            sb.AppendLine($"  CurrentState: {GameManager.Instance.CurrentState}");
            sb.AppendLine($"  TotalQuestsInSession: {GameManager.Instance.TotalQuestsInSession}");
            sb.AppendLine($"  CurrentQuestIndex: {GameManager.Instance.CurrentQuestIndex}");
            sb.AppendLine();

            sb.AppendLine($"--- EVENTS FIRED ---");
            sb.AppendLine($"  OnSessionStarted: {sessionStartedCount}");
            sb.AppendLine($"  OnStateChanged: {stateChangedCount} (last: {lastState})");
            sb.AppendLine();

            // Unsubscribe
            UnsubscribeFromEvents();

            // Verify
            sb.AppendLine($"--- VERIFICATION ---");
            bool sessionOk = sessionStartedCount == 1;
            bool stateOk = GameManager.Instance.CurrentState == GameState.BetweenQuests ||
                           GameManager.Instance.CurrentState == GameState.InQuest;
            sb.AppendLine($"  Session started event: {(sessionOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  State is BetweenQuests/InQuest: {(stateOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("Note: Session will auto-advance to first quest after delay.");
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests beginning a quest manually
        /// </summary>
        [ContextMenu("Test Begin Quest")]
        public void TestBeginQuest()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER TESTER: Begin Quest ===");
            sb.AppendLine();

            // Make sure we have a session
            if (GameManager.Instance.TotalQuestsInSession == 0)
            {
                sb.AppendLine("No active session. Starting one...");
                GameManager.Instance.StartSession(testQuestCount);
                sb.AppendLine();
            }

            // Skip any pending auto-advance
            GameManager.Instance.SkipDelay();

            sb.AppendLine($"--- STATE BEFORE ---");
            sb.AppendLine($"  CurrentState: {GameManager.Instance.CurrentState}");
            sb.AppendLine($"  CurrentQuestIndex: {GameManager.Instance.CurrentQuestIndex}");
            sb.AppendLine($"  HasActiveQuest: {GameManager.Instance.HasActiveQuest}");
            sb.AppendLine();

            // Subscribe to state changes
            ResetEventCounters();
            SubscribeToEvents();

            // Begin quest (if not already in one from auto-advance)
            if (GameManager.Instance.CurrentState != GameState.InQuest)
            {
                GameManager.Instance.BeginNextQuest();
            }

            sb.AppendLine($"--- STATE AFTER ---");
            sb.AppendLine($"  CurrentState: {GameManager.Instance.CurrentState}");
            sb.AppendLine($"  HasActiveQuest: {GameManager.Instance.HasActiveQuest}");

            if (GameManager.Instance.CurrentQuest != null)
            {
                sb.AppendLine($"  Quest Client: {GameManager.Instance.CurrentQuest.client?.clientName}");
            }

            // Check queue population
            if (MatchQueueManager.Instance != null)
            {
                sb.AppendLine($"  Queue Count: {MatchQueueManager.Instance.Count}");
            }

            sb.AppendLine();

            // Unsubscribe
            UnsubscribeFromEvents();

            // Verify
            sb.AppendLine($"--- VERIFICATION ---");
            bool stateOk = GameManager.Instance.CurrentState == GameState.InQuest;
            bool questOk = GameManager.Instance.HasActiveQuest;
            bool queueOk = MatchQueueManager.Instance != null && MatchQueueManager.Instance.Count > 0;

            sb.AppendLine($"  State is InQuest: {(stateOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Has active quest: {(questOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Queue populated: {(queueOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests completing a quest by simulating all decisions
        /// </summary>
        [ContextMenu("Test Complete Quest (Simulate Decisions)")]
        public void TestCompleteQuest()
        {
            if (!ValidateManager()) return;

            if (DecisionController.Instance == null || MatchListController.Instance == null)
            {
                Debug.LogError("GameManagerTester: DecisionController or MatchListController not found!");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER TESTER: Complete Quest ===");
            sb.AppendLine();

            // Ensure we're in a quest
            if (GameManager.Instance.CurrentState != GameState.InQuest)
            {
                sb.AppendLine("Not in a quest. Starting session and quest...");
                GameManager.Instance.StartSession(testQuestCount);
                GameManager.Instance.SkipDelay();
                if (GameManager.Instance.CurrentState != GameState.InQuest)
                {
                    GameManager.Instance.BeginNextQuest();
                }
                sb.AppendLine();
            }

            if (MatchQueueManager.Instance == null || MatchQueueManager.Instance.Count == 0)
            {
                sb.AppendLine("ERROR: No candidates in queue");
                Debug.Log(sb.ToString());
                return;
            }

            // Subscribe to events
            ResetEventCounters();
            SubscribeToEvents();

            sb.AppendLine($"--- SIMULATING DECISIONS ---");
            sb.AppendLine($"  Candidates to decide: {MatchQueueManager.Instance.Count}");
            sb.AppendLine();

            // Make decisions on all candidates (accept all for simplicity)
            int decisionCount = 0;
            while (!MatchQueueManager.Instance.AllDecided)
            {
                // Select first pending
                MatchListController.Instance.SelectNextPending();
                var candidate = MatchListController.Instance.CurrentCandidate;

                if (candidate == null)
                {
                    sb.AppendLine("  WARNING: No candidate selected, breaking");
                    break;
                }

                // Accept (could also reject randomly for variety)
                DecisionController.Instance.AcceptCurrent();
                decisionCount++;

                if (verboseOutput)
                {
                    sb.AppendLine($"  Decided on: {candidate.profile.characterName}");
                }

                // Safety limit
                if (decisionCount > 50)
                {
                    sb.AppendLine("  WARNING: Too many decisions, breaking");
                    break;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"--- RESULT ---");
            sb.AppendLine($"  Decisions made: {decisionCount}");
            sb.AppendLine($"  CurrentState: {GameManager.Instance.CurrentState}");
            sb.AppendLine($"  QuestsWon: {GameManager.Instance.QuestsWon}");
            sb.AppendLine($"  QuestsLost: {GameManager.Instance.QuestsLost}");
            sb.AppendLine();

            sb.AppendLine($"--- EVENTS FIRED ---");
            sb.AppendLine($"  OnQuestWon: {questWonCount}");
            sb.AppendLine($"  OnQuestLost: {questLostCount}");
            sb.AppendLine($"  OnStateChanged: {stateChangedCount}");
            sb.AppendLine();

            // Unsubscribe
            UnsubscribeFromEvents();

            // Verify
            sb.AppendLine($"--- VERIFICATION ---");
            bool questResultOk = questWonCount + questLostCount == 1;
            bool stateOk = GameManager.Instance.CurrentState == GameState.BetweenQuests ||
                           GameManager.Instance.CurrentState == GameState.InQuest ||
                           GameManager.Instance.CurrentState == GameState.GameOver;

            sb.AppendLine($"  Quest result event fired: {(questResultOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  State transitioned: {(stateOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("Note: GameManager will auto-advance to next quest after delay.");
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests a full session from start to game over
        /// </summary>
        [ContextMenu("Test Full Session Flow")]
        public void TestFullSessionFlow()
        {
            if (!ValidateManager()) return;

            if (DecisionController.Instance == null || MatchListController.Instance == null)
            {
                Debug.LogError("GameManagerTester: DecisionController or MatchListController not found!");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER TESTER: Full Session Flow ===");
            sb.AppendLine();

            // Reset and start fresh
            GameManager.Instance.ResetSession();

            // Subscribe to events
            ResetEventCounters();
            SubscribeToEvents();

            // Start session
            sb.AppendLine($"--- STARTING SESSION ---");
            GameManager.Instance.StartSession(testQuestCount);
            sb.AppendLine($"  Quest count: {GameManager.Instance.TotalQuestsInSession}");
            sb.AppendLine();

            // Play through all quests
            int questNumber = 0;
            while (GameManager.Instance.CurrentState != GameState.GameOver && questNumber < 10)
            {
                questNumber++;

                // Skip auto-advance delay and start quest
                GameManager.Instance.SkipDelay();
                if (GameManager.Instance.CurrentState == GameState.BetweenQuests)
                {
                    GameManager.Instance.BeginNextQuest();
                }

                if (GameManager.Instance.CurrentState != GameState.InQuest)
                {
                    break;
                }

                sb.AppendLine($"--- QUEST {questNumber} ---");
                if (GameManager.Instance.CurrentQuest != null)
                {
                    sb.AppendLine($"  Client: {GameManager.Instance.CurrentQuest.client?.clientName}");
                }

                // Simulate decisions
                int decisions = 0;
                while (!MatchQueueManager.Instance.AllDecided && decisions < 50)
                {
                    MatchListController.Instance.SelectNextPending();
                    if (MatchListController.Instance.CurrentCandidate == null) break;

                    // Randomly accept or reject
                    if (Random.value > 0.5f)
                    {
                        DecisionController.Instance.AcceptCurrent();
                    }
                    else
                    {
                        DecisionController.Instance.RejectCurrent();
                    }
                    decisions++;
                }

                sb.AppendLine($"  Decisions: {decisions}");
                sb.AppendLine($"  Accuracy: {DecisionController.Instance.Accuracy:F1}%");
                sb.AppendLine();
            }

            // Wait a frame for events to process, then log final state
            sb.AppendLine($"--- FINAL RESULT ---");
            sb.AppendLine($"  CurrentState: {GameManager.Instance.CurrentState}");
            sb.AppendLine($"  QuestsCompleted: {GameManager.Instance.QuestsCompleted}");
            sb.AppendLine($"  QuestsWon: {GameManager.Instance.QuestsWon}");
            sb.AppendLine($"  QuestsLost: {GameManager.Instance.QuestsLost}");
            sb.AppendLine($"  AverageAccuracy: {GameManager.Instance.AverageAccuracy:F1}%");
            sb.AppendLine();

            sb.AppendLine($"--- EVENTS SUMMARY ---");
            sb.AppendLine($"  OnSessionStarted: {sessionStartedCount}");
            sb.AppendLine($"  OnQuestWon: {questWonCount}");
            sb.AppendLine($"  OnQuestLost: {questLostCount}");
            sb.AppendLine($"  OnSessionEnded: {sessionEndedCount}");
            sb.AppendLine($"  OnStateChanged: {stateChangedCount}");
            sb.AppendLine();

            // Unsubscribe
            UnsubscribeFromEvents();

            // Verify
            sb.AppendLine($"--- VERIFICATION ---");
            bool sessionStartOk = sessionStartedCount == 1;
            bool questsOk = questWonCount + questLostCount == testQuestCount;
            bool sessionEndOk = sessionEndedCount == 1 || GameManager.Instance.CurrentState == GameState.BetweenQuests;

            sb.AppendLine($"  Session started once: {(sessionStartOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  All quests had results: {(questsOk ? "OK" : "FAIL")} ({questWonCount + questLostCount}/{testQuestCount})");
            sb.AppendLine($"  Session ended: {(sessionEndOk ? "OK" : "WAITING (auto-advance pending)")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests state transitions
        /// </summary>
        [ContextMenu("Test State Transitions")]
        public void TestStateTransitions()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER TESTER: State Transitions ===");
            sb.AppendLine();

            // Track all state changes
            var stateHistory = new System.Collections.Generic.List<GameState>();
            void TrackState(GameState s) => stateHistory.Add(s);

            GameManager.Instance.OnStateChanged += TrackState;

            // Reset
            GameManager.Instance.ResetSession();
            sb.AppendLine($"After Reset: {GameManager.Instance.CurrentState}");

            // Start session
            GameManager.Instance.StartSession(1);
            sb.AppendLine($"After StartSession: {GameManager.Instance.CurrentState}");

            // Skip delay and begin quest
            GameManager.Instance.SkipDelay();
            if (GameManager.Instance.CurrentState != GameState.InQuest)
            {
                GameManager.Instance.BeginNextQuest();
            }
            sb.AppendLine($"After BeginNextQuest: {GameManager.Instance.CurrentState}");

            // Return to idle
            GameManager.Instance.ReturnToIdle();
            sb.AppendLine($"After ReturnToIdle: {GameManager.Instance.CurrentState}");

            GameManager.Instance.OnStateChanged -= TrackState;

            sb.AppendLine();
            sb.AppendLine($"--- STATE HISTORY ---");
            for (int i = 0; i < stateHistory.Count; i++)
            {
                sb.AppendLine($"  {i + 1}. {stateHistory[i]}");
            }

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests reset functionality
        /// </summary>
        [ContextMenu("Test Reset Session")]
        public void TestResetSession()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== GAME MANAGER TESTER: Reset Session ===");
            sb.AppendLine();

            // Start a session first
            sb.AppendLine($"--- SETUP ---");
            GameManager.Instance.StartSession(2);
            GameManager.Instance.SkipDelay();
            sb.AppendLine($"  Started session with 2 quests");
            sb.AppendLine($"  State: {GameManager.Instance.CurrentState}");
            sb.AppendLine();

            // Reset
            sb.AppendLine($"--- RESETTING ---");
            GameManager.Instance.ResetSession();

            sb.AppendLine($"--- AFTER RESET ---");
            sb.AppendLine($"  CurrentState: {GameManager.Instance.CurrentState}");
            sb.AppendLine($"  TotalQuestsInSession: {GameManager.Instance.TotalQuestsInSession}");
            sb.AppendLine($"  CurrentQuestIndex: {GameManager.Instance.CurrentQuestIndex}");
            sb.AppendLine($"  QuestsWon: {GameManager.Instance.QuestsWon}");
            sb.AppendLine($"  QuestsLost: {GameManager.Instance.QuestsLost}");
            sb.AppendLine($"  HasActiveQuest: {GameManager.Instance.HasActiveQuest}");
            sb.AppendLine();

            // Verify
            sb.AppendLine($"--- VERIFICATION ---");
            bool stateOk = GameManager.Instance.CurrentState == GameState.Idle;
            bool countOk = GameManager.Instance.TotalQuestsInSession == 0;
            bool indexOk = GameManager.Instance.CurrentQuestIndex == 0;
            bool statsOk = GameManager.Instance.QuestsWon == 0 && GameManager.Instance.QuestsLost == 0;

            sb.AppendLine($"  State is Idle: {(stateOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Quest count reset: {(countOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Index reset: {(indexOk ? "OK" : "FAIL")}");
            sb.AppendLine($"  Stats reset: {(statsOk ? "OK" : "FAIL")}");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        #region Event Handlers

        private void ResetEventCounters()
        {
            stateChangedCount = 0;
            sessionStartedCount = 0;
            questWonCount = 0;
            questLostCount = 0;
            sessionEndedCount = 0;
            lastState = GameState.Idle;
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                GameManager.Instance.OnSessionStarted += HandleSessionStarted;
                GameManager.Instance.OnQuestWon += HandleQuestWon;
                GameManager.Instance.OnQuestLost += HandleQuestLost;
                GameManager.Instance.OnSessionEnded += HandleSessionEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnSessionStarted -= HandleSessionStarted;
                GameManager.Instance.OnQuestWon -= HandleQuestWon;
                GameManager.Instance.OnQuestLost -= HandleQuestLost;
                GameManager.Instance.OnSessionEnded -= HandleSessionEnded;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            stateChangedCount++;
            lastState = state;
        }

        private void HandleSessionStarted()
        {
            sessionStartedCount++;
        }

        private void HandleQuestWon(int index, float accuracy)
        {
            questWonCount++;
            if (verboseOutput)
            {
                Debug.Log($"GameManagerTester: Quest {index + 1} WON with {accuracy:F1}% accuracy");
            }
        }

        private void HandleQuestLost(int index, float accuracy)
        {
            questLostCount++;
            if (verboseOutput)
            {
                Debug.Log($"GameManagerTester: Quest {index + 1} LOST with {accuracy:F1}% accuracy");
            }
        }

        private void HandleSessionEnded(SessionResult result)
        {
            sessionEndedCount++;
            if (verboseOutput)
            {
                Debug.Log($"GameManagerTester: Session ended - Won: {result.QuestsWon}, Lost: {result.QuestsLost}, Avg: {result.OverallAccuracy:F1}%");
            }
        }

        #endregion

        private bool ValidateManager()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManagerTester: GameManager not found! Add it to the scene.");
                return false;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogError("GameManagerTester: QuestManager not found! Add it to the scene.");
                return false;
            }

            if (MatchQueueManager.Instance == null)
            {
                Debug.LogError("GameManagerTester: MatchQueueManager not found! Add it to the scene.");
                return false;
            }

            return true;
        }
    }
}
