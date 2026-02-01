# Maskhot - System Overview

High-level diagrams showing how all systems connect. For implementation details, see the specific system docs.

## Game Flow

```mermaid
sequenceDiagram
    participant QG as QuestManager
    participant ClientSO as ClientProfileSO
    participant Quest as Quest (runtime)
    participant UI as UI System
    participant Player as Player
    participant SFC as SocialFeedController
    participant ME as MatchEvaluator

    QG->>ClientSO: Load client profile
    QG->>Quest: Create Quest instance
    Quest->>UI: Display client info + criteria

    UI->>SFC: Load candidate queue
    UI->>Player: Show candidate list

    Player->>UI: Select candidate
    UI->>SFC: SetCandidate(candidate)
    SFC->>SFC: Fire OnCandidateChanged event
    UI->>Player: Display social media feed

    Player->>UI: Click Accept/Reject
    UI->>ME: Evaluate(candidate, criteria)
    ME->>UI: Return MatchResult
    UI->>Player: Show feedback, next candidate
```

## System Architecture

```mermaid
graph TB
    subgraph "Data Layer"
        JSON[JSONData/*.json]
        Importer[ScriptableObjectImporter]
        Resources[Resources/GameData/]
    end

    subgraph "Manager Layer (Singletons)"
        PM[ProfileManager]
        PPM[PostPoolManager]
        MQM[MatchQueueManager]
    end

    subgraph "Controller Layer (Singletons)"
        MLC[MatchListController]
    end

    subgraph "Data Access"
        Candidates[CandidateProfileSO]
        Clients[ClientProfileSO]
        Traits[Trait SOs]
        Posts[SocialMediaPost]
    end

    subgraph "Matching"
        ME[MatchEvaluator]
        MR[MatchResult]
        MC[MatchCriteria]
    end

    subgraph "Runtime"
        Quest[Quest]
        UI[UI Layer]
    end

    JSON -->|Import| Importer
    Importer -->|Create| Resources
    Resources -->|Load| PM
    Resources -->|Load| PPM

    PM -->|Provides| Candidates
    PM -->|Provides| Clients
    PM -->|Provides| Traits
    PPM -->|Generates| Posts

    Candidates -->|Queued in| MQM
    MQM -->|Selection via| MLC
    MLC -->|Events to| UI

    Clients -->|Creates| Quest
    Quest -->|Contains| MC
    MC -->|Evaluated by| ME
    ME -->|Returns| MR
```

## Data Relationships

```mermaid
graph LR
    subgraph "Trait ScriptableObjects"
        Interest[InterestSO]
        Personality[PersonalityTraitSO]
        Lifestyle[LifestyleTraitSO]
    end

    subgraph "Profile ScriptableObjects"
        CandidateSO[CandidateProfileSO]
        ClientSO[ClientProfileSO]
    end

    subgraph "Content"
        Post[SocialMediaPost]
        Hints[NarrativeHintCollectionSO]
    end

    subgraph "Criteria"
        MC[MatchCriteria]
        TR[TraitRequirement]
    end

    Interest --> CandidateSO
    Personality --> CandidateSO
    Lifestyle --> CandidateSO

    Interest --> ClientSO
    Personality --> ClientSO
    Lifestyle --> ClientSO

    Interest --> Post
    Personality --> Post
    Lifestyle --> Post

    Interest --> TR
    Personality --> TR
    Lifestyle --> TR

    Hints --> TR
    TR --> MC
    MC --> ClientSO
```

## Key Design Principles

1. **Trait SOs are the hub** - Referenced by profiles, posts, criteria, and hints
2. **JSON is the source of truth** - Edit JSON, import to create SOs
3. **Managers handle data/logic** - ProfileManager, PostPoolManager, MatchQueueManager
4. **Controllers handle UI state** - MatchListController manages selection and fires events
5. **Separation of concerns**:
   - Quest = "Who wants what"
   - MatchCriteria = "What they want"
   - CandidateProfile = "Who the candidate is"
   - SocialMediaPost = "What they show"
   - MatchQueueManager = "Which candidates are available"
   - MatchListController = "Which candidate is selected"

## Related Docs

- [architecture.md](architecture.md) - Component patterns, dependency rules, conventions
- [profiles-and-traits.md](profiles-and-traits.md) - Profile and trait system details
- [matching-system.md](matching-system.md) - How matching and scoring works
- [quest-system.md](quest-system.md) - Quest structure and criteria
- [random-post-system.md](random-post-system.md) - Post generation system
- [data-import.md](data-import.md) - JSON import workflow
