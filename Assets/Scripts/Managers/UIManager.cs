using Maskhot.Controllers;
using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Utilities;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.InputSystem.HID.HID;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private VisualElement m_Root;
    private ListView m_PostList;
    private ListView  m_ProfileList;
    private EventRegistry m_EventRegistry;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Root = GetComponent<UIDocument>()?.rootVisualElement;
        m_ProfileList =  m_Root.Q("profile-pane").Q<ListView>();
        m_PostList =  m_Root.Q("posts-pane").Q<ListView>();
    }

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
    void OnEnable()
    {

        RegisterCallbacks();
    }

    void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        // Slider callbacks
        //m_EventRegistry.RegisterCallback<ChangeEvent<float>>(m_Slider, SliderChangeHandler);

        //// TextField callbacks
        //m_EventRegistry.RegisterCallback<ChangeEvent<string>>(m_TextField, TextFieldInputHandler);
        //m_EventRegistry.RegisterCallback<FocusEvent>(m_TextField, TextFieldFocusHandler);

        //// Button callbacks

        //// Using the EventRegistry allows you to unregister the callback, even if it's a lambda 
        

        //// The GeometryChangedEvent invokes when the Button changes layout (position/size)
        //m_EventRegistry.RegisterCallback<GeometryChangedEvent>(m_Button, ButtonGeometryChangedHandler);

    }

    private void UnregisterCallbacks()
    {
        //m_EventRegistry.Dispose();
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
            element.Q("post-header").Q<Label>().text = RedactionController.Instance.GetDisplayText(candidate, post);
            element.Q("post-header").Q<Image>().sprite = candidate.GetProfilePicture();

            // Show/hide post image based on post type
            var postImage = element.Q<Image>("post-image");
            postImage.style.display = post.ShowImage ? DisplayStyle.Flex : DisplayStyle.None;
            if (post.ShowImage)
                postImage.sprite = post.DisplayImage;
        };

        m_PostList.unbindItem = (element, index) => {
            element.UnregisterCallback<ClickEvent>(OnPostClicked);
        };

        m_PostList.RefreshItems();
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
