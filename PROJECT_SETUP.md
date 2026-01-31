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

### Data Organization
```
Data/
└── ScriptableObjects/
    ├── Traits/
    │   ├── Interests/             - Interest ScriptableObjects (Hiking, Gaming, etc.)
    │   ├── PersonalityTraits/     - Personality trait SOs (Outgoing, Creative, etc.)
    │   └── LifestyleTraits/       - Lifestyle trait SOs (Night Owl, Homebody, etc.)
    ├── NarrativeHints/            - Narrative hint collections (Food_Hints, Travel_Hints, etc.)
    ├── Profiles/                  - Candidate profile ScriptableObjects
    └── Clients/                   - Client profile ScriptableObjects
```

### Other Asset Folders
```
Resources/                         - Runtime-loaded assets
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
  - Content (text, image reference)
  - Timestamp/date
  - Likes, comments, engagement data
  - Trait associations (ScriptableObject references to related interests, personality traits, lifestyle traits)
  - Red flag / green flag indicators
  - Categories for organization

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
  - Positive/negative flag
  - Opposite and complementary traits
  - Match weight (1-10)

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
  - Randomization rules (post count, allowed categories)
- **ClientProfileSO** (IMPLEMENTED) - Curated client profiles (hand-crafted story characters)
  - Wraps ClientProfile data
  - Story client flag and suggested level metadata
  - Used by QuestGenerator to create story-appropriate quests

### Manager Classes (Priority 3)
Game-wide state management:

- **GameManager** - Overall game state, level progression, game loop
- **ProfileManager** - Loads and provides profile data to other systems
- **QuestManager** - Manages current quest/criteria, validates matches
- **MatchQueueManager** - Manages the queue of potential matches (left panel data source)

### Controller Classes (Priority 4)
Feature-specific logic:

- **MatchListController** - Handles profile selection from the queue
- **SocialFeedController** - Generates/displays social media posts for selected profile
- **DecisionController** - Handles accept/reject match decisions and scoring

## Testing & Sample Data

See **SAMPLE_DATA.md** for ready-to-use test data including:
- 5 sample interests (Hiking, Gaming, Cooking, Reading, Fitness)
- 5 sample personality traits (Outgoing, Introverted, Adventurous, Creative, Reliable)
- 5 sample lifestyle traits (Night Owl, Early Riser, Homebody, Social Butterfly, Fitness Focused)
- 3 complete candidate profiles with social media posts (Sarah, Alex, Jamie)

To test in Unity:
1. Create trait ScriptableObjects first (in Data/ScriptableObjects/Traits/)
2. Create candidate profiles (in Data/ScriptableObjects/Profiles/)
3. Reference the sample data file for values to input
4. Enter Play mode to verify data displays correctly

## Development Workflow

### Recommended Build Order:
1. **Data Structures** ✓ COMPLETE - Core classes implemented
2. **ScriptableObjects** ✓ COMPLETE - Templates created, ready for data entry
3. **Manager Layer** - Build systems to load and manage data
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
  - SocialMediaPost.cs
  - CandidateProfile.cs
  - ClientProfile.cs
  - MatchCriteria.cs (with TraitRequirement and RequirementLevel)
  - Gender enum
  - PersonalityArchetype enum
- Trait ScriptableObjects:
  - InterestSO.cs
  - PersonalityTraitSO.cs
  - LifestyleTraitSO.cs
  - NarrativeHintCollectionSO.cs (hint collections for quest requirements)
- Profile ScriptableObjects:
  - CandidateProfileSO.cs (with guaranteed posts and randomization rules)
  - ClientProfileSO.cs (curated client profiles for story progression)
- Quest system:
  - Quest.cs (runtime quest data: client + introduction + match criteria)
- Sample data file (SAMPLE_DATA.md) with 3 test candidate profiles
- Hybrid design supporting both curated (ScriptableObject) and procedural (runtime) content

### ⏳ In Progress
- Creating ScriptableObject assets in Unity (using sample data)

### 📋 To Do
- Sample client data (create a few ClientProfileSO assets for story progression)
- Manager classes (GameManager, ProfileManager, QuestManager, MatchQueueManager)
- Controller classes (MatchListController, SocialFeedController, DecisionController)
- Random post selection system
- Matching/scoring algorithm
- Procedural generation systems (QuestGenerator - creates Quest instances from ClientProfileSO or procedural data)
- UI implementation
