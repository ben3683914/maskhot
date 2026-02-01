# Code Templates

Reference templates for creating new components. See [architecture.md](architecture.md) for patterns and rules.

## Manager Template

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Maskhot.Managers
{
    public class ExampleManager : MonoBehaviour
    {
        public static ExampleManager Instance { get; private set; }

        // Events
        public event Action OnDataChanged;

        // Inspector settings
        public bool verboseLogging = false;

        // Private data
        private List<SomeData> data = new List<SomeData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadData();
        }

        private void LoadData()
        {
            // Load from Resources or other source
        }

        // Public accessor methods
        public SomeData GetData() { /* ... */ }

        // Business logic methods
        public void DoSomething()
        {
            // Modify data...
            OnDataChanged?.Invoke();
        }
    }
}
```

## Controller Template

```csharp
using UnityEngine;
using System;
using Maskhot.Managers;

namespace Maskhot.Controllers
{
    public class ExampleController : MonoBehaviour
    {
        public static ExampleController Instance { get; private set; }

        // UI-facing events
        public event Action<SomeData> OnSelectionChanged;

        // Inspector settings
        public bool verboseLogging = false;

        // UI state (not domain data)
        private SomeData currentSelection;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (SomeManager.Instance != null)
            {
                SomeManager.Instance.OnDataChanged += HandleDataChanged;
            }
        }

        private void Start()
        {
            // Re-subscribe in case manager wasn't ready in OnEnable
            if (SomeManager.Instance != null)
            {
                SomeManager.Instance.OnDataChanged -= HandleDataChanged;
                SomeManager.Instance.OnDataChanged += HandleDataChanged;
            }
        }

        private void OnDisable()
        {
            if (SomeManager.Instance != null)
            {
                SomeManager.Instance.OnDataChanged -= HandleDataChanged;
            }
        }

        // Selection/navigation methods
        public void Select(SomeData item)
        {
            currentSelection = item;
            OnSelectionChanged?.Invoke(item);
        }

        // Event handlers
        private void HandleDataChanged()
        {
            // React to manager data changes
        }
    }
}
```

## Tester Template

```csharp
using UnityEngine;
using System.Text;
using Maskhot.Managers;

namespace Maskhot.Testing
{
    public class ExampleTester : MonoBehaviour
    {
        [Header("Test Targets")]
        public SomeAsset[] testAssets;

        [Header("Options")]
        public bool verboseOutput = false;

        public void TestSomething()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TEST: Something ===");
            sb.AppendLine();

            // Test logic here...
            sb.AppendLine("Result: PASS/FAIL");

            sb.AppendLine();
            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        public void LogCurrentState()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CURRENT STATE ===");
            // Log state...
            Debug.Log(sb.ToString());
        }
    }
}
```

## Tester Editor Template

```csharp
using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(ExampleTester))]
    public class ExampleTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ExampleTester tester = (ExampleTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Something"))
            {
                tester.TestSomething();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
```

## Output Formatting

Use StringBuilder for clean console output:

```
=== MAIN HEADER ===

--- Section Name ---
  Property: value
    - Sub-item 1
    - Sub-item 2

--- Another Section (3 total) ---
  Item 1
  Item 2
  Item 3

=== END TEST ===
```

Rules:
- Single `Debug.Log()` call per test
- Clear section separators
- Consistent indentation (2 spaces)
- Blank lines between sections
- Show "(none)" for empty collections
- Include counts where helpful
