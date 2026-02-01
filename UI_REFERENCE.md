# Maskhot - UI Developer Reference

**Game Concept**: A Papers Please-inspired social media matchmaking game where you review candidate profiles to find matches for your client (friend).

## Quick Overview

**Three-Panel Layout** (single screen):
- **Left Panel**: Queue of candidates to review
- **Center Panel**: Social media feed of selected candidate
- **Right Panel**: Quest criteria (what the client wants)

**Core Gameplay Loop**:
1. Client gives you requirements (abstract hints like "enjoys fine cuisine")
2. Review candidates' social media posts
3. Accept or reject each candidate based on how well they match
4. Get scored on your matchmaking accuracy

---

## Screen Layout

### Main Game Screen

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
│           │                         │                      │
│           │  [Accept] [Reject]      │                      │
└───────────┴─────────────────────────┴──────────────────────┘
```

---

## 1. Client Info Header (Top Bar)

**Purpose**: Shows who you're finding a match for

**Data to Display**:
- Client name (string)
- Client age (int)
- Client gender (enum: Male, Female, NonBinary)
- Client photo/avatar (Sprite)
- Introduction text (string) - what they're asking for
  - Example: "Hey! I'm looking for someone active and outdoorsy to go on adventures with!"

**Data Source**: `Quest.client` (ClientProfile)

**Visual Style**: Friendly, personal - like a text message from your friend

---

## 2. Left Panel - Candidate Queue

**Purpose**: Shows the lineup of candidates waiting to be reviewed

**Data to Display** (per candidate card):
- Profile photo (Sprite)
- Name (string)
- Age (int)
- 1-line preview of bio (truncated string)

**Interactions**:
- Click a candidate card → loads their profile in the center panel
- Visual indicator for:
  - Currently selected candidate (highlight/border)
  - Candidates already reviewed (greyed out or checkmark)

**Data Source**: Provided by `MatchQueueManager` (list of CandidateProfile)

**Layout**: Vertical scrollable list

---

## 3. Center Panel - Social Media Feed

**Purpose**: Main interaction area - displays candidate's social media posts

### 3A. Profile Header

**Data to Display**:
- Profile photo (Sprite)
- Full name (string)
- Age (int)
- Gender (enum: Male, Female, NonBinary)
- Bio (multi-line string)
- Archetype (enum: Adventurous, Creative, Intellectual, etc.) - optional display

**Data Source**: `CandidateProfile.profile`

### 3B. Social Media Posts

**Each post displays**:
- Post type icon (Photo, TextOnly, Story, Video, Poll, SharedPost)
- Post content:
  - Text content (string)
  - Image (Sprite) - if photo/video post
- Timestamp (string) - "2 days ago", "1 week ago"
- Engagement metrics:
  - Likes (int) with heart icon
  - Comments (int) with comment icon
- Visual indicators:
  - Green flag indicator (if `isGreenFlag == true`)
  - Red flag indicator (if `isRedFlag == true`)

**Data Source**: `CandidateProfile.guaranteedPosts` (List of SocialMediaPost)

**Layout**: Vertical scrollable feed (like Instagram/Twitter)

**Important Notes**:
- Posts should feel like a real social media app
- Red/green flags should be subtle but noticeable
- Players will spend most time here analyzing posts

### 3C. Decision Buttons

**Two buttons at bottom**:
- **ACCEPT** button (green) - "This is a good match!"
- **REJECT** button (red) - "Not a good match"

**Interactions**:
- Click → submits decision
- Locks the candidate (can't change decision)
- Moves to next candidate or shows results

**Data Flow**: Sends decision to `DecisionController` → gets scored → updates game state

---

## 4. Right Panel - Quest Criteria

**Purpose**: Shows what the client is looking for (helps player make decisions)

### 4A. Basic Requirements

**Data to Display**:
- Acceptable genders (array of Gender enum)
  - Display: "Looking for: Male / Female / Any"
- Age range (int min, int max)
  - Display: "Age: 25-35"

**Data Source**: `MatchCriteria.acceptableGenders`, `minAge`, `maxAge`

### 4B. Trait Requirements (Narrative Hints)

**What the player sees**: Abstract, natural language hints

**Data to Display** (per requirement):
- Narrative hint text (string) - randomly selected from collection
  - Examples: "enjoys fine cuisine", "morning person", "loves adventure"
- Importance level (optional visual indicator):
  - Required (must have - red/bold)
  - Preferred (nice to have - yellow)
  - Avoid (should not have - crossed out)

**Data Source**:
- `MatchCriteria.traitRequirements[]`
- Each has `narrativeHints.GetRandomHint()` (one random hint per requirement)
- `level` enum (Required, Preferred, Avoid)

**Important**:
- These are intentionally vague/abstract (not "must like cooking", but "enjoys fine cuisine")
- Player must interpret hints by reading social media posts
- This is the core puzzle mechanic!

### 4C. Dealbreakers (Optional Display)

**Data to Display**:
- List of absolute no-gos
  - Display: "Deal Breakers: [trait names]" or use ❌ icons

**Data Source**: `MatchCriteria.dealbreakerPersonalityTraits`, `dealbreakerInterests`, `dealbreakerLifestyleTraits`

### 4D. Red Flag Tolerance (Optional Display)

**Data to Display**:
- Max red flags allowed (int)
- Min green flags required (int)

**Visual Suggestion**: Simple icon counters or progress bars

**Data Source**: `MatchCriteria.maxRedFlags`, `minGreenFlags`

---

## UI Flow & States

### Game Start
1. Quest loads → Client info header populates
2. Right panel shows criteria
3. Left panel loads candidate queue
4. First candidate auto-selected → Center panel shows their feed

### During Gameplay
1. Player clicks candidate in left panel
2. Center panel transitions to show new candidate's profile/feed
3. Player scrolls through posts, analyzes against criteria
4. Player clicks Accept or Reject
5. Decision locks, candidate marked as reviewed
6. Repeat until all candidates reviewed

### End of Round
1. Show results screen (score, feedback)
2. Unlock new quest or end session

---

## Data Flow (Backend → UI)

### On Quest Start
```
QuestManager provides:
  └─ Quest
      ├─ client (ClientProfile) → Client Header
      └─ matchCriteria (MatchCriteria) → Right Panel

MatchQueueManager provides:
  └─ List<CandidateProfile> → Left Panel

ProfileManager provides:
  └─ Selected CandidateProfile → Center Panel
```

### Player Decision
```
UI (Accept/Reject button clicked)
  ↓
DecisionController.SubmitDecision(candidate, accepted)
  ↓
Scoring/validation happens
  ↓
UI updates (lock candidate, show next)
```

---

## Visual Design Notes

### Theme
- Modern social media aesthetic
- Clean, minimal UI
- Focus on readability (players need to analyze text)

### Color Coding
- **Green**: Positive/Accept/Green flags
- **Red**: Negative/Reject/Red flags/Dealbreakers
- **Yellow/Orange**: Warnings/Preferred traits
- **Neutral grays**: Unreviewed candidates

### Fonts
- Social media posts: Sans-serif, modern (like Roboto, Inter)
- UI elements: Clean, readable
- Criteria hints: Slightly stylized (handwritten feel?) to show they're from the client

### Icons Needed
- Post types: Photo, Video, Text, Story, Poll, Share
- Engagement: Heart (likes), Comment bubble
- Flags: Green flag 🟢, Red flag 🔴
- Gender: Male ♂, Female ♀, Non-binary ⚧
- Accept/Reject: Checkmark, X

---

## Responsive Considerations

**Fixed Layout** (single screen):
- Three panels should maintain proportions
- Center panel is largest (60% width)
- Left and right panels ~20% each
- Panels should not overlap

**Scrolling**:
- Left panel: Vertical scroll (candidate list)
- Center panel: Vertical scroll (social feed)
- Right panel: May scroll if many criteria

---

## Future Features (Stretch Goals)

These are "if we have time" during the 48-hour jam:

### Unlockable Information
- Some candidate info starts redacted/blurred
- Spend currency to unlock
- Visual: Blur effect or pixelation that clears when unlocked

### Animations
- Smooth transitions between candidates
- Post loading animation (like Instagram)
- Success/failure feedback on decision
- Card flip/swipe animations

### Sound Effects
- Click sounds
- Success/failure tones
- Notification sounds for new posts

---

## Technical Notes for UI Developer

### Data Classes You'll Work With

1. **Quest** - Overall quest data
   - `client` (ClientProfile)
   - `introductionText` (string)
   - `matchCriteria` (MatchCriteria)

2. **ClientProfile** - Client info
   - `clientName`, `age`, `gender`, `profilePicture`
   - `relationship`, `backstory`

3. **CandidateProfileSO** - Candidate data (ScriptableObject wrapper)
   - `profile.characterName`, `profile.age`, `profile.gender`, `profile.bio`
   - `profile.personalityTraits[]`, `profile.interests[]`, `profile.lifestyleTraits[]`
   - `GetProfilePicture()` → Returns profile Sprite (with gender-based fallback)
   - `GetPostsForPlaythrough()` → Returns posts (cached per session)

4. **SocialMediaPost** - Individual post
   - `postType`, `content`, `imageContent`, `timestamp`
   - `likes`, `comments`
   - `isGreenFlag`, `isRedFlag`
   - `relatedInterests[]`, `relatedPersonalityTraits[]`, `relatedLifestyleTraits[]`

5. **MatchCriteria** - What client wants
   - `acceptableGenders[]`, `minAge`, `maxAge`
   - `traitRequirements[]` (each has `narrativeHints` and `level`)
   - `dealbreakerPersonalityTraits[]`, etc.
   - `maxRedFlags`, `minGreenFlags`

### Controller Hooks

You'll interact with these managers/controllers (will be implemented):

- **QuestManager**: `GetCurrentQuest()` → Quest data
- **MatchQueueManager**: `GetCandidateQueue()` → List of candidates
- **ProfileManager**: `GetCandidateByName(name)` → Candidate details
  - `ResetAllCandidatePosts()` → Call when starting new quest to clear cached posts
- **CandidateProfileSO** (directly):
  - `GetProfilePicture()` → Profile Sprite (uses gender-based default if none assigned)
  - `GetPostsForPlaythrough()` → Social media posts (cached per session)
- **DecisionController**: `SubmitDecision(candidate, accepted)` → Handle accept/reject

### Testing Without Backend

Use **SAMPLE_DATA.md** to create test UI:
- 3 sample candidates (Sarah, Alex, Jamie)
- 5 interests, 5 personality traits, 5 lifestyle traits
- Each candidate has 3 guaranteed posts

You can hard-code this data into UI prefabs for initial testing.

---

## Questions for Backend Team?

- How will candidates be provided to UI? (event system, direct calls, or property binding?)
- Animation timing for transitions?
- Audio integration approach?
- How to handle "unlockable info" system if implemented?

---

## Priority Order for Implementation

**Phase 1 - Core UI (MVP)**:
1. Three-panel layout
2. Center panel: Profile header + social feed
3. Decision buttons (Accept/Reject)
4. Right panel: Basic criteria display

**Phase 2 - Full Features**:
5. Left panel: Candidate queue
6. Client header
7. Visual indicators (flags, reviewed status)

**Phase 3 - Polish**:
8. Animations/transitions
9. Sound effects
10. Unlockable info system (if time)

---

Good luck! Reference PROJECT_SETUP.md and SAMPLE_DATA.md for additional backend details and test data.
