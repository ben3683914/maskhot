using Maskhot.Controllers;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Audio.ProcessorInstance.AvailableData;
using static UnityEngine.InputSystem.HID.HID;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private VisualElement m_Root;
    private ListView m_PostList;
    private ListView  m_ProfileList;
    private ListView  m_RequiremenetsList;
    private ListView  m_DealbreakersList;
    private EventRegistry m_EventRegistry;

    [Header("Options")]
    [Tooltip("Enable detailed logging")]
    public bool verboseOutput = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        m_EventRegistry = new EventRegistry();

    }

    void Start()
    {
        m_Root = GetComponent<UIDocument>()?.rootVisualElement;
        m_ProfileList =  m_Root.Q("profile-pane").Q<ListView>();
        m_PostList =  m_Root.Q("posts-pane").Q<ListView>();
        m_RequiremenetsList =  m_Root.Q("requirements").Q<ListView>();
        m_DealbreakersList =  m_Root.Q("dealbreakers").Q<ListView>();

        // Re-subscribe in Start in case controller wasn't ready
        if (MatchListController.Instance != null)
        {
            MatchListController.Instance.OnSelectionChanged -= HandleSelectionChanged;
            MatchListController.Instance.OnSelectionChanged += HandleSelectionChanged;
            MatchListController.Instance.OnQueueUpdated -= HandleQueueUpdated;
            MatchListController.Instance.OnQueueUpdated += HandleQueueUpdated;
        }

        if (QuestController.Instance != null)
        {
            QuestController.Instance.OnQuestChanged -= RefreshCriteria;
            QuestController.Instance.OnQuestChanged += RefreshCriteria;
        }

        if (MoneyController.Instance != null)
        {
            MoneyController.Instance.OnBalanceChanged -= UpdateBalance;
            MoneyController.Instance.OnBalanceChanged += UpdateBalance;
            UpdateBalance(MoneyController.Instance.CurrentBalance);
        }

        RegisterCallbacks();
    }

    private void UpdateBalance(int money)
    {
        Debug.Log("update money: " +  money);
        m_Root.Q<Label>("money").text = $"${money}";
    }

    private void OnEnable()
    {
        if (MatchListController.Instance != null)
        {
            MatchListController.Instance.OnSelectionChanged += HandleSelectionChanged;
            MatchListController.Instance.OnQueueUpdated += HandleQueueUpdated;

        }

        if (QuestController.Instance != null)
        {
            QuestController.Instance.OnQuestChanged += RefreshCriteria;
        }

        if (MoneyController.Instance != null)
        {
            MoneyController.Instance.OnBalanceChanged += UpdateBalance;
        }

        RegisterCallbacks();
    }

    private void OnDisable()
    {
        if (MatchListController.Instance != null)
        {
            MatchListController.Instance.OnSelectionChanged -= HandleSelectionChanged;
            MatchListController.Instance.OnQueueUpdated -= HandleQueueUpdated;
        }

        if(QuestController.Instance != null)
        {
            QuestController.Instance.OnQuestChanged -= RefreshCriteria;
        }

        if (MoneyController.Instance != null)
        {
            MoneyController.Instance.OnBalanceChanged -= UpdateBalance;
        }

        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        if (m_EventRegistry == null) return;

        m_Root = GetComponent<UIDocument>()?.rootVisualElement;
        if (m_Root == null) return;

        var approveButton = m_Root.Q("button-approve");
        var rejectButton = m_Root.Q("button-reject");

        if (approveButton != null)
            m_EventRegistry.RegisterCallback<ClickEvent>(approveButton, evt => DecisionController.Instance.AcceptCurrent());
        if (rejectButton != null)
            m_EventRegistry.RegisterCallback<ClickEvent>(rejectButton, evt => DecisionController.Instance.RejectCurrent());
    }

    private void UnregisterCallbacks()
    {
        m_EventRegistry?.Dispose();
    }

    private void RefreshCriteria()
    {
        if(!QuestController.Instance.HasActiveQuest)
        {
            // ClearPanel();
            Debug.Log("UIManager: RefreshCriteria fired");

            return;
        }

        m_Root.Q<Label>("client-name").text = QuestController.Instance.ClientName;
        m_Root.Q<Label>("client-intro").text = QuestController.Instance.ClientIntroduction;
        m_Root.Q<Label>("client-age-range").text = $"Age: {QuestController.Instance.MinAge}-{QuestController.Instance.MaxAge}";


        //bind requiremenets
        m_RequiremenetsList.dataSource = QuestController.Instance.Requirements.ToList();

        m_RequiremenetsList.SetBinding("itemsSource", new DataBinding
        {
            dataSourcePath = new PropertyPath("")
        });

        m_RequiremenetsList.bindItem = (VisualElement element, int index) => {
            // 1. Get the specific data for this row (use itemsSource to avoid stale closure)
            DisplayRequirement requirement = (DisplayRequirement)m_RequiremenetsList.itemsSource[index];

            // Standard binding (e.g., setting label text)
            element.Q<Label>().text = requirement.hint;
            element.Q<Label>().AddToClassList(requirement.level.ToString().ToLower());
        };

        //bind Dealbreakers
        m_DealbreakersList.dataSource = QuestController.Instance.Dealbreakers.ToList();

        m_DealbreakersList.SetBinding("itemsSource", new DataBinding
        {
            dataSourcePath = new PropertyPath("")
        });

        m_DealbreakersList.bindItem = (VisualElement element, int index) => {
            // 1. Get the specific data for this row (use itemsSource to avoid stale closure)
            var dealbreaker = m_DealbreakersList.itemsSource[index] as string;

            // Standard binding (e.g., setting label text)
            element.Q<Label>().text = dealbreaker;
        };
    }

    private void HandleSelectionChanged(CandidateProfileSO candidate)
    {

        if (verboseOutput)
        {
            string name = candidate != null ? candidate.profile.characterName : "null";
            Debug.Log($"UIManager: OnSelectionChanged fired - {name}");
        }

        updatePostList(MatchListController.Instance.CurrentPosts);
    }

    private void HandleQueueUpdated()
    {
        if (verboseOutput)
        {
            Debug.Log("UIManager: OnQueueUpdated fired");
        }

        updateProfileList(MatchListController.Instance.Queue);
    }

    public void updateProfileList(IReadOnlyList<CandidateProfileSO> candidateProfileSOs)
    {
        var dataSource = candidateProfileSOs.ToList();
        m_ProfileList.dataSource = dataSource;

        m_ProfileList.SetBinding("itemsSource", new DataBinding
        {
            dataSourcePath = new PropertyPath("")
        });

        m_ProfileList.bindItem = (VisualElement element, int index) => {
            // 1. Get the specific data for this row
            var candidate = dataSource[index];

            // 2. Unregister any previous clicks (Crucial for recycled elements!)
            element.UnregisterCallback<ClickEvent>(OnProfileClicked);

            // 3. Attach the new click event using the 'userData' to pass data
            element.userData = candidate;
            element.RegisterCallback<ClickEvent>(OnProfileClicked);

            // Standard binding (e.g., setting label text)
            element.Q("profile-info").Q<Image>().sprite = candidate.GetProfilePicture();
        };

        m_ProfileList.unbindItem = (element, index) => {
            element.UnregisterCallback<ClickEvent>(OnProfileClicked);
        };

        m_ProfileList.RefreshItems();
    }

    public void updatePostList(List<SocialMediaPost> socialMediaPosts)
    {
        m_PostList.dataSource = socialMediaPosts;

        m_PostList.SetBinding("itemsSource", new DataBinding
        {
            dataSourcePath = new PropertyPath("")
        });

        m_PostList.bindItem = (VisualElement element, int index) => {
            // 1. Get the specific data for this row (use itemsSource to avoid stale closure)
            var post = m_PostList.itemsSource[index] as SocialMediaPost;
            if (post == null) return;

            // 2. Unregister any previous clicks (Crucial for recycled elements!)
            element.UnregisterCallback<ClickEvent>(OnPostClicked);

            // 3. Attach the new click event using the 'userData' to pass data
            element.userData = post;
            element.RegisterCallback<ClickEvent>(OnPostClicked);

            // Standard binding (e.g., setting label text)
            var candidate = MatchListController.Instance.CurrentCandidate;
            element.Q("post-header").Q<Label>().text = candidate.profile.characterName; 
            element.Q("post-header").Q<Image>().sprite = candidate.GetProfilePicture();
            element.Q("post-body").Q<Label>().text = RedactionController.Instance.GetDisplayText(candidate, post);
            element.Q<Label>("post-timestamp").text = $"{post.daysSincePosted} days ago";

            // Show/hide post image based on post type
            var postImage = element.Q<Image>("post-image");
            postImage.style.display = post.ShowImage ? DisplayStyle.Flex : DisplayStyle.None;
            if (post.ShowImage)
            {
                postImage.sprite = post.DisplayImage;

                // Override template styles: prevent flex expansion, set width to fill
                postImage.style.flexGrow = 0;
                postImage.style.width = Length.Percent(100);

                // Calculate height based on sprite aspect ratio
                var sprite = post.DisplayImage;
                if (sprite != null && sprite.rect.width > 0)
                {
                    float aspectRatio = sprite.rect.height / sprite.rect.width;

                    // Use ListView width as reference (more reliable than waiting for layout)
                    float listWidth = m_PostList.resolvedStyle.width;
                    if (listWidth > 0)
                    {
                        postImage.style.height = listWidth * aspectRatio;
                    }
                    else
                    {
                        // Fallback: wait for geometry if ListView width not available yet
                        var postBody = element.Q("post-body");
                        void OnGeometryChanged(GeometryChangedEvent evt)
                        {
                            float width = postBody.resolvedStyle.width;
                            if (width > 0)
                                postImage.style.height = width * aspectRatio;
                            postBody.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                        }
                        postBody.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    }
                }
            }
            else
            {
                // Reset styles for non-photo posts
                postImage.style.flexGrow = StyleKeyword.Null;
                postImage.style.width = StyleKeyword.Auto;
                postImage.style.height = StyleKeyword.Auto;
            }
        };

        m_PostList.unbindItem = (element, index) => {
            element.UnregisterCallback<ClickEvent>(OnPostClicked);
        };

        m_PostList.RefreshItems();
        m_PostList.ScrollToItem(0);
    }

    private void OnProfileClicked(ClickEvent evt)
    {
        // Access the VisualElement that was clicked
        VisualElement clickedElement = evt.currentTarget as VisualElement;

        // Access the Data Source attached to it
        var candidate = clickedElement.userData as CandidateProfileSO;

        if (candidate != null)
        {
            Debug.Log($"Selected candidate: {candidate.name}");

            MatchListController.Instance.SelectCandidate(candidate);
        }
    }

    private void OnPostClicked(ClickEvent evt)
    {
        // Access the VisualElement that was clicked
        VisualElement clickedElement = evt.currentTarget as VisualElement;

        // Access the Data Source attached to it
        var post = clickedElement.userData as SocialMediaPost;
        var candidate = MatchListController.Instance.CurrentCandidate;

        if (post != null)
        {
            Debug.Log($"Selected candidate: {post.content}");

            RedactionController.Instance.TryUnredact(candidate, post);
            m_PostList.RefreshItems();

        }
    }
}
