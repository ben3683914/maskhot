# Redaction System

How the post redaction and unredaction mechanics work.

## Overview

Posts in the social media feed can be redacted (blocked out) to create a resource management layer. Players spend money to unredact posts and reveal their content.

**Key behaviors:**
- **Guaranteed posts** (from `CandidateProfileSO.guaranteedPosts`) are NEVER redacted - they provide baseline info
- **Random posts** (from `PostPoolManager`) are redacted by default
- Posts can be unredacted individually, firing events for UI refresh
- Redaction state resets when the queue changes (new quest/session)

## Visual Appearance

Redacted text uses block characters (`█`) while preserving whitespace:

```
Original:  "Just finished an amazing hike at Mt. Rainier!"
Redacted:  "████ ████████ ██ ███████ ████ ██ ███ ████████"
```

This creates an authentic "government document" look where word structure is visible but content is hidden.

---

## Architecture

The redaction system follows the Manager/Controller pattern:

```
┌─────────────────────────────────────────┐
│              UI Layer                    │
│   (subscribes to controller events)      │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│        RedactionController               │
│   - IsRedacted(candidate, post)          │
│   - GetDisplayText(candidate, post)      │
│   - TryUnredact(candidate, post)         │
│   - GetRedactedCount/VisibleCount        │
│   - Events: OnPostUnredacted, OnReset    │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│         RedactionManager                 │
│   - IsUnredacted(post)                   │
│   - MarkUnredacted(post)                 │
│   - ResetAll()                           │
│   - UnredactedCount                      │
│   - Events: OnPostUnredacted, OnReset    │
└─────────────────────────────────────────┘
```

---

## Components

### SocialMediaPost.GetRedactedContent()

Generates the redacted version of post content:

```csharp
// Returns block characters for non-whitespace, preserves spaces/newlines
string redacted = post.GetRedactedContent();
```

### RedactionManager (Data Layer)

Owns the unredaction state - tracks which posts have been revealed.

**Properties:**

| Property | Description |
|----------|-------------|
| `UnredactedCount` | Number of posts marked as unredacted |

**Methods:**

| Method | Description |
|--------|-------------|
| `IsUnredacted(post)` | Checks if a post has been marked as unredacted |
| `MarkUnredacted(post)` | Marks a post as unredacted, fires event |
| `ResetAll()` | Clears all unredaction state, fires event |

**Events:**

| Event | When Fired |
|-------|------------|
| `OnPostUnredacted(post)` | When a post is marked as unredacted |
| `OnRedactionReset` | When all state is cleared |

### RedactionController (UI Layer)

Provides UI-facing interface - determines what should appear redacted and handles user actions.

**Methods:**

| Method | Description |
|--------|-------------|
| `IsRedacted(candidate, post)` | Returns true if post should appear redacted |
| `IsGuaranteedPost(candidate, post)` | Checks if post is guaranteed (never redacted) |
| `GetDisplayText(candidate, post)` | Returns content to display (full or blocks) |
| `TryUnredact(candidate, post)` | Attempts to unredact, validates and delegates |
| `GetRedactedCount(candidate)` | Count of redacted posts for a candidate |
| `GetVisibleCount(candidate)` | Count of visible posts (includes guaranteed) |
| `GetTotalCount(candidate)` | Total posts for a candidate |
| `GetGuaranteedCount(candidate)` | Guaranteed posts for a candidate |

**Events:**

| Event | When Fired |
|-------|------------|
| `OnPostUnredacted(candidate, post)` | When a post is unredacted (enriched with candidate) |
| `OnRedactionReset` | When redaction state is cleared |

---

## UI Developer Workflow

**Important:** Use `RedactionController` for all UI interactions, not `RedactionManager` directly.

### Displaying Posts

When rendering a post in the UI:

```csharp
// Get the text to display
string displayText = RedactionController.Instance.GetDisplayText(candidate, post);

// Check if still redacted (to show "Unredact" button)
bool isRedacted = RedactionController.Instance.IsRedacted(candidate, post);
```

### Unredacting a Post

When the player pays to unredact:

```csharp
bool success = RedactionController.Instance.TryUnredact(candidate, post);
// If success, OnPostUnredacted event fires
```

### Listening for Changes

Subscribe to controller events for UI refresh:

```csharp
void OnEnable()
{
    RedactionController.Instance.OnPostUnredacted += HandlePostUnredacted;
    RedactionController.Instance.OnRedactionReset += HandleRedactionReset;
}

void HandlePostUnredacted(CandidateProfileSO candidate, SocialMediaPost post)
{
    // Refresh the specific post's display
    RefreshPostUI(candidate, post);
}

void HandleRedactionReset()
{
    // Refresh all post displays (new session started)
    RefreshAllPostsUI();
}
```

### Getting Counts for UI

Show the player how many posts are still redacted:

```csharp
int redacted = RedactionController.Instance.GetRedactedCount(candidate);
int visible = RedactionController.Instance.GetVisibleCount(candidate);
int total = RedactionController.Instance.GetTotalCount(candidate);

// Display: "Posts: 3/8 visible"
```

---

## Integration Points

### Automatic Reset on Queue Change

RedactionManager subscribes to `MatchQueueManager.OnQueueChanged`. When the queue is populated or cleared (new quest), all unredaction state is automatically reset.

---

## Future: Money System Integration

When the money system is implemented:

```csharp
public void OnUnredactButtonClicked(CandidateProfileSO candidate, SocialMediaPost post)
{
    int cost = GetUnredactCost(); // e.g., $50

    if (MoneyManager.Instance.CanAfford(cost))
    {
        MoneyManager.Instance.Spend(cost);
        RedactionController.Instance.TryUnredact(candidate, post);
    }
    else
    {
        ShowInsufficientFundsMessage();
    }
}
```

---

## Testing

### RedactionManagerTester

Tests the data layer (RedactionManager):

1. Attach to a GameObject alongside ProfileManager, MatchQueueManager, and RedactionManager
2. Enter Play Mode
3. Run tests via Inspector buttons

**Available Tests:**
- **Test IsUnredacted** - Verifies posts start as not unredacted
- **Test MarkUnredacted** - Verifies marking a post and event firing
- **Test MarkUnredacted Twice** - Verifies idempotency
- **Test ResetAll** - Verifies reset clears all state

### RedactionControllerTester

Tests the UI layer (RedactionController):

1. Attach to a GameObject alongside ProfileManager, PostPoolManager, MatchQueueManager, RedactionManager, and RedactionController
2. Enter Play Mode
3. Click "Populate Test Queue" to get candidates
4. Run tests via Inspector buttons

**Available Tests:**
- **Test Guaranteed Posts** - Verifies guaranteed posts are never redacted
- **Test Random Posts Redacted** - Verifies random posts start redacted
- **Test GetDisplayText** - Verifies correct content is returned
- **Test TryUnredact** - Verifies unredaction works and fires event
- **Test Counts** - Verifies count methods work correctly

See [testing.md](testing.md) for general testing conventions.
