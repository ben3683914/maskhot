# Testing Guide

## Running Tests

All testers have Inspector buttons. **Enter Play Mode**, then click buttons in the Inspector.

## Creating New Testers

When creating a new manager or controller, create a corresponding tester:

1. Create `Assets/Scripts/Testing/[Name]Tester.cs`
2. Create `Assets/Scripts/Editor/[Name]TesterEditor.cs`
3. Add entry to the tester list below

See [templates.md](templates.md) for code templates.

---

## Tester Scripts

| Tester | Location | Purpose |
|--------|----------|---------|
| ProfileTester | `Scripts/Data/ProfileTester.cs` | Verify profile data and trait references |
| RandomPostTester | `Scripts/Testing/RandomPostTester.cs` | Verify random post generation and trait matching |
| ClientTester | `Scripts/Testing/ClientTester.cs` | Verify client profiles and match criteria |
| MatchingTester | `Scripts/Testing/MatchingTester.cs` | Verify matching algorithm and scoring |
| MatchQueueTester | `Scripts/Testing/MatchQueueTester.cs` | Verify queue population and decision tracking |
| MatchListTester | `Scripts/Testing/MatchListTester.cs` | Verify selection and navigation |
| QuestManagerTester | `Scripts/Testing/QuestManagerTester.cs` | Verify quest lifecycle and events |
| QuestControllerTester | `Scripts/Testing/QuestControllerTester.cs` | Verify controller event subscription and caching |
| DecisionControllerTester | `Scripts/Testing/DecisionControllerTester.cs` | Verify decisions and statistics |
| GameManagerTester | `Scripts/Testing/GameManagerTester.cs` | Verify session management and state transitions |
| RedactionManagerTester | `Scripts/Testing/RedactionManagerTester.cs` | Verify redaction data state |
| RedactionControllerTester | `Scripts/Testing/RedactionControllerTester.cs` | Verify redaction UI layer |

Tester editors are in `Assets/Scripts/Editor/[Name]TesterEditor.cs`.

---

## Verbose Logging

Most managers/controllers have a `verboseLogging` Inspector toggle for detailed console output.

---

## Common Issues

**"Reference not found" in logs**
- Trait name in JSON doesn't match loaded trait - fix JSON and reimport

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
