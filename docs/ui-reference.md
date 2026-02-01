# UI Developer Reference

Actionable reference for building the game UI with Unity UI Toolkit.

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

## MatchQueueManager

**Manages the queue of candidates for the current session.** Handles queue population, decision tracking, and candidate filtering.

### Properties

```csharp
// Singleton access
MatchQueueManager.Instance

// The queue (read-only)
IReadOnlyList<CandidateProfileSO> Queue { get; }

// Counts
int Count { get; }           // Total candidates in queue
int PendingCount { get; }    // Undecided candidates
int AcceptedCount { get; }   // Accepted candidates
int RejectedCount { get; }   // Rejected candidates

// State
bool HasCandidates { get; }  // Queue is not empty
bool AllDecided { get; }     // All candidates have been decided
```

### Methods

```csharp
// Queue population
void PopulateRandom(int count = 0)              // Fill with random candidates
void PopulateForQuest(Quest quest, int count = 0)  // Fill with balanced mix for quest
void ClearQueue()                               // Clear queue and decisions

// Decision tracking
CandidateDecision GetDecision(CandidateProfileSO candidate)
void Accept(CandidateProfileSO candidate)
void Reject(CandidateProfileSO candidate)
void ResetDecision(CandidateProfileSO candidate)

// Query
CandidateProfileSO GetCandidateAt(int index)
int GetIndexOf(CandidateProfileSO candidate)
bool IsInQueue(CandidateProfileSO candidate)
List<CandidateProfileSO> GetPendingCandidates()
List<CandidateProfileSO> GetAcceptedCandidates()
List<CandidateProfileSO> GetRejectedCandidates()
```

### Events

```csharp
// Fired when queue is modified (populated, cleared)
event Action OnQueueChanged;

// Fired when a decision is made
event Action<CandidateProfileSO, CandidateDecision> OnDecisionMade;
```

---

## MatchListController

**The main controller for candidate selection and navigation.** Singleton that manages the currently selected candidate and provides UI-facing state.

### Properties

```csharp
// Singleton access
MatchListController.Instance

// Current selection
CandidateProfileSO CurrentCandidate { get; }  // Current candidate (null if none)
int CurrentIndex { get; }                      // Index in queue (-1 if none)
bool HasSelection { get; }                     // Quick check if selected

// Navigation state
bool HasNext { get; }      // Can navigate forward
bool HasPrevious { get; }  // Can navigate backward

// Post data for current candidate
List<SocialMediaPost> CurrentPosts { get; }
```

### Methods

```csharp
// Selection
bool SelectByIndex(int index)                  // Select by queue index
bool SelectCandidate(CandidateProfileSO candidate)  // Select specific candidate
void ClearSelection()                          // Clear current selection

// Navigation
bool SelectNext()          // Move to next candidate
bool SelectPrevious()      // Move to previous candidate
bool SelectFirst()         // Select first in queue
bool SelectNextPending()   // Select next undecided candidate
```

### Events

```csharp
// Fired when selection changes
event Action<CandidateProfileSO> OnSelectionChanged;

// Fired when queue is updated
event Action OnQueueUpdated;
```

### Usage Examples

**Option 1: Subscribe to events (recommended for UI Toolkit)**

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
        var profile = candidate.profile;
        nameLabel.text = profile.characterName;
        ageLabel.text = $"{profile.age} years old";
        bioLabel.text = profile.bio;
        profileImage.style.backgroundImage = new StyleBackground(candidate.GetProfilePicture());

        // Populate posts
        ClearFeedUI();
        foreach (var post in MatchListController.Instance.CurrentPosts)
        {
            CreatePostElement(post);
        }
    }
}
```

**Option 2: Direct access (for one-off reads)**

```csharp
// Check if there's a candidate
if (MatchListController.Instance.HasSelection)
{
    var candidate = MatchListController.Instance.CurrentCandidate;
    var posts = MatchListController.Instance.CurrentPosts;
    // ...
}
```

**Selecting a candidate (from left panel)**

```csharp
// When player clicks a candidate in the queue
public void OnCandidateClicked(CandidateProfileSO candidate)
{
    MatchListController.Instance.SelectCandidate(candidate);
}

// Or by index
public void OnCandidateClickedByIndex(int index)
{
    MatchListController.Instance.SelectByIndex(index);
}
```

**Navigation**

```csharp
// Navigate through candidates
nextButton.clicked += () => MatchListController.Instance.SelectNext();
prevButton.clicked += () => MatchListController.Instance.SelectPrevious();

// Auto-advance to next pending after decision
MatchQueueManager.Instance.Accept(currentCandidate);
MatchListController.Instance.SelectNextPending();
```

---

## ProfileManager

Access profiles and traits.

```csharp
// Get all candidates for the queue
CandidateProfileSO[] candidates = ProfileManager.Instance.GetAllCandidates();

// Get a specific candidate by name
CandidateProfileSO candidate = ProfileManager.Instance.GetCandidateByName("Sarah Mitchell");

// Reset all cached posts (call when starting new quest)
ProfileManager.Instance.ResetAllCandidatePosts();
```

---

## CandidateProfileSO

Access candidate data.

```csharp
// Profile data
candidate.profile.characterName  // string
candidate.profile.gender         // Gender enum
candidate.profile.age            // int
candidate.profile.bio            // string
candidate.profile.archetype      // PersonalityArchetype enum

// Traits (arrays)
candidate.profile.personalityTraits  // PersonalityTraitSO[]
candidate.profile.interests          // InterestSO[]
candidate.profile.lifestyleTraits    // LifestyleTraitSO[]

// Profile picture (with gender-based fallback)
Sprite picture = candidate.GetProfilePicture();

// Posts (guaranteed + random, cached per session)
List<SocialMediaPost> posts = candidate.GetPostsForPlaythrough();
```

---

## SocialMediaPost

Display post data.

```csharp
post.postType           // PostType enum: Photo, TextOnly, Story, Video, Poll, SharedPost
post.content            // string - the post text
post.postImage          // Sprite - image for photo posts (may be null)
post.daysSincePosted    // int - 1 = yesterday, 7 = week ago
post.likes              // int
post.comments           // int
post.isGreenFlag        // bool - show green indicator
post.isRedFlag          // bool - show red indicator
```

**Timestamp formatting:**
```csharp
string FormatTimestamp(int daysSincePosted)
{
    if (daysSincePosted == 1) return "Yesterday";
    if (daysSincePosted < 7) return $"{daysSincePosted} days ago";
    if (daysSincePosted == 7) return "1 week ago";
    return $"{daysSincePosted / 7} weeks ago";
}
```

---

## RedactionController

**Handles post redaction/unredaction for the UI.** Use this instead of RedactionManager directly.

### Properties

```csharp
// Singleton access
RedactionController.Instance
```

### Methods

```csharp
// Check if a post is redacted
bool IsRedacted(CandidateProfileSO candidate, SocialMediaPost post)

// Check if a post is guaranteed (never redacted)
bool IsGuaranteedPost(CandidateProfileSO candidate, SocialMediaPost post)

// Get display text (returns blocks for redacted, content for visible)
string GetDisplayText(CandidateProfileSO candidate, SocialMediaPost post)

// Attempt to unredact a post
bool TryUnredact(CandidateProfileSO candidate, SocialMediaPost post)

// Get counts for UI display
int GetRedactedCount(CandidateProfileSO candidate)
int GetVisibleCount(CandidateProfileSO candidate)
int GetTotalCount(CandidateProfileSO candidate)
int GetGuaranteedCount(CandidateProfileSO candidate)
```

### Events

```csharp
// Fired when a post is unredacted
event Action<CandidateProfileSO, SocialMediaPost> OnPostUnredacted;

// Fired when redaction state is reset (queue changed)
event Action OnRedactionReset;
```

### Usage Example

```csharp
public class PostCard : MonoBehaviour
{
    public void SetPost(CandidateProfileSO candidate, SocialMediaPost post)
    {
        // Get display text (blocks or content)
        contentLabel.text = RedactionController.Instance.GetDisplayText(candidate, post);

        // Show unredact button only if post is redacted
        unredactButton.gameObject.SetActive(
            RedactionController.Instance.IsRedacted(candidate, post)
        );
    }

    public void OnUnredactClicked()
    {
        // Future: check money first
        RedactionController.Instance.TryUnredact(currentCandidate, currentPost);
    }
}
```

---

## Quest Data

For the right panel (criteria) and client header.

```csharp
// Assuming QuestManager provides current quest
Quest currentQuest = QuestManager.Instance.CurrentQuest;

// Client header
currentQuest.client.clientName      // string
currentQuest.client.age             // int
currentQuest.client.gender          // Gender enum
currentQuest.client.profilePicture  // Sprite
currentQuest.introductionText       // string - what client says

// Match criteria
MatchCriteria criteria = currentQuest.matchCriteria;
criteria.acceptableGenders          // Gender[]
criteria.minAge, criteria.maxAge    // int, int
criteria.traitRequirements          // TraitRequirement[]
criteria.maxRedFlags                // int
criteria.minGreenFlags              // int
```

**Displaying trait requirements:**
```csharp
foreach (var req in criteria.traitRequirements)
{
    // Get one random hint to display
    string hint = req.narrativeHints.GetRandomHint();
    RequirementLevel level = req.level;  // Required, Preferred, Avoid

    // Style based on level
    switch (level)
    {
        case RequirementLevel.Required:
            // Bold/red styling
            break;
        case RequirementLevel.Preferred:
            // Normal/yellow styling
            break;
        case RequirementLevel.Avoid:
            // Crossed out/gray styling
            break;
    }
}
```

---

## Decision Flow

When player accepts/rejects a candidate:

```csharp
// Get match result for feedback
MatchResult result = MatchEvaluator.Evaluate(
    MatchListController.Instance.CurrentCandidate,
    currentQuest.matchCriteria
);

if (playerAccepted)
{
    MatchQueueManager.Instance.Accept(MatchListController.Instance.CurrentCandidate);

    if (result.IsMatch)
    {
        // Correct! Show score: result.Score
    }
    else
    {
        // Wrong! Show why: result.FailureReason
    }
}
else // playerRejected
{
    MatchQueueManager.Instance.Reject(MatchListController.Instance.CurrentCandidate);

    if (!result.IsMatch)
    {
        // Correct rejection!
    }
    else
    {
        // Wrong! This was a good match with score: result.Score
    }
}

// Auto-advance to next pending candidate
MatchListController.Instance.SelectNextPending();
```

---

## Session Management

At the start of each quest:

```csharp
// Reset post pools for fresh random posts
PostPoolManager.Instance.ResetPool();
ProfileManager.Instance.ResetAllCandidatePosts();

// Clear current selection
MatchListController.Instance.ClearSelection();

// Populate queue for the quest
MatchQueueManager.Instance.PopulateForQuest(currentQuest, 5);

// Auto-select first candidate
MatchListController.Instance.SelectFirst();
```

---

## Panel Responsibilities

### Left Panel (Candidate Queue)
- Subscribe to `MatchQueueManager.OnQueueChanged` for queue updates
- Subscribe to `MatchQueueManager.OnDecisionMade` for decision state
- Display candidates from `MatchQueueManager.Instance.Queue`
- Show: profile picture, name, age, decision state (pending/accepted/rejected)
- On click: `MatchListController.Instance.SelectCandidate(candidate)`
- Highlight current selection based on `MatchListController.CurrentCandidate`

### Center Panel (Social Feed)
- Subscribe to `MatchListController.OnSelectionChanged`
- Display profile header from `CurrentCandidate.profile`
- Display posts from `CurrentPosts`
- Show Accept/Reject buttons
- On decision: call `MatchQueueManager.Accept/Reject`, evaluate, show feedback

### Right Panel (Quest Criteria)
- Display client info from `Quest.client`
- Display requirements from `Quest.matchCriteria.traitRequirements`
- Show hint text from `narrativeHints.GetRandomHint()`
- Style by `RequirementLevel`

---

## Recommended Panel Widths

```
Left:   20% (candidate queue)
Center: 60% (social feed - main interaction area)
Right:  20% (quest criteria)
```

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

## File Locations

- **MatchQueueManager**: `Assets/Scripts/Managers/MatchQueueManager.cs`
- **MatchListController**: `Assets/Scripts/Controllers/MatchListController.cs`
- **RedactionController**: `Assets/Scripts/Controllers/RedactionController.cs`
- **ProfileManager**: `Assets/Scripts/Managers/ProfileManager.cs`
- **RedactionManager**: `Assets/Scripts/Managers/RedactionManager.cs`
- **MatchEvaluator**: `Assets/Scripts/Matching/MatchEvaluator.cs`
- **Data classes**: `Assets/Scripts/Data/`
