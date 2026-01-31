# Maskhot - Social Media Matchmaking Game

## Game Concept
A 2D UI-based game inspired by Papers Please, where you review social media profiles to find matches for your client (friend). Instead of checking papers, you examine social media feeds, posts, and profile information of candidates (potential matches) to determine if someone is a good match based on given criteria.

**Terminology:**
- **Candidate** - A potential match being reviewed (gender-neutral)
- **Client** - The person you're finding a match for (your friend)

## UI Layout
The game operates on a **single screen with three panels**:

- **Left Panel**: Queue of potential matches that need to be reviewed
- **Center Panel**: Social media feed of the currently selected potential match (main interaction area)
- **Right Panel**: Quest info/match criteria - the requirements you need to match against

## Project Structure

### Scripts Organization (Type-Based)
```
Scripts/
├── Managers/          - Game-wide systems (GameManager, ProfileManager, QuestManager, MatchQueueManager)
├── Controllers/       - Feature controllers (MatchListController, SocialFeedController, DecisionController)
├── Data/             - Data classes, structs, enums (ProfileData, SocialMediaPost, MatchCriteria)
├── UI/               - UI-specific scripts for each screen/panel
└── Utilities/        - Helper classes and extensions
```

### Prefabs Organization (Screen/Panel-Based)
```
Prefabs/UI/
├── MainMenu/                       - Initial menu screen
├── Panels/
│   ├── MatchListPanel/            - Left panel: Queue of potential matches
│   ├── SocialFeedPanel/           - Center panel: Social media feed
│   │   └── PostTypes/             - Different post prefabs (photo, text, story, etc.)
│   └── QuestCriteriaPanel/        - Right panel: Match criteria display
└── Common/
    ├── Buttons/                    - Reusable button components
    ├── Cards/                      - Profile cards, info cards
    └── Popups/                     - Dialogs, tooltips, confirmations
```

### Sprites Organization
```
Sprites/
├── UI/
│   ├── LeftPanel/                 - UI elements for match list
│   ├── CenterPanel/               - UI elements for social feed
│   └── RightPanel/                - UI elements for criteria panel
├── Profiles/                      - Character portraits, profile pictures
├── Backgrounds/                   - Screen backgrounds, desk, environment
├── Icons/                         - Social media icons, status indicators
└── SocialMedia/                   - Post images, story content, photos
```

### Data Organization (Resources Folder)
ScriptableObjects are stored in the Resources folder for automatic runtime loading by ProfileManager:
```
Resources/GameData/
├── Traits/
│   ├── Interests/             - Interest ScriptableObjects (Hiking, Gaming, etc.)
│   ├── Personality/           - Personality trait SOs (Outgoing, Creative, etc.)
│   └── Lifestyle/             - Lifestyle trait SOs (Night Owl, Homebody, etc.)
├── NarrativeHints/            - Narrative hint collections (Food_Hints, Travel_Hints, etc.)
├── Profiles/                  - Candidate profile ScriptableObjects
├── PostPool/                  - RandomPostPool.asset (global random post pool)
└── Clients/                   - Client profile ScriptableObjects (future)
```

### Other Asset Folders
```
Audio/
├── Music/
└── SFX/
Fonts/
Materials/
```

## Backend Systems to Implement

### Core Data Structures (Priority 1)
Define the fundamental data types:

- **SocialMediaPost** (IMPLEMENTED) - Individual post data
  - Type (photo, text, story, video, poll, shared post)
  - Content (text, image Sprite reference)
  - Days since posted (int - for sorting, 1 = yesterday, 7 = week ago, etc.)
  - Likes, comments, engagement data
  - Trait associations (ScriptableObject references to related interests, personality traits, lifestyle traits)
  - Red flag / green flag boolean indicators
  - Note: Categories removed - use "Controversial" personality trait for divisive posts instead

- **CandidateProfile** (IMPLEMENTED) - Character profile information
  - Name, gender, age, bio
  - Profile photo reference
  - Personality traits (ScriptableObject references)
  - Interests/hobbies (ScriptableObject references)
  - Lifestyle traits (ScriptableObject references)
  - Personality archetype enum

- **Gender enum** (IMPLEMENTED) - Male, Female, NonBinary

- **InterestSO** (IMPLEMENTED) - ScriptableObject for interests/hobbies
  - Display name, icon, description
  - Category (Outdoor, Indoor, Creative, Athletic, etc.)
  - Match weight (1-10)
  - Related interests

- **PersonalityTraitSO** (IMPLEMENTED) - ScriptableObject for personality traits
  - Display name, icon, description
  - Positive/negative flag (`isPositiveTrait`)
  - Opposite and complementary traits (array-based)
  - Match weight (1-10)
  - **Special: "Controversial" trait** - Used for divisive/polarizing posts (replaces old category system)

- **LifestyleTraitSO** (IMPLEMENTED) - ScriptableObject for lifestyle indicators
  - Display name, icon, description
  - Category (Activity Level, Social Preference, Schedule, etc.)
  - Conflicting and compatible traits
  - Match weight (1-10)

- **NarrativeHintCollectionSO** (IMPLEMENTED) - ScriptableObject for narrative hint collections
  - Direct ScriptableObject references to related traits (type-safe, no string typos)
  - Can reference Interests, Personality Traits, or Lifestyle Traits
  - Array of hint variations (one randomly selected at runtime)
  - GetRandomHint() helper method
  - IMPORTANT: Each collection maps to ONE trait or COMPATIBLE/SIMILAR traits only
  - DO NOT mix opposite/contradictory hints (e.g., "morning person" and "night owl" in same collection)
  - Example: EarlyRiser_Hints → references Early Riser lifestyle + hints ["morning person", "early bird", "loves sunrise"]
  - Example: Cooking_Hints → references Cooking interest + hints ["enjoys fine cuisine", "loves gourmet food", "foodie"]
  - NOTE: You CAN use multiple trait types (interest + personality + lifestyle) in one collection if hints genuinely apply to ALL referenced traits
    - Example: CreativeCooking_Hints could reference both Cooking (interest) + Creative (personality) with hints like "experimental chef", "culinary artist"
    - The related trait fields are organizational metadata, not hard requirements used by matching logic
    - Most collections will use 1 trait type for clarity, but mixing is acceptable when hints truly relate to all traits

- **ClientProfile** (IMPLEMENTED) - The client's profile (person asking for a match)
  - Name, gender, age, photo/avatar
  - Relationship to player and backstory
  - Client's own traits (personality, interests, lifestyle)
  - Personality archetype enum
  - Can be used in ScriptableObjects (curated) or created at runtime (procedural)

- **MatchCriteria** (IMPLEMENTED) - Requirements for matching a candidate to a client
  - Gender preferences and age range (min/max)
  - Trait requirements with narrative hints (abstract player-facing text)
  - Backend trait mappings (concrete ScriptableObject references)
  - RequirementLevel enum (Required, Preferred, Avoid)
  - Dealbreaker traits (auto-reject)
  - Red flag tolerance (max allowed, min green flags required)
  - Scoring weights (personality, interests, lifestyle)
  - Can be used in ScriptableObjects (curated) or created at runtime (procedural)

- **Quest** (IMPLEMENTED) - Runtime quest data for a matching session
  - ClientProfile (who is asking for a match)
  - Introduction text (what the client says)
  - MatchCriteria (what they want, presented as narrative hints)
  - Procedurally generated by QuestGenerator (future)
  - Can use curated ClientProfileSO or procedural ClientProfile
  - Helper methods: FromClientProfileSO(), CreateProcedural()

- **RandomPostPoolSO** (IMPLEMENTED) - ScriptableObject holding the global random post pool
  - List of SocialMediaPost instances
  - Posts are selected based on trait matching with candidates
  - Pool is tracked per session (no duplicates within a quest)

### ScriptableObject Templates (Priority 2)
Create SO classes to hold game data:

- **NarrativeHintCollectionSO** (IMPLEMENTED) - Collections of narrative hints for quest requirements
  - Each collection maps to ONE trait or group of COMPATIBLE traits (not opposites)
  - Create one collection per trait or similar trait group
  - Examples: EarlyRiser_Hints, NightOwl_Hints, Cooking_Hints, Homebody_Hints
  - One hint randomly selected at runtime to show player
- **CandidateProfileSO** (IMPLEMENTED) - Individual candidate profiles with social media content
  - Static profile data (consistent across playthroughs)
  - Guaranteed posts (always appear)
  - Random post settings: `randomPostMin/Max` (range for post count per session)
  - Social metrics: `friendsCountMin/Max` (affects engagement generation)
- **ClientProfileSO** (IMPLEMENTED) - Curated client profiles (hand-crafted story characters)
  - Wraps ClientProfile data
  - Story client flag and suggested level metadata
  - Used by QuestGenerator to create story-appropriate quests

### Manager Classes (Priority 3)
Game-wide state management:

- **GameManager** - Overall game state, level progression, game loop
- **ProfileManager** (IMPLEMENTED) - Singleton that auto-loads all profiles and traits from Resources folder
  - Auto-loads on Awake via `Resources.LoadAll<T>()`
  - Provides lookup by name: `GetCandidateByName()`, `GetInterestByName()`, etc.
  - Category filters: `GetInterestsByCategory()`, `GetLifestyleTraitsByCategory()`
  - Verbose logging toggle for debugging
- **PostPoolManager** (IMPLEMENTED) - Singleton that handles random post selection
  - Loads RandomPostPool from Resources folder
  - Selects posts based on trait matching with 10% wild card chance
  - Generates engagement (likes/comments) based on candidate's friends count
  - Generates `daysSincePosted` to interleave with guaranteed posts
  - Tracks used posts per session (call `ResetPool()` for new quest)
  - Configurable: `wildCardChance`, `photoWeight`, `baseEngagementMultiplier`
  - Verbose logging toggle for debugging
- **QuestManager** - Manages current quest/criteria, validates matches
- **MatchQueueManager** - Manages the queue of potential matches (left panel data source)

### Controller Classes (Priority 4)
Feature-specific logic:

- **MatchListController** - Handles profile selection from the queue
- **SocialFeedController** - Generates/displays social media posts for selected profile
- **DecisionController** - Handles accept/reject match decisions and scoring

## Testing & Sample Data

### JSON Import System
All game data is managed through JSON files in the **JSONData/** folder:
- **25 Interests** - Hobbies and activities (Hiking, Gaming, Yoga, Photography, etc.)
- **15 Personality Traits** - Character traits including special "Controversial" trait for red flags
- **10 Lifestyle Traits** - Daily life patterns (Night Owl, Career Driven, Eco Friendly, etc.)
- **10 Candidate Profiles** - Complete dating profiles with 1-5 posts each (29 total posts)
- **1000 Random Posts** - Global pool of posts for trait-based random assignment

### Automated Import Workflow
1. **Edit JSON files** in `JSONData/` folder to add/modify data
2. **In Unity**, go to: `Tools > Maskhot > Import Data from JSON`
3. **Confirm import** - All ScriptableObjects are created in `Assets/Resources/GameData/`
4. **Test with ProfileManager** - Add ProfileManager to a GameObject, enable `verboseLogging`, enter Play mode
5. **Or use ProfileTester** - Attach ProfileTester component to GameObject, drag in profiles, run tests

See **JSONData/README.md** for complete documentation on JSON structure and usage.
See **SAMPLE_DATA.md** for original reference data (now available as JSON files).

### Testing Components
- **ProfileManager.cs** - Runtime data manager with verbose logging
  - Enable `verboseLogging` toggle in Inspector to see full data dump
  - Outputs all loaded profiles, traits, and their relationships
  - Usage: Add to GameObject, enable verbose logging, enter Play mode
- **ProfileTester.cs** - Test script to verify profile data
  - Tests multiple profiles in a single run
  - Consolidated output per profile for easy debugging
  - Validates all trait references and post data
  - Usage: Attach to GameObject, assign profile arrays, use Context Menu "Test All Profiles"

## Development Workflow

### JSON Data Management
**All game data is managed through JSON files** - no manual ScriptableObject creation needed!

1. **Edit JSON files** in `JSONData/` folder:
   - `Interests.json` - 25 hobbies/activities
   - `PersonalityTraits.json` - 15 character traits
   - `LifestyleTraits.json` - 10 daily life patterns
   - `Candidates.json` - 10 complete profiles with posts
   - `RandomPosts.json` - 1000 global random posts for trait-based selection

2. **Import to Unity**: `Tools > Maskhot > Import Data from JSON`
   - Automatically creates all ScriptableObjects
   - Resolves all trait references
   - Handles enum conversions
   - Creates proper folder structure

3. **Test**: Use ProfileManager (with verbose logging) or ProfileTester to verify imported data

**Benefits:**
- Version control friendly (JSON diff)
- Easy to edit and collaborate
- Fast iteration (reimport overwrites)
- No manual inspector work
- Bulk data entry support

See `JSONData/README.md` for complete JSON documentation.

### Recommended Build Order:
1. **Data Structures** ✓ COMPLETE - Core classes implemented
2. **ScriptableObjects** ✓ COMPLETE - JSON import system functional
3. **Manager Layer** ⏳ IN PROGRESS - ProfileManager done, GameManager/QuestManager/MatchQueueManager remaining
4. **Controllers** - Wire up logic for game flow
5. **UI Integration** - Hand off to UI developer with working backend

### Technology Stack
- Unity 2D
- Unity's New Input System (InputSystem_Actions.inputactions already configured)
- ScriptableObjects for data management
- Prefabs (used extensively)
- Resources folder for runtime loading

## Notes for UI Developer
- Backend systems will provide data through Manager classes
- Each panel should have a corresponding Controller to handle logic
- Common UI components should be highly reusable
- Focus on modular prefabs that can be easily tested independently

## Implementation Status

### ✓ Completed
- Project folder structure (Scripts, Prefabs, Sprites, Data, Audio, etc.)
- Core data classes:
  - SocialMediaPost.cs (with int-based timestamp for sorting)
  - CandidateProfile.cs
  - ClientProfile.cs
  - MatchCriteria.cs (with TraitRequirement and RequirementLevel)
  - Gender enum
  - PersonalityArchetype enum
- Trait ScriptableObjects:
  - InterestSO.cs
  - PersonalityTraitSO.cs (with array-based opposite traits)
  - LifestyleTraitSO.cs
  - NarrativeHintCollectionSO.cs (hint collections for quest requirements)
- Profile ScriptableObjects:
  - CandidateProfileSO.cs (with guaranteed posts and randomization rules)
  - ClientProfileSO.cs (curated client profiles for story progression)
- Quest system:
  - Quest.cs (runtime quest data: client + introduction + match criteria)
- **JSON Import System**:
  - ScriptableObjectImporter.cs (Unity Editor tool with menu integration)
  - Automated asset creation from JSON files
  - Reference resolution between traits and profiles
  - Enum parsing for categories, gender, archetype, post types
  - JSONData/ folder with complete test dataset
  - JSONData/README.md documentation
- **Test Data**:
  - 25 interests across all categories
  - 15 personality traits (including "Controversial" for red flags)
  - 10 lifestyle traits
  - 10 diverse candidate profiles (varying post counts: 1-5)
  - 29 total social media posts with green/red flags
  - SAMPLE_DATA.md reference documentation
- **Testing Tools**:
  - ProfileTester.cs (multi-profile batch testing with consolidated output)
- Hybrid design supporting both curated (ScriptableObject) and procedural (runtime) content
- **Manager Layer**:
  - ProfileManager.cs (singleton, auto-loads from Resources, verbose logging toggle)
    - Auto-loads all profiles/traits via `Resources.LoadAll<T>()`
    - Dictionary lookups by name for fast access
    - Category filtering for interests and lifestyle traits
    - Narrative hint lookup by trait reference
  - PostPoolManager.cs (singleton, random post selection with trait matching)
    - Loads RandomPostPool from Resources
    - Weighted trait-based selection with 10% wild card chance
    - Engagement generation based on friends count
    - Session-based uniqueness tracking
    - Verbose logging toggle
- **Random Post System**:
  - RandomPostPoolSO.cs (data container for global post pool)
  - RandomPosts.json (1000 posts with trait associations - 65% text, 35% photo)
  - CandidateProfileSO updated with `randomPostMin/Max` and `friendsCountMin/Max` ranges
  - ScriptableObjectImporter updated to import random posts

### ⏳ In Progress
- None - ready for next development phase

### 📋 To Do
- Sample client data (create a few ClientProfileSO assets for story progression)
- Remaining manager classes (GameManager, QuestManager, MatchQueueManager)
- Controller classes (MatchListController, SocialFeedController, DecisionController)
- Matching/scoring algorithm
- Procedural generation systems (QuestGenerator - creates Quest instances from ClientProfileSO or procedural data)
- UI implementation
