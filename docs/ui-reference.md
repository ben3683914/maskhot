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

## SocialFeedController

**The main controller for the center panel.** Singleton that manages the currently selected candidate.

### Properties

```csharp
// Singleton access
SocialFeedController.Instance

// Current candidate (null if none selected)
CandidateProfileSO CurrentCandidate { get; }

// Quick check if a candidate is selected
bool HasCandidate { get; }

// Get all posts for current candidate (guaranteed + random, sorted by date)
List<SocialMediaPost> CurrentPosts { get; }
```

### Methods

```csharp
// Set the current candidate (fires OnCandidateChanged event)
void SetCandidate(CandidateProfileSO candidate)

// Clear the feed (equivalent to SetCandidate(null))
void ClearFeed()
```

### Events

```csharp
// Fired when the current candidate changes
// Passes the new candidate (null if cleared)
event Action<CandidateProfileSO> OnCandidateChanged;
```

### Usage Examples

**Option 1: Subscribe to events (recommended for UI Toolkit)**

```csharp
public class SocialFeedPanel : MonoBehaviour
{
    private void OnEnable()
    {
        SocialFeedController.Instance.OnCandidateChanged += HandleCandidateChanged;
    }

    private void OnDisable()
    {
        SocialFeedController.Instance.OnCandidateChanged -= HandleCandidateChanged;
    }

    private void HandleCandidateChanged(CandidateProfileSO candidate)
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
        foreach (var post in SocialFeedController.Instance.CurrentPosts)
        {
            CreatePostElement(post);
        }
    }
}
```

**Option 2: Direct access (for one-off reads)**

```csharp
// Check if there's a candidate
if (SocialFeedController.Instance.HasCandidate)
{
    var candidate = SocialFeedController.Instance.CurrentCandidate;
    var posts = SocialFeedController.Instance.CurrentPosts;
    // ...
}
```

**Setting the candidate (from left panel)**

```csharp
// When player clicks a candidate in the queue
public void OnCandidateClicked(CandidateProfileSO candidate)
{
    SocialFeedController.Instance.SetCandidate(candidate);
}
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
    SocialFeedController.Instance.CurrentCandidate,
    currentQuest.matchCriteria
);

if (playerAccepted)
{
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
    if (!result.IsMatch)
    {
        // Correct rejection!
    }
    else
    {
        // Wrong! This was a good match with score: result.Score
    }
}
```

---

## Session Management

At the start of each quest:

```csharp
// Reset post pools for fresh random posts
PostPoolManager.Instance.ResetPool();
ProfileManager.Instance.ResetAllCandidatePosts();

// Clear current selection
SocialFeedController.Instance.ClearFeed();
```

---

## Panel Responsibilities

### Left Panel (Candidate Queue)
- Display list of candidates from `ProfileManager.Instance.GetAllCandidates()`
- Show: profile picture, name, age, truncated bio
- On click: `SocialFeedController.Instance.SetCandidate(candidate)`
- Track reviewed/pending state locally

### Center Panel (Social Feed)
- Subscribe to `SocialFeedController.OnCandidateChanged`
- Display profile header from `CurrentCandidate.profile`
- Display posts from `CurrentPosts`
- Show Accept/Reject buttons
- On decision: evaluate and provide feedback

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

---

## File Locations

- **SocialFeedController**: `Assets/Scripts/Controllers/SocialFeedController.cs`
- **ProfileManager**: `Assets/Scripts/Managers/ProfileManager.cs`
- **MatchEvaluator**: `Assets/Scripts/Matching/MatchEvaluator.cs`
- **Data classes**: `Assets/Scripts/Data/`
