# UI Developer Reference

Reference for building the game UI. **Use Controllers, not Managers directly.**

## Screen Layout

```
┌─────────────────────────────────────────────────────────────┐
│  [Client Info Header]                                       │
├───────────┬─────────────────────────┬──────────────────────┤
│           │                         │                      │
│   LEFT    │        CENTER          │       RIGHT         │
│   PANEL   │        PANEL           │       PANEL         │
│           │                         │                      │
│  Candidate│   Social Media Feed    │  Quest Criteria     │
│   Queue   │                         │                      │
│           │                         │                      │
│           │  [Accept] [Reject]      │                      │
└───────────┴─────────────────────────┴──────────────────────┘
```

---

## MatchListController

**Primary interface for candidate selection and navigation.**

### Properties

```csharp
MatchListController.Instance

// Selection state
CandidateProfileSO CurrentCandidate { get; }  // Currently selected (null if none)
int CurrentIndex { get; }                      // Index in queue (-1 if none)
bool HasSelection { get; }                     // Is a candidate selected?
bool HasNext { get; }                          // Can navigate forward?
bool HasPrevious { get; }                      // Can navigate backward?
List<SocialMediaPost> CurrentPosts { get; }   // Posts for current candidate

// Queue access (for left panel)
IReadOnlyList<CandidateProfileSO> Queue { get; }  // The candidate list
int Count { get; }                                 // Total candidates
int PendingCount { get; }                          // Undecided candidates
```

### Methods

```csharp
// Selection
bool SelectByIndex(int index)
bool SelectCandidate(CandidateProfileSO candidate)
void ClearSelection()

// Navigation
bool SelectNext()
bool SelectPrevious()
bool SelectFirst()
bool SelectNextPending()

// Decision state (for styling)
CandidateDecision GetDecision(CandidateProfileSO candidate)
// Returns: CandidateDecision.Pending, .Accepted, or .Rejected
```

### Events

```csharp
event Action<CandidateProfileSO> OnSelectionChanged;  // Selection changed
event Action OnQueueUpdated;                          // Queue was modified
```

### Usage Example

```csharp
public class SocialFeedPanel : MonoBehaviour
{
    private void OnEnable()
    {
        MatchListController.Instance.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        MatchListController.Instance.OnSelectionChanged -= HandleSelectionChanged;
    }

    private void HandleSelectionChanged(CandidateProfileSO candidate)
    {
        if (candidate == null)
        {
            ClearFeedUI();
            return;
        }

        // Update profile header
        nameLabel.text = candidate.profile.characterName;
        ageLabel.text = $"{candidate.profile.age} years old";
        bioLabel.text = candidate.profile.bio;
        profileImage.style.backgroundImage = new StyleBackground(candidate.GetProfilePicture());

        // Populate posts
        ClearFeedUI();
        foreach (var post in MatchListController.Instance.CurrentPosts)
        {
            CreatePostElement(post);
        }
    }

    // When player clicks a candidate in the queue
    public void OnCandidateClicked(CandidateProfileSO candidate)
    {
        MatchListController.Instance.SelectCandidate(candidate);
    }
}
```

---

## DecisionController

**Primary interface for accept/reject decisions.** Handles decision logic, correctness evaluation, and statistics.

### Properties

```csharp
DecisionController.Instance

bool autoAdvance;              // Auto-advance to next pending after decision
bool AllDecided { get; }       // All candidates decided?

// Session statistics
int CorrectAccepts { get; }    // True positives
int CorrectRejects { get; }    // True negatives
int FalseAccepts { get; }      // False positives
int FalseRejects { get; }      // False negatives
int TotalCorrect { get; }
int TotalIncorrect { get; }
int TotalDecisions { get; }
float Accuracy { get; }        // 0.0 - 1.0
```

### Methods

```csharp
DecisionResult? AcceptCurrent()   // Accept currently selected candidate
DecisionResult? RejectCurrent()   // Reject currently selected candidate
bool HasDecisionFor(CandidateProfileSO candidate)
void ResetSession()               // Clear statistics (called automatically on new quest)
```

### Events

```csharp
event Action<DecisionResult> OnDecisionResult;   // Fired after each decision
event Action OnAllDecisionsComplete;             // Fired when all candidates decided
```

### DecisionResult

Returned from `AcceptCurrent()`/`RejectCurrent()`:

```csharp
result.Candidate        // The candidate
result.Decision         // CandidateDecision.Accepted or .Rejected
result.WasActualMatch   // Was this candidate actually a match?
result.IsCorrect        // Did player make the right call?
result.MatchEvaluation  // Full MatchResult with score and details
result.MatchReason      // Human-readable reason (for feedback)
```

### Usage Example

```csharp
public void OnAcceptClicked()
{
    var result = DecisionController.Instance.AcceptCurrent();
    if (result == null) return;

    if (result.Value.IsCorrect)
    {
        ShowFeedback($"Correct! Score: {result.Value.MatchEvaluation.Score}");
    }
    else
    {
        ShowFeedback($"Wrong! {result.Value.MatchReason}");
    }
    // Note: auto-advance happens automatically if enabled
}

public void OnRejectClicked()
{
    var result = DecisionController.Instance.RejectCurrent();
    if (result == null) return;

    if (result.Value.IsCorrect)
    {
        ShowFeedback("Correct rejection!");
    }
    else
    {
        ShowFeedback($"Wrong! This was a match with score {result.Value.MatchEvaluation.Score}");
    }
}
```

---

## QuestController

**Primary interface for quest/criteria display.**

### Properties

```csharp
QuestController.Instance

bool HasActiveQuest { get; }
string ClientName { get; }
string ClientIntroduction { get; }

// Criteria for display
int MinAge { get; }
int MaxAge { get; }
int MaxRedFlags { get; }
int MinGreenFlags { get; }
string[] Dealbreakers { get; }               // Trait names that auto-fail
CachedRequirement[] Requirements { get; }    // See below
```

### CachedRequirement

```csharp
requirement.hint       // Display text (e.g., "enjoys fine cuisine")
requirement.level      // RequirementLevel.Required, .Preferred, or .Avoid
```

### Events

```csharp
event Action OnQuestChanged;  // Fired when quest starts, completes, or clears
```

### Usage Example

```csharp
public class CriteriaPanel : MonoBehaviour
{
    private void OnEnable()
    {
        QuestController.Instance.OnQuestChanged += RefreshCriteria;
        RefreshCriteria();
    }

    private void OnDisable()
    {
        QuestController.Instance.OnQuestChanged -= RefreshCriteria;
    }

    private void RefreshCriteria()
    {
        if (!QuestController.Instance.HasActiveQuest)
        {
            ClearPanel();
            return;
        }

        clientNameLabel.text = QuestController.Instance.ClientName;
        introLabel.text = QuestController.Instance.ClientIntroduction;
        ageRangeLabel.text = $"Age: {QuestController.Instance.MinAge}-{QuestController.Instance.MaxAge}";

        // Display requirements
        foreach (var req in QuestController.Instance.Requirements)
        {
            var element = CreateRequirementElement(req.hint);
            StyleByLevel(element, req.level);  // Required=bold/red, Preferred=yellow, Avoid=strikethrough
        }

        // Display dealbreakers
        foreach (var dealbreaker in QuestController.Instance.Dealbreakers)
        {
            CreateDealbreakElement(dealbreaker);
        }
    }
}
```

---

## RedactionController

**Primary interface for post redaction/unredaction.**

### Methods

```csharp
RedactionController.Instance

bool IsRedacted(CandidateProfileSO candidate, SocialMediaPost post)
bool IsGuaranteedPost(CandidateProfileSO candidate, SocialMediaPost post)
string GetDisplayText(CandidateProfileSO candidate, SocialMediaPost post)  // Blocks or content
bool TryUnredact(CandidateProfileSO candidate, SocialMediaPost post)

// Counts for UI
int GetRedactedCount(CandidateProfileSO candidate)
int GetVisibleCount(CandidateProfileSO candidate)
int GetTotalCount(CandidateProfileSO candidate)
int GetGuaranteedCount(CandidateProfileSO candidate)
```

### Events

```csharp
event Action<CandidateProfileSO, SocialMediaPost> OnPostUnredacted;
event Action OnRedactionReset;  // When queue changes
```

### Usage Example

```csharp
public class PostCard : MonoBehaviour
{
    private CandidateProfileSO candidate;
    private SocialMediaPost post;

    public void SetPost(CandidateProfileSO candidate, SocialMediaPost post)
    {
        this.candidate = candidate;
        this.post = post;

        contentLabel.text = RedactionController.Instance.GetDisplayText(candidate, post);
        unredactButton.SetActive(RedactionController.Instance.IsRedacted(candidate, post));
    }

    public void OnUnredactClicked()
    {
        // Future: check MoneyManager.CanAfford() first
        RedactionController.Instance.TryUnredact(candidate, post);
        contentLabel.text = RedactionController.Instance.GetDisplayText(candidate, post);
        unredactButton.SetActive(false);
    }
}
```

---

## Data Classes

### CandidateProfileSO

```csharp
candidate.profile.characterName  // string
candidate.profile.gender         // Gender enum
candidate.profile.age            // int
candidate.profile.bio            // string
candidate.GetProfilePicture()    // Sprite (with fallback)
```

### SocialMediaPost

```csharp
post.postType           // PostType: Photo, TextOnly, Story, Video, Poll, SharedPost
post.content            // string
post.postImage          // Sprite (may be null)
post.daysSincePosted    // int (1 = yesterday)
post.likes              // int
post.comments           // int
post.isGreenFlag        // bool
post.isRedFlag          // bool
```

**Timestamp formatting:**
```csharp
string FormatTimestamp(int days)
{
    if (days == 1) return "Yesterday";
    if (days < 7) return $"{days} days ago";
    if (days == 7) return "1 week ago";
    return $"{days / 7} weeks ago";
}
```

---

## Panel Summary

### Left Panel (Candidate Queue)
- **Subscribe to**: `MatchListController.OnQueueUpdated`, `DecisionController.OnDecisionResult`
- **Display**: `MatchListController.Instance.Queue`, decision state via `GetDecision(candidate)`
- **On click**: `MatchListController.Instance.SelectCandidate(candidate)`
- **Highlight**: Compare with `MatchListController.Instance.CurrentCandidate`

### Center Panel (Social Feed)
- **Subscribe to**: `MatchListController.OnSelectionChanged`
- **Display**: `CurrentCandidate.profile`, `CurrentPosts`
- **On Accept**: `DecisionController.Instance.AcceptCurrent()`
- **On Reject**: `DecisionController.Instance.RejectCurrent()`

### Right Panel (Quest Criteria)
- **Subscribe to**: `QuestController.OnQuestChanged`
- **Display**: `ClientName`, `ClientIntroduction`, `Requirements`, `Dealbreakers`

---

## Color Coding

| Element | Color |
|---------|-------|
| Green flags | Green |
| Red flags | Red |
| Required traits | Bold/Red |
| Preferred traits | Yellow/Orange |
| Avoid traits | Gray/Strikethrough |
| Accept button | Green |
| Reject button | Red |
| Pending candidate | Default |
| Accepted candidate | Green tint |
| Rejected candidate | Gray/dimmed |

---

## Recommended Panel Widths

```
Left:   20% (candidate queue)
Center: 60% (social feed)
Right:  20% (quest criteria)
```
