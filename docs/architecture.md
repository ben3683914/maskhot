# Maskhot - Architecture Guidelines

This document defines the architectural patterns and conventions used in the codebase. Follow these guidelines when creating new components.

## Component Types

### Managers

**Purpose**: Own data and execute business logic.

**Responsibilities**:
- Load and store data
- Provide accessor methods for data retrieval
- Execute domain logic and computations
- Fire events when data changes
- May depend on other Managers

**Characteristics**:
- Namespace: `Maskhot.Managers`
- Singleton pattern with `static Instance`
- Inherit from `MonoBehaviour`
- Use `DontDestroyOnLoad` for persistence
- Initialize in `Awake()`

**Examples**: ProfileManager, PostPoolManager, MatchQueueManager

**Template**:
```csharp
namespace Maskhot.Managers
{
    public class ExampleManager : MonoBehaviour
    {
        public static ExampleManager Instance { get; private set; }

        // Data events
        public event Action OnDataChanged;

        // Private data storage
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

        // Public accessor methods
        public SomeData GetData() { ... }

        // Business logic methods
        public void DoSomething() { ... }
    }
}
```

---

### Controllers

**Purpose**: Manage UI state and provide a UI-facing interface.

**Responsibilities**:
- Track selection, navigation, and UI-related state
- Subscribe to Manager events and react to data changes
- Expose events for UI components to subscribe to
- Translate user actions into Manager calls
- Never own domain data (delegate to Managers)

**Characteristics**:
- Namespace: `Maskhot.Controllers`
- Singleton pattern with `static Instance`
- Inherit from `MonoBehaviour`
- Use `DontDestroyOnLoad` for persistence
- Subscribe to Manager events in `OnEnable` / `Start`
- Unsubscribe in `OnDisable`

**Examples**: MatchListController

**Template**:
```csharp
namespace Maskhot.Controllers
{
    public class ExampleController : MonoBehaviour
    {
        public static ExampleController Instance { get; private set; }

        // UI-facing events
        public event Action<SomeData> OnSelectionChanged;

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
            SomeManager.Instance.OnDataChanged += HandleDataChanged;
        }

        private void OnDisable()
        {
            SomeManager.Instance.OnDataChanged -= HandleDataChanged;
        }

        // Selection/navigation methods
        public void Select(SomeData item) { ... }

        // Event handlers
        private void HandleDataChanged() { ... }
    }
}
```

---

### Static Utility Classes

**Purpose**: Stateless helper functions and algorithms.

**Responsibilities**:
- Execute pure computations
- Return results without side effects
- No state, no events, no Unity lifecycle

**Characteristics**:
- Static class with static methods
- No inheritance from `MonoBehaviour`
- Namespace varies by domain (e.g., `Maskhot.Matching`)

**Examples**: MatchEvaluator

---

## Dependency Rules

```
┌─────────────────────────────────────────────────┐
│                   UI Layer                       │
│         (UI Toolkit, MonoBehaviours)            │
└───────────────────────┬─────────────────────────┘
                        │ subscribes to events
                        ▼
┌─────────────────────────────────────────────────┐
│              Controller Layer                    │
│    (MatchListController, future controllers)    │
└───────────────────────┬─────────────────────────┘
                        │ calls methods, subscribes to events
                        ▼
┌─────────────────────────────────────────────────┐
│               Manager Layer                      │
│  (ProfileManager, MatchQueueManager, etc.)      │
└───────────────────────┬─────────────────────────┘
                        │ loads
                        ▼
┌─────────────────────────────────────────────────┐
│                Data Layer                        │
│       (ScriptableObjects, Resources/)           │
└─────────────────────────────────────────────────┘
```

### Allowed Dependencies

| From | To | Allowed? |
|------|----|----------|
| UI | Controller | ✅ Subscribe to events, call methods |
| UI | Manager | ⚠️ Read-only access OK, prefer Controller |
| Controller | Manager | ✅ Call methods, subscribe to events |
| Controller | Controller | ⚠️ Avoid; coordinate via shared Manager |
| Manager | Manager | ✅ Call methods, subscribe to events |
| Manager | Controller | ❌ Never - breaks layering |
| Manager | UI | ❌ Never - use events instead |

### Key Principle

**Managers are unaware of Controllers.** Data flows up via events, commands flow down via method calls. This keeps Managers testable and reusable.

---

## Event Patterns

### When to Use Events

- **Data changed**: Manager fires event when its data is modified
- **State changed**: Controller fires event when selection/navigation changes
- **Process completed**: Manager fires event when async operation finishes

### Naming Conventions

- Manager events: `On[Data]Changed`, `On[Action]Completed`
- Controller events: `On[State]Changed`, `On[UI action]`

### Event Signatures

```csharp
// Simple notification (something changed, re-query if needed)
public event Action OnQueueChanged;

// State change with new value
public event Action<CandidateProfileSO> OnSelectionChanged;

// Action completed with context
public event Action<CandidateProfileSO, CandidateDecision> OnDecisionMade;
```

---

## Singleton Initialization Order

Singletons initialize in `Awake()`. If a singleton depends on another, handle missing references gracefully:

1. **Check for null before accessing**: Always verify `Instance != null`
2. **Re-subscribe in Start()**: If a dependency might not exist in `OnEnable`, also subscribe in `Start()`
3. **Fail gracefully**: Log warnings, don't throw exceptions

Example from MatchListController:
```csharp
private void OnEnable()
{
    if (MatchQueueManager.Instance != null)
    {
        MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
    }
}

private void Start()
{
    // Re-subscribe in case MatchQueueManager wasn't ready in OnEnable
    if (MatchQueueManager.Instance != null)
    {
        MatchQueueManager.Instance.OnQueueChanged -= HandleQueueChanged;
        MatchQueueManager.Instance.OnQueueChanged += HandleQueueChanged;
    }
}
```

---

## Folder Structure

```
Assets/Scripts/
├── Controllers/     # UI state controllers
├── Data/            # Data classes, ScriptableObjects
├── Editor/          # Editor-only scripts
├── Managers/        # Data/logic managers
├── Matching/        # Matching system (MatchEvaluator, etc.)
├── Testing/         # Tester scripts
└── UI/              # Future UI components
```

---

## Decision Guide

**"Where does this code belong?"**

| If you need to... | Use a... |
|-------------------|----------|
| Load or store persistent data | Manager |
| Execute business logic | Manager |
| Track what's currently selected | Controller |
| Handle navigation (next/previous) | Controller |
| React to user input | Controller → Manager |
| Notify UI of changes | Events (from Controller or Manager) |
| Compute a result without side effects | Static utility class |

---

## Checklist for New Components

### New Manager
- [ ] Namespace is `Maskhot.Managers`
- [ ] Implements singleton pattern
- [ ] Uses `DontDestroyOnLoad`
- [ ] Initializes in `Awake()`
- [ ] Does not reference any Controllers
- [ ] Fires events for data changes
- [ ] Has corresponding tester script

### New Controller
- [ ] Namespace is `Maskhot.Controllers`
- [ ] Implements singleton pattern
- [ ] Uses `DontDestroyOnLoad`
- [ ] Subscribes to Manager events in `OnEnable` and `Start`
- [ ] Unsubscribes in `OnDisable`
- [ ] Fires events for UI state changes
- [ ] Has corresponding tester script

---

## Related Docs

- [overview.md](overview.md) - System diagrams
- [testing.md](testing.md) - Tester script conventions
- [project-status.md](project-status.md) - Implementation status
