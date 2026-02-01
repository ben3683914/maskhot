# Testing Guide

How to verify each system works correctly using tester scripts and verbose logging.

## Tester Scripts

All tester scripts use Unity's Context Menu (right-click the component in Inspector) to run tests.

### ProfileTester

Verifies profile data and trait references.

**Location**: `Assets/Scripts/Data/ProfileTester.cs`

**Setup**:
1. Attach to a GameObject
2. Drag candidate profiles into the `testCandidates` array

**Context Menu**:
- **Test All Profiles** - Tests all assigned candidates

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

**Context Menu**:
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

**Context Menu**:
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

**Inspector Settings**:
- `requirementMode` - Algorithm mode to test
- `showScoreBreakdown` - Include detailed score breakdown
- `verboseRequirements` - Show all requirement details

**Context Menu**:
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

### SocialFeedTester

Verifies SocialFeedController events and data access.

**Location**: `Assets/Scripts/Testing/SocialFeedTester.cs`

**Setup**:
1. Attach to a GameObject alongside ProfileManager, PostPoolManager, SocialFeedController
2. Optionally assign test candidates

**Context Menu**:
- **Test Set Candidate** - Sets a candidate and logs the event
- **Test Clear Feed** - Clears the feed
- **Log Current State** - Logs current controller state

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

### SocialFeedController

```csharp
// In Inspector, enable verboseLogging
public bool verboseLogging = false;
```

**Outputs**:
- Candidate changes (previous → new)
- Post count for current candidate
- Skip messages when same candidate selected

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
3. Right-click RandomPostTester → "Test All Candidates (via ProfileManager)"
4. Check output for trait matching and engagement

### Testing Matching

1. Add MatchingTester to a GameObject
2. Enable `showScoreBreakdown` and `verboseRequirements`
3. Enter Play mode
4. Right-click → "Test All (from Resources)"
5. Cycle through algorithm modes to compare

### Testing SocialFeedController

1. Add ProfileManager, PostPoolManager, SocialFeedController, SocialFeedTester
2. Enter Play mode
3. Right-click SocialFeedTester → "Test Set Candidate"
4. Verify event fires and data is accessible

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

- **ProfileTester**: `Assets/Scripts/Data/ProfileTester.cs`
- **RandomPostTester**: `Assets/Scripts/Testing/RandomPostTester.cs`
- **ClientTester**: `Assets/Scripts/Testing/ClientTester.cs`
- **MatchingTester**: `Assets/Scripts/Testing/MatchingTester.cs`
- **SocialFeedTester**: `Assets/Scripts/Testing/SocialFeedTester.cs`
