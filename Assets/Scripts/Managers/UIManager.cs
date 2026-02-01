using Maskhot.Managers;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{

    private VisualElement m_Root;
    private VisualElement m_ProfilePane;
    private ListView  m_ProfileList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Root = GetComponent<UIDocument>()?.rootVisualElement;
        m_ProfileList =  m_Root.Q("profile-pane").Q("profile-list") as ListView;

        var profiles = ProfileManager.Instance.GetAllCandidates();
        m_ProfileList.dataSource = profiles;

        m_ProfileList.SetBinding("itemsSource", new DataBinding
        {
            dataSourcePath = new PropertyPath("")
        });

        m_ProfileList.RefreshItems();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
