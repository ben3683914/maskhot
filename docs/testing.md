# Testing Guide

How to verify each system works correctly using tester scripts and verbose logging.

## When to Create/Update Tests

### Create New Tests When:
- Adding a new **Manager** (e.g., GameManager, QuestManager)
- Adding a new **Controller** (e.g., DecisionController, MatchListController)
- Adding a new system with testable logic

### Update Existing Tests When:
- Modifying a system's public API (new methods, changed signatures)
- Adding new features to an existing manager/controller
- Fixing bugs (add test coverage for the bug scenario)

### Checklist for New/Updated Testers

When creating or updating a test script:
1. **Create the tester script** - `Assets/Scripts/Testing/[Name]Tester.cs`
2. **Create the custom editor** - `Assets/Scripts/Editor/[Name]TesterEditor.cs` with Inspector buttons
3. **Document in the tester's summary** - Setup steps, how to run, what to verify
4. **Add to testing.md** - Add a section describing available tests

---

## Test Script Conventions

Follow these patterns for consistency with existing testers.

### File Locations

- **Tester scripts**: `Assets/Scripts/Testing/[SystemName]Tester.cs`
- **Custom editors**: `Assets/Scripts/Editor/[SystemName]TesterEditor.cs`

### Basic Structure

```csharp
using UnityEngine;
using System.Text;

namespace Maskhot.Testing
{
    public class MySystemTester : MonoBehaviour
    {
        [Header("Test Targets")]
        [Tooltip("Specific assets to test (optional)")]
        public MyAssetType[] testAssets;

        [Header("Options")]
        public bool verboseOutput = false;

        [ContextMenu("Test Specific Asset")]
        private void TestSpecific()
        {
            if (testAssets == null || testAssets.Length == 0)
            {
                Debug.LogWarning("MySystemTester: No assets assigned");
                return;
            }
            TestAsset(testAssets[0]);
        }

        [ContextMenu("Test All Assigned")]
        private void TestAllAssigned()
        {
            foreach (var asset in testAssets)
            {
                TestAsset(asset);
            }
        }

        [ContextMenu("Test All (from Resources)")]
        private void TestAllFromResources()
        {
            // Load from ProfileManager or Resources
            var allAssets = ProfileManager.Instance.GetAllX();
            foreach (var asset in allAssets)
            {
                TestAsset(asset);
            }
        }

        private void TestAsset(MyAssetType asset)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== TESTING: {asset.name} ===");
            sb.AppendLine();

            // Section 1
            sb.AppendLine("--- BASIC INFO ---");
            sb.AppendLine($"  Name: {asset.displayName}");
            sb.AppendLine($"  Type: {asset.type}");
            sb.AppendLine();

            // Section 2
            sb.AppendLine("--- RELATIONSHIPS ---");
            if (asset.relatedItems != null && asset.relatedItems.Length > 0)
            {
                foreach (var item in asset.relatedItems)
                {
                    sb.AppendLine($"  - {item.name}");
                }
            }
            else
            {
                sb.AppendLine("  (none)");
            }
            sb.AppendLine();

            // Verbose details (optional)
            if (verboseOutput)
            {
                sb.AppendLine("--- VERBOSE DETAILS ---");
                // Additional detailed output
                sb.AppendLine();
            }

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }
    }
}
```

### Output Formatting Rules

1. **Use StringBuilder** - Consolidate all output into a single `Debug.Log()` call for clean console logs

2. **Section Headers** - Use clear separators for logical groupings:
   ```
   === MAIN HEADER ===
   --- Section Name ---
   ```

3. **Indentation** - Use consistent spacing for hierarchy:
   ```
   --- SECTION ---
     Property: value
       - Sub-item 1
       - Sub-item 2
   ```

4. **Blank Lines** - Add blank lines between sections for readability

5. **Null Handling** - Always check for null and show "(none)" or "(not set)" instead of crashing

6. **Summary Counts** - Include counts where helpful:
   ```
   --- POSTS (3 total) ---
   ```

### Custom Editor with Inspector Buttons

**Every tester script must have a corresponding custom editor** in `Assets/Scripts/Editor/` that displays buttons in the Inspector.

**Required pattern:**
1. Create `[TesterName]Editor.cs` in `Assets/Scripts/Editor/`
2. Use `[CustomEditor(typeof(TesterClass))]` attribute
3. Add buttons for each test method using `GUILayout.Button()`
4. Disable buttons when not in Play Mode

**Example editor:**
```csharp
using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(MyTester))]
    public class MyTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MyTester tester = (MyTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Something"))
            {
                tester.TestSomething();
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

**Button organization:**
- **Test Specific X** - Test first assigned asset (quick single test)
- **Test All Assigned** - Test all assets in the Inspector array
- **Test All (from Resources/Manager)** - Test everything loaded in the system
- Add a space before utility buttons (Log State, Clear, etc.)

### Inspector Fields

```csharp
[Header("Test Targets")]
public MyAssetType[] testAssets;           // Optional specific assets

[Header("Options")]
public bool verboseOutput = false;         // Toggle detailed output
public bool showBreakdown = false;         // Toggle score/detail breakdown
```

---

## Existing Tester Scripts

All testers have Inspector buttons for running tests. Enter Play Mode, then click the buttons in the Inspector.

### ProfileTester

Verifies profile data and trait references.

**Location**: `Assets/Scripts/Data/ProfileTester.cs` *(legacy location)*

**Setup**:
1. Attach to a GameObject
2. Drag candidate profiles into the `testProfiles` array

**Available Tests**:
- **Test All Profiles** - Tests all assigned candidates (works in Edit or Play mode)

**What it checks**:
- Profile data (name, age, gender, bio)
- Trait references are valid
- Post data and flag counts
- Consolidated output per profile

---

### RandomPostTester

Verifies random post generation and trait matching.

**Location**: `Assets/Scripts/Testing/RandomPostTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager and PostPoolManager
2. Optionally drag specific candidates to test
3. Enter Play Mode

**Available Tests**:
- **Test Specific Candidate** - Tests first assigned candidate
- **Test All Assigned Candidates** - Tests all assigned candidates
- **Test All Candidates (via ProfileManager)** - Tests all loaded candidates
- **Reset Post Pool** - Clears used post tracking

**What it checks**:
- Post count within range
- Trait matching scores
- Engagement generation
- Flag distribution
- Session uniqueness

---

### ClientTester

Verifies client profiles and match criteria.

**Location**: `Assets/Scripts/Testing/ClientTester.cs`

**Setup**:
1. Attach to a GameObject
2. Optionally drag client profiles to test
3. Enter Play Mode

**Available Tests**:
- **Test Specific Client** - Tests first assigned client
- **Test All Assigned Clients** - Tests all assigned clients
- **Test All Clients (from Resources)** - Tests all loaded clients

**What it checks**:
- Client info (name, relationship, backstory)
- Introduction text
- Match criteria structure
- Trait requirements with narrative hints
- Dealbreakers and flag tolerance
- Scoring weights

---

### MatchingTester

Verifies the matching algorithm.

**Location**: `Assets/Scripts/Testing/MatchingTester.cs`

**Setup**:
1. Attach to a GameObject
2. Optionally assign candidates and clients to test
3. Enter Play Mode

**Inspector Settings**:
- `requirementMode` - Algorithm mode to test
- `showScoreBreakdown` - Include detailed score breakdown
- `verboseRequirements` - Show all requirement details

**Available Tests**:
- **Test Specific Match** - Tests first candidate against first client
- **Test All Assigned (Candidates x Clients)** - Tests all combinations
- **Test All (from Resources)** - Tests all loaded candidates x clients
- **Quick Summary (from Resources)** - Summary view of all matches
- **Cycle Algorithm Mode** - Switch between ExplicitThreshold/ImplicitSoftening/ScoringOnly

**What it checks**:
- Pass/fail status
- Match scores
- Met/failed requirements
- Green/red flag counts
- Score breakdown (personality, interests, lifestyle)
- Failure reasons

---

### MatchQueueTester

Verifies MatchQueueManager functionality.

**Location**: `Assets/Scripts/Testing/MatchQueueTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager and MatchQueueManager
2. Optionally assign a client for quest-based population tests
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test Random Population** - Populates queue with random candidates
- **Test Quest Population** - Populates queue balanced for a quest
- **Test Decision Tracking** - Tests accept/reject/reset decisions
- **Test Query Methods** - Tests GetCandidateAt, GetIndexOf, IsInQueue
- **Clear Queue** - Clears the queue
- **Log Current State** - Logs current queue state

---

### MatchListTester

Verifies MatchListController functionality.

**Location**: `Assets/Scripts/Testing/MatchListTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager, MatchQueueManager, and MatchListController
2. Optionally assign test candidates
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test Selection By Index** - Tests SelectByIndex and event firing
- **Test Navigation** - Tests SelectNext, SelectPrevious, SelectFirst
- **Test Select Next Pending** - Tests auto-advancing to next undecided
- **Test Current Posts** - Tests CurrentPosts property
- **Test Clear Selection** - Tests ClearSelection
- **Log Current State** - Logs current controller state

---

### QuestManagerTester

Verifies QuestManager functionality.

**Location**: `Assets/Scripts/Testing/QuestManagerTester.cs`

**Setup**:
1. Attach to a GameObject alongside QuestManager
2. Optionally assign a test client
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test Client Loading** - Verifies clients load from Resources
- **Test Start Quest** - Tests starting a quest from a ClientProfileSO
- **Test Quest Lifecycle** - Tests start, complete, and clear events
- **Test Quest with Queue Population** - Tests integration with MatchQueueManager
- **Log Current State** - Logs current quest state
- **Clear Current Quest** - Clears the active quest

---

### QuestControllerTester

Verifies QuestController functionality.

**Location**: `Assets/Scripts/Testing/QuestControllerTester.cs`

**Setup**:
1. Attach to a GameObject alongside QuestManager and QuestController
2. Optionally assign a test client
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test Event Subscription** - Verifies controller receives QuestManager events
- **Test Cached Data** - Tests that requirements/hints are cached consistently
- **Test Cache Clearing** - Tests that cache is cleared when quest ends
- **Log Current State** - Logs current controller state
- **Start Test Quest** - Convenience button to start a quest
- **Clear Quest** - Convenience button to clear the quest

---

### DecisionControllerTester

Verifies DecisionController functionality.

**Location**: `Assets/Scripts/Testing/DecisionControllerTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager, MatchQueueManager, MatchListController, QuestManager, and DecisionController
2. Optionally assign a test client
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test Accept Current** - Tests accepting the currently selected candidate
- **Test Reject Current** - Tests rejecting the currently selected candidate
- **Test Decide All** - Decides on all candidates, verifies completion event
- **Test Auto-Advance** - Tests auto-advancing to next pending after decision
- **Test Statistics** - Shows current session statistics (TP, TN, FP, FN, accuracy)
- **Test Session Reset** - Tests clearing session statistics
- **Log Current State** - Logs current controller state
- **Start Test Quest** - Starts a quest and populates queue for testing
- **Clear Setup** - Clears quest and queue

**What it checks**:
- Decisions are recorded correctly (accept/reject)
- Correctness evaluation works (comparing to quest criteria)
- Statistics update properly (true/false positives/negatives)
- Events fire on decision and completion
- Auto-advance works when enabled
- Session resets on new quest

---

### RedactionManagerTester

Verifies RedactionManager (data layer) functionality.

**Location**: `Assets/Scripts/Testing/RedactionManagerTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager, MatchQueueManager, and RedactionManager
2. Optionally assign a test candidate
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test IsUnredacted** - Verifies posts start as not unredacted
- **Test MarkUnredacted** - Verifies marking a post and event firing
- **Test MarkUnredacted Twice** - Verifies idempotency
- **Test ResetAll** - Verifies reset clears all state
- **Log Current State** - Logs unredacted count
- **Populate Test Queue** - Adds 3 random candidates for testing
- **Clear Queue** - Clears the queue

**What it checks**:
- IsUnredacted returns false for new posts
- MarkUnredacted adds posts to the set and fires event
- MarkUnredacted returns false for already unredacted posts
- ResetAll clears all state and fires event
- UnredactedCount property is accurate

---

### RedactionControllerTester

Verifies RedactionController (UI-facing layer) functionality.

**Location**: `Assets/Scripts/Testing/RedactionControllerTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager, PostPoolManager, MatchQueueManager, RedactionManager, and RedactionController
2. Optionally assign a test candidate
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Test Guaranteed Posts** - Verifies guaranteed posts are never redacted
- **Test Random Posts Redacted** - Verifies random posts start redacted
- **Test GetDisplayText** - Verifies correct content is returned
- **Test TryUnredact** - Verifies unredaction works and fires event
- **Test Counts** - Verifies count methods work correctly
- **Log Current State** - Logs redaction state for all candidates in queue
- **Populate Test Queue** - Adds 3 random candidates for testing
- **Clear Queue** - Clears the queue

**What it checks**:
- Guaranteed posts are never redacted (IsRedacted returns false)
- Random posts start redacted (IsRedacted returns true)
- GetDisplayText returns blocks for redacted, content for visible
- TryUnredact reveals content and fires event with candidate info
- Counts are calculated correctly (redacted, visible, total, guaranteed)

---

### GameManagerTester

Verifies GameManager functionality.

**Location**: `Assets/Scripts/Testing/GameManagerTester.cs`

**Setup**:
1. Attach to a GameObject alongside GameManager, QuestManager, MatchQueueManager, DecisionController, and MatchListController
2. Set `testQuestCount` for how many quests to use in session tests
3. Enter Play Mode

**How to Run**: Click the buttons in the Inspector (buttons are enabled during Play Mode)

**Available Tests**:
- **Log Current State** - Logs current GameManager state and session stats
- **Test Start Session** - Tests starting a session with N random clients
- **Test Begin Quest** - Tests beginning the next quest in a session
- **Test Complete Quest (Simulate Decisions)** - Simulates all decisions and verifies quest completion
- **Test Full Session Flow** - Runs a complete session from start to GameOver
- **Test State Transitions** - Tests all state changes (Idle → BetweenQuests → InQuest → etc.)
- **Test Reset Session** - Tests that reset clears all state

**What it checks**:
- Session starts correctly with client list
- State transitions fire OnStateChanged events
- Quest progression (BeginNextQuest loads quest and populates queue)
- Quest completion triggers OnQuestWon or OnQuestLost based on accuracy
- Session ends with OnSessionEnded after all quests
- Auto-advance works with configurable delay
- Reset clears all session state

---

## Verbose Logging

Enable detailed logging on manager components.

### ProfileManager

```csharp
// In Inspector, enable verboseLogging
public bool verboseLogging = false;
```

**Outputs**:
- All loaded interests with categories and weights
- All personality traits with opposite/complementary traits
- All lifestyle traits with conflicts/compatibles
- All candidate profiles with traits and posts
- All narrative hint collections

### PostPoolManager

```csharp
// In Inspector, enable verboseLogging
public bool verboseLogging = false;
```

**Outputs**:
- Pool load status
- Post selection for each candidate (trait-matched vs wild card)
- Engagement generation details
- Pool exhaustion warnings

### MatchQueueManager

```csharp
// In Inspector, enable verboseLogging
public bool verboseLogging = false;
```

**Outputs**:
- Queue population details
- Decision changes (accept/reject/reset)
- Queue cleared notifications

### MatchListController

```csharp
// In Inspector, enable verboseLogging
public bool verboseLogging = false;
```

**Outputs**:
- Selection changes (previous → new)
- Post count for current candidate
- Navigation attempts and results
- Skip messages when same candidate selected

### DecisionController

```csharp
// In Inspector, enable verboseLogging
public bool verboseLogging = false;
```

**Outputs**:
- Decision outcomes (candidate name, decision, correct/incorrect)
- Match evaluation details (was match, score)
- Session completion with final accuracy
- Quest start/clear notifications

---

## Testing Workflow

### After JSON Import

1. Enable `verboseLogging` on ProfileManager
2. Enter Play mode
3. Check console for data dump
4. Verify expected counts and references

### Testing Random Posts

1. Add ProfileManager, PostPoolManager, RandomPostTester to a GameObject
2. Enter Play mode
3. Click "Test All Candidates (via ProfileManager)" button in Inspector
4. Check output for trait matching and engagement

### Testing Matching

1. Add MatchingTester to a GameObject
2. Enable `showScoreBreakdown` and `verboseRequirements`
3. Enter Play mode
4. Click "Test All (from Resources)" button in Inspector
5. Cycle through algorithm modes to compare

### Testing Queue and Selection

1. Add ProfileManager, PostPoolManager, MatchQueueManager, MatchListController to a GameObject
2. Add MatchQueueTester and MatchListTester
3. Enter Play mode
4. Click "Test Random Population" button on MatchQueueTester
5. Click "Test Navigation" button on MatchListTester
6. Verify events fire and selection updates correctly

---

## Common Issues

**"Reference not found" in logs**
- Trait name in JSON doesn't match loaded trait
- Run import again after fixing JSON

**Posts not generating**
- PostPoolManager not in scene
- `randomPostMin/Max` set to 0 on candidate
- Pool exhausted (call `ResetPool()`)

**Match always failing**
- Check dealbreakers
- Check Required trait threshold
- Try ScoringOnly mode to see scores

**Cached posts not updating**
- Call `ProfileManager.Instance.ResetAllCandidatePosts()`
- Call `PostPoolManager.Instance.ResetPool()`

---

## File Locations

All new tester scripts should go in `Assets/Scripts/Testing/`.

**Existing testers:**
- **ProfileTester**: `Assets/Scripts/Data/ProfileTester.cs` *(legacy location)*
- **RandomPostTester**: `Assets/Scripts/Testing/RandomPostTester.cs`
- **ClientTester**: `Assets/Scripts/Testing/ClientTester.cs`
- **MatchingTester**: `Assets/Scripts/Testing/MatchingTester.cs`
- **MatchQueueTester**: `Assets/Scripts/Testing/MatchQueueTester.cs`
- **MatchListTester**: `Assets/Scripts/Testing/MatchListTester.cs`
- **QuestManagerTester**: `Assets/Scripts/Testing/QuestManagerTester.cs`
- **QuestControllerTester**: `Assets/Scripts/Testing/QuestControllerTester.cs`
- **DecisionControllerTester**: `Assets/Scripts/Testing/DecisionControllerTester.cs`
- **GameManagerTester**: `Assets/Scripts/Testing/GameManagerTester.cs`
- **RedactionManagerTester**: `Assets/Scripts/Testing/RedactionManagerTester.cs`
- **RedactionControllerTester**: `Assets/Scripts/Testing/RedactionControllerTester.cs`

**Tester editors** (in `Assets/Scripts/Editor/`):
- `ProfileTesterEditor.cs`
- `RandomPostTesterEditor.cs`
- `ClientTesterEditor.cs`
- `MatchingTesterEditor.cs`
- `MatchQueueTesterEditor.cs`
- `MatchListTesterEditor.cs`
- `QuestManagerTesterEditor.cs`
- `QuestControllerTesterEditor.cs`
- `DecisionControllerTesterEditor.cs`
- `GameManagerTesterEditor.cs`
- `RedactionManagerTesterEditor.cs`
- `RedactionControllerTesterEditor.cs`
