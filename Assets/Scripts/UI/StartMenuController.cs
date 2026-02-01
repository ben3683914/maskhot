using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.UI
{
    /// <summary>
    /// Controls the Start Menu UI.
    /// Handles Start Game, Credits, and Quit button interactions.
    /// Uses additive scene loading - keeps StartMenu visible until game is fully ready.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StartMenuController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Name of the game scene to load")]
        public string gameSceneName = "SampleScene";

        [Tooltip("Name of this scene (for unloading)")]
        public string startMenuSceneName = "StartMenu";

        [Tooltip("Minimum time to show loading screen (seconds)")]
        public float minimumLoadingTime = 1.5f;

        private UIDocument uiDocument;
        private VisualElement root;

        // Main Menu Elements
        private Button startButton;
        private Button creditsButton;
        private Button quitButton;
        private VisualElement creditsOverlay;
        private Button creditsBackButton;

        // Loading Elements
        private VisualElement loadingOverlay;
        private VisualElement progressBarFill;
        private Label loadingStatus;

        private bool isLoading = false;
        private bool queuePopulated = false;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;

            // Query main menu buttons
            startButton = root.Q<Button>("start-button");
            creditsButton = root.Q<Button>("credits-button");
            quitButton = root.Q<Button>("quit-button");

            // Query credits panel elements
            creditsOverlay = root.Q<VisualElement>("credits-overlay");
            creditsBackButton = root.Q<Button>("credits-back-button");

            // Query loading elements
            loadingOverlay = root.Q<VisualElement>("loading-overlay");
            progressBarFill = root.Q<VisualElement>("progress-bar-fill");
            loadingStatus = root.Q<Label>("loading-status");

            // Register button callbacks
            if (startButton != null)
            {
                startButton.clicked += OnStartClicked;
            }

            if (creditsButton != null)
            {
                creditsButton.clicked += OnCreditsClicked;
            }

            if (quitButton != null)
            {
                quitButton.clicked += OnQuitClicked;
            }

            if (creditsBackButton != null)
            {
                creditsBackButton.clicked += OnCreditsBackClicked;
            }

            // Ensure overlays start hidden
            HideCredits();
            HideLoading();
        }

        private void OnDisable()
        {
            // Unregister button callbacks
            if (startButton != null)
            {
                startButton.clicked -= OnStartClicked;
            }

            if (creditsButton != null)
            {
                creditsButton.clicked -= OnCreditsClicked;
            }

            if (quitButton != null)
            {
                quitButton.clicked -= OnQuitClicked;
            }

            if (creditsBackButton != null)
            {
                creditsBackButton.clicked -= OnCreditsBackClicked;
            }

            // Unsubscribe from controllers if we were loading
            UnsubscribeFromControllers();
        }

        private void UnsubscribeFromControllers()
        {
            if (MatchListController.Instance != null)
            {
                MatchListController.Instance.OnQueueUpdated -= HandleQueueUpdated;
            }
        }

        private void OnStartClicked()
        {
            if (isLoading) return;

            Debug.Log($"StartMenuController: Starting additive load of '{gameSceneName}'");
            StartCoroutine(LoadGameSceneAdditive());
        }

        private void OnCreditsClicked()
        {
            if (isLoading) return;

            Debug.Log("StartMenuController: Showing credits");
            ShowCredits();
        }

        private void OnQuitClicked()
        {
            if (isLoading) return;

            Debug.Log("StartMenuController: Quitting application");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnCreditsBackClicked()
        {
            Debug.Log("StartMenuController: Hiding credits");
            HideCredits();
        }

        private IEnumerator LoadGameSceneAdditive()
        {
            isLoading = true;
            queuePopulated = false;
            ShowLoading();
            SetProgress(0f, "Preparing...");

            float loadStartTime = Time.time;

            // Phase 1: Load the scene additively
            SetProgress(0.1f, "Loading scene...");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
            {
                // Progress from 0.1 to 0.4 during scene load
                float progress = 0.1f + (asyncLoad.progress / 0.9f) * 0.3f;
                SetProgress(progress, "Loading scene...");
                yield return null;
            }

            Debug.Log("StartMenuController: Scene loaded, waiting for managers...");
            SetProgress(0.4f, "Initializing managers...");

            // Phase 2: Wait for GameManager to be available
            float timeout = 10f;
            float elapsed = 0f;

            while (GameManager.Instance == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("StartMenuController: GameManager not found!");
                yield break;
            }

            // Phase 3: Wait for MatchListController to be available
            SetProgress(0.5f, "Waiting for controllers...");
            elapsed = 0f;

            while (MatchListController.Instance == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (MatchListController.Instance == null)
            {
                Debug.LogError("StartMenuController: MatchListController not found!");
                yield break;
            }

            // Subscribe to queue updates - this fires when candidates are actually populated
            MatchListController.Instance.OnQueueUpdated += HandleQueueUpdated;

            // Phase 4: Start the game session
            SetProgress(0.6f, "Starting session...");
            Debug.Log("StartMenuController: Starting game session...");
            GameManager.Instance.StartSession();

            // Phase 5: Wait for queue to be populated (OnQueueUpdated fires)
            SetProgress(0.7f, "Populating candidates...");
            elapsed = 0f;

            while (!queuePopulated && elapsed < timeout)
            {
                // Animate progress from 0.7 to 0.9 while waiting
                float waitProgress = 0.7f + (elapsed / timeout) * 0.2f;
                SetProgress(Mathf.Min(waitProgress, 0.9f), "Populating candidates...");

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Unsubscribe
            MatchListController.Instance.OnQueueUpdated -= HandleQueueUpdated;

            if (!queuePopulated)
            {
                Debug.LogWarning("StartMenuController: Timed out waiting for queue, proceeding anyway");
            }
            else
            {
                Debug.Log("StartMenuController: Queue populated, candidates ready!");
            }

            // Ensure minimum loading time has passed
            float totalElapsed = Time.time - loadStartTime;
            if (totalElapsed < minimumLoadingTime)
            {
                SetProgress(0.95f, "Almost ready...");
                yield return new WaitForSeconds(minimumLoadingTime - totalElapsed);
            }

            SetProgress(1f, "Starting game...");
            yield return new WaitForSeconds(0.3f);

            // Phase 6: Unload the StartMenu scene
            Debug.Log("StartMenuController: Unloading StartMenu");

            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(startMenuSceneName);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
            }

            Debug.Log("StartMenuController: StartMenu unloaded, game is now active");
        }

        private void HandleQueueUpdated()
        {
            Debug.Log($"StartMenuController: Queue updated! Count: {MatchListController.Instance.Count}");

            // Only mark as populated if there are actually candidates
            if (MatchListController.Instance.Count > 0)
            {
                queuePopulated = true;
            }
        }

        private void SetProgress(float progress, string status)
        {
            if (progressBarFill != null)
            {
                progressBarFill.style.width = Length.Percent(progress * 100f);
            }

            if (loadingStatus != null)
            {
                loadingStatus.text = status;
            }
        }

        private void ShowCredits()
        {
            if (creditsOverlay != null)
            {
                creditsOverlay.RemoveFromClassList("hidden");
            }
        }

        private void HideCredits()
        {
            if (creditsOverlay != null)
            {
                creditsOverlay.AddToClassList("hidden");
            }
        }

        private void ShowLoading()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.RemoveFromClassList("hidden");
            }
        }

        private void HideLoading()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.AddToClassList("hidden");
            }
        }
    }
}
