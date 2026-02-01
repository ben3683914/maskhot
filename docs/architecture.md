# Architecture Guidelines

Patterns and conventions for creating new components. See [templates.md](templates.md) for code templates.

## Component Types

### Managers

**Purpose**: Own data and execute business logic.

**Responsibilities**: Load/store data, provide accessors, execute domain logic, fire events on data changes.

**Characteristics**:
- Namespace: `Maskhot.Managers`
- Singleton with `static Instance`
- `MonoBehaviour` with `DontDestroyOnLoad`
- Initialize in `Awake()`

**Examples**: ProfileManager, PostPoolManager, MatchQueueManager, QuestManager, GameManager, RedactionManager

### Controllers

**Purpose**: Manage UI state and provide UI-facing interface.

**Responsibilities**: Track selection/navigation state, subscribe to Manager events, expose events for UI, translate user actions to Manager calls. Never own domain data.

**Characteristics**:
- Namespace: `Maskhot.Controllers`
- Singleton with `static Instance`
- `MonoBehaviour` with `DontDestroyOnLoad`
- Subscribe in `OnEnable`/`Start`, unsubscribe in `OnDisable`

**Examples**: MatchListController, QuestController, DecisionController, RedactionController

### Static Utility Classes

**Purpose**: Stateless helper functions and algorithms.

**Characteristics**: Static class, no `MonoBehaviour`, no state/events.

**Examples**: MatchEvaluator

---

## Dependency Rules

| From | To | Allowed? |
|------|----|----------|
| UI | Controller | ✅ Subscribe to events, call methods |
| UI | Manager | ⚠️ Read-only OK, prefer Controller |
| Controller | Manager | ✅ Call methods, subscribe to events |
| Controller | Controller | ⚠️ Avoid; use shared Manager |
| Manager | Manager | ✅ Call methods, subscribe to events |
| Manager | Controller | ❌ Never |
| Manager | UI | ❌ Never |

**Key Principle**: Managers are unaware of Controllers. Data flows up via events, commands flow down via method calls.

---

## Event Patterns

**Naming**:
- Manager events: `On[Data]Changed`, `On[Action]Completed`
- Controller events: `On[State]Changed`

**Signatures**:
```csharp
public event Action OnQueueChanged;                              // Simple notification
public event Action<CandidateProfileSO> OnSelectionChanged;      // With new value
public event Action<CandidateProfileSO, CandidateDecision> OnDecisionMade;  // With context
```

---

## Singleton Initialization

Check for null before accessing other singletons. Re-subscribe in `Start()` if dependency might not exist in `OnEnable()`. See [templates.md](templates.md) for full pattern.

---

## Decision Guide

| If you need to... | Use a... |
|-------------------|----------|
| Load or store data | Manager |
| Execute business logic | Manager |
| Track current selection | Controller |
| Handle navigation | Controller |
| React to user input | Controller → Manager |
| Notify UI of changes | Events |
| Pure computation | Static utility class |

---

## Checklists

### New Manager
- [ ] Namespace: `Maskhot.Managers`
- [ ] Singleton pattern
- [ ] `DontDestroyOnLoad`
- [ ] Initialize in `Awake()`
- [ ] Does NOT reference Controllers
- [ ] Fires events for data changes
- [ ] Has tester script + editor

### New Controller
- [ ] Namespace: `Maskhot.Controllers`
- [ ] Singleton pattern
- [ ] `DontDestroyOnLoad`
- [ ] Subscribe in `OnEnable`/`Start`
- [ ] Unsubscribe in `OnDisable`
- [ ] Fires events for state changes
- [ ] Has tester script + editor
