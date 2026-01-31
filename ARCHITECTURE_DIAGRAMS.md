# Maskhot - Architecture Diagrams

This document contains Mermaid diagrams showing the relationships between all data structures in the Maskhot project.

---

## 1. High-Level System Overview

```mermaid
graph TB
    subgraph "Runtime Quest System"
        Quest[Quest]
        Client[ClientProfile]
        Criteria[MatchCriteria]
    end

    subgraph "Candidate System"
        CandidateSO[CandidateProfileSO]
        Candidate[CandidateProfile]
        Post[SocialMediaPost]
    end

    subgraph "Authored Data (ScriptableObjects)"
        ClientSO[ClientProfileSO]
        Hints[NarrativeHintCollectionSO]
        Interest[InterestSO]
        Personality[PersonalityTraitSO]
        Lifestyle[LifestyleTraitSO]
    end

    Quest --> Client
    Quest --> Criteria
    ClientSO --> Client
    CandidateSO --> Candidate
    CandidateSO --> Post

    style Quest fill:#90EE90
    style ClientSO fill:#FFD700
    style CandidateSO fill:#FFD700
    style Hints fill:#FFD700
    style Interest fill:#87CEEB
    style Personality fill:#87CEEB
    style Lifestyle fill:#87CEEB
```

**Legend**:
- 🟢 Green: Runtime-generated classes
- 🟡 Yellow: ScriptableObject wrappers
- 🔵 Blue: Core trait ScriptableObjects

---

## 2. Quest System - Detailed

```mermaid
graph LR
    Quest[Quest<br/>Runtime Instance]
    Client[ClientProfile]
    Criteria[MatchCriteria]
    TraitReq[TraitRequirement]
    Hints[NarrativeHintCollectionSO]

    Quest -->|client| Client
    Quest -->|matchCriteria| Criteria
    Quest -->|introductionText| Text[string]

    Criteria -->|traitRequirements| TraitReq
    Criteria -->|dealbreaker traits| Traits[Trait SOs]
    Criteria -->|maxRedFlags<br/>minGreenFlags| Ints[int]

    TraitReq -->|narrativeHints| Hints
    TraitReq -->|acceptableInterests| Interest[InterestSO]
    TraitReq -->|acceptablePersonality| Personality[PersonalityTraitSO]
    TraitReq -->|acceptableLifestyle| Lifestyle[LifestyleTraitSO]
    TraitReq -->|level| Level[RequirementLevel enum]

    style Quest fill:#90EE90
    style Client fill:#FFE4B5
    style Criteria fill:#FFE4B5
    style Hints fill:#FFD700
```

---

## 3. Client Profile System

```mermaid
graph TB
    ClientSO[ClientProfileSO<br/>ScriptableObject]
    Client[ClientProfile<br/>Serializable]

    ClientSO -->|profile| Client
    ClientSO -->|isStoryClient| Bool1[bool]
    ClientSO -->|suggestedLevel| Int1[int]

    Client -->|clientName| Name[string]
    Client -->|gender| Gender[Gender enum]
    Client -->|age| Age[int]
    Client -->|profilePicture| Sprite1[Sprite]
    Client -->|relationship| Rel[string]
    Client -->|backstory| Story[string]
    Client -->|archetype| Arch[PersonalityArchetype enum]

    Client -->|personalityTraits| PT[PersonalityTraitSO array]
    Client -->|interests| IT[InterestSO array]
    Client -->|lifestyleTraits| LT[LifestyleTraitSO array]

    style ClientSO fill:#FFD700
    style Client fill:#FFE4B5
```

---

## 4. Candidate Profile System

```mermaid
graph TB
    CandidateSO[CandidateProfileSO<br/>ScriptableObject]
    Candidate[CandidateProfile<br/>Serializable]
    Post[SocialMediaPost<br/>Serializable]

    CandidateSO -->|profile| Candidate
    CandidateSO -->|guaranteedPosts| Post

    Candidate -->|characterName| Name[string]
    Candidate -->|gender| Gender[Gender enum]
    Candidate -->|age| Age[int]
    Candidate -->|bio| Bio[string]
    Candidate -->|profilePhoto| Sprite1[Sprite]
    Candidate -->|archetype| Arch[PersonalityArchetype enum]

    Candidate -->|personalityTraits| PT[PersonalityTraitSO array]
    Candidate -->|interests| IT[InterestSO array]
    Candidate -->|lifestyleTraits| LT[LifestyleTraitSO array]

    Post -->|postType| Type[PostType enum]
    Post -->|content| Content[string]
    Post -->|imageContent| Image[Sprite]
    Post -->|timestamp| Time[string]
    Post -->|likes/comments| Engagement[int]
    Post -->|isGreenFlag/isRedFlag| Flags[bool]
    Post -->|categories| Cat[PostCategory array]

    Post -->|relatedInterests| PostIT[InterestSO array]
    Post -->|relatedPersonalityTraits| PostPT[PersonalityTraitSO array]
    Post -->|relatedLifestyleTraits| PostLT[LifestyleTraitSO array]

    style CandidateSO fill:#FFD700
    style Candidate fill:#FFE4B5
    style Post fill:#FFE4B5
```

---

## 5. Trait System (ScriptableObjects)

```mermaid
graph TB
    subgraph "Trait ScriptableObjects"
        Interest[InterestSO]
        Personality[PersonalityTraitSO]
        Lifestyle[LifestyleTraitSO]
    end

    Interest -->|displayName| Name1[string]
    Interest -->|icon| Icon1[Sprite]
    Interest -->|description| Desc1[string]
    Interest -->|category| Cat1[InterestCategory enum]
    Interest -->|matchWeight| Weight1[int 1-10]
    Interest -->|relatedInterests| Related1[InterestSO array]

    Personality -->|displayName| Name2[string]
    Personality -->|icon| Icon2[Sprite]
    Personality -->|description| Desc2[string]
    Personality -->|isPositive| Pos[bool]
    Personality -->|matchWeight| Weight2[int 1-10]
    Personality -->|oppositeTrait| Opp[PersonalityTraitSO]
    Personality -->|complementaryTraits| Comp[PersonalityTraitSO array]

    Lifestyle -->|displayName| Name3[string]
    Lifestyle -->|icon| Icon3[Sprite]
    Lifestyle -->|description| Desc3[string]
    Lifestyle -->|category| Cat3[LifestyleCategory enum]
    Lifestyle -->|matchWeight| Weight3[int 1-10]
    Lifestyle -->|conflictingTraits| Conflict[LifestyleTraitSO array]
    Lifestyle -->|compatibleTraits| Compatible[LifestyleTraitSO array]

    style Interest fill:#87CEEB
    style Personality fill:#87CEEB
    style Lifestyle fill:#87CEEB
```

---

## 6. Narrative Hint System

```mermaid
graph TB
    Hints[NarrativeHintCollectionSO<br/>ScriptableObject]

    Hints -->|relatedInterests| Interest[InterestSO array]
    Hints -->|relatedPersonalityTraits| Personality[PersonalityTraitSO array]
    Hints -->|relatedLifestyleTraits| Lifestyle[LifestyleTraitSO array]
    Hints -->|hints| HintStrings[string array]
    Hints -->|GetRandomHint| Method[Returns random hint]

    TraitReq[TraitRequirement] -.->|references| Hints

    style Hints fill:#FFD700
    style TraitReq fill:#FFE4B5
```

**Note**: NarrativeHintCollectionSO provides the abstract, player-facing text ("enjoys fine cuisine") while the backend trait references provide the concrete matching logic.

---

## 7. Match Criteria Deep Dive

```mermaid
graph TB
    Criteria[MatchCriteria]

    subgraph "Basic Preferences"
        Criteria -->|acceptableGenders| Genders[Gender array]
        Criteria -->|minAge/maxAge| Ages[int]
    end

    subgraph "Trait Requirements"
        Criteria -->|traitRequirements| TraitReq[TraitRequirement array]
        TraitReq -->|narrativeHints| Hints[NarrativeHintCollectionSO]
        TraitReq -->|acceptableInterests| AcceptInt[InterestSO array]
        TraitReq -->|acceptablePersonality| AcceptPer[PersonalityTraitSO array]
        TraitReq -->|acceptableLifestyle| AcceptLife[LifestyleTraitSO array]
        TraitReq -->|level| Level[RequirementLevel enum]
    end

    subgraph "Dealbreakers"
        Criteria -->|dealbreakerPersonalityTraits| DBPer[PersonalityTraitSO array]
        Criteria -->|dealbreakerInterests| DBInt[InterestSO array]
        Criteria -->|dealbreakerLifestyleTraits| DBLife[LifestyleTraitSO array]
    end

    subgraph "Red Flag Tolerance"
        Criteria -->|maxRedFlags| MaxRed[int]
        Criteria -->|minGreenFlags| MinGreen[int]
    end

    subgraph "Scoring Weights"
        Criteria -->|personalityWeight| PW[float 0-1]
        Criteria -->|interestsWeight| IW[float 0-1]
        Criteria -->|lifestyleWeight| LW[float 0-1]
    end

    style Criteria fill:#FFE4B5
    style Hints fill:#FFD700
```

---

## 8. Complete Data Flow (Gameplay)

```mermaid
sequenceDiagram
    participant QG as QuestGenerator
    participant ClientSO as ClientProfileSO
    participant Quest as Quest (runtime)
    participant UI as UI System
    participant Player as Player
    participant CandSO as CandidateProfileSO
    participant DC as DecisionController

    QG->>ClientSO: Load curated client or generate procedural
    QG->>Quest: Create Quest instance
    Quest->>UI: Display client info + criteria

    UI->>CandSO: Load candidate pool
    UI->>Player: Show candidate queue

    Player->>UI: Select candidate
    UI->>CandSO: Get profile + posts
    UI->>Player: Display social media feed

    Player->>UI: Analyze posts against criteria
    Player->>UI: Click Accept/Reject
    UI->>DC: Submit decision
    DC->>DC: Score match quality
    DC->>UI: Return feedback
    UI->>Player: Show next candidate
```

---

## 9. Trait Usage Across Systems

```mermaid
graph TB
    subgraph "Trait ScriptableObjects (Authored Once)"
        Interest[InterestSO]
        Personality[PersonalityTraitSO]
        Lifestyle[LifestyleTraitSO]
    end

    subgraph "Used By: Profiles"
        ClientProfile[ClientProfile]
        CandidateProfile[CandidateProfile]
    end

    subgraph "Used By: Posts"
        Post[SocialMediaPost]
    end

    subgraph "Used By: Criteria"
        TraitReq[TraitRequirement]
        Dealbreaker[Dealbreaker Arrays]
    end

    subgraph "Used By: Hints"
        Hints[NarrativeHintCollectionSO]
    end

    Interest --> ClientProfile
    Personality --> ClientProfile
    Lifestyle --> ClientProfile

    Interest --> CandidateProfile
    Personality --> CandidateProfile
    Lifestyle --> CandidateProfile

    Interest --> Post
    Personality --> Post
    Lifestyle --> Post

    Interest --> TraitReq
    Personality --> TraitReq
    Lifestyle --> TraitReq

    Interest --> Dealbreaker
    Personality --> Dealbreaker
    Lifestyle --> Dealbreaker

    Interest --> Hints
    Personality --> Hints
    Lifestyle --> Hints

    style Interest fill:#87CEEB
    style Personality fill:#87CEEB
    style Lifestyle fill:#87CEEB
```

**Key Insight**: Trait ScriptableObjects are the central hub - they're referenced by almost every other system. This allows for:
- Consistency across the game
- Easy balancing (change matchWeight in one place)
- Type-safe references
- No string-based lookups

---

## 10. Procedural vs Curated Content Flow

```mermaid
graph TB
    subgraph "Curated Content Path"
        ClientSO[ClientProfileSO<br/>Hand-crafted in Unity]
        CandidateSO[CandidateProfileSO<br/>Hand-crafted in Unity]
        HintsSO[NarrativeHintCollectionSO<br/>Hand-crafted in Unity]
        TraitSO[Trait SOs<br/>Hand-crafted in Unity]
    end

    subgraph "Procedural Generation Path"
        QuestGen[QuestGenerator]
        ClientGen[Generates ClientProfile]
        CriteriaGen[Generates MatchCriteria]
    end

    subgraph "Runtime Quest"
        Quest[Quest Instance]
    end

    ClientSO -->|Option A: Story Quest| Quest
    QuestGen -->|Option B: Procedural Quest| Quest
    QuestGen --> ClientGen
    QuestGen --> CriteriaGen
    ClientGen -.->|May reference| TraitSO
    CriteriaGen -.->|References| HintsSO
    CriteriaGen -.->|References| TraitSO

    CandidateSO -->|Always used| Gameplay[Gameplay System]

    style ClientSO fill:#FFD700
    style CandidateSO fill:#FFD700
    style HintsSO fill:#FFD700
    style TraitSO fill:#87CEEB
    style Quest fill:#90EE90
```

**Design Philosophy**:
- **Candidates**: Always curated (hand-crafted CandidateProfileSO with posts)
- **Clients**: Can be curated (ClientProfileSO for story) or procedural (generated ClientProfile)
- **Quests**: Always procedural (Quest instances created at runtime)
- **Traits**: Always curated (InterestSO, PersonalityTraitSO, LifestyleTraitSO)
- **Hints**: Always curated (NarrativeHintCollectionSO)

This hybrid approach allows for:
- Quick content creation (generate quests from authored traits/hints)
- Consistent quality (candidates are hand-crafted)
- Story flexibility (can use curated clients or go fully procedural)

---

## 11. Enums Reference

```mermaid
graph LR
    subgraph "Profile Enums"
        Gender[Gender<br/>Male, Female, NonBinary]
        Archetype[PersonalityArchetype<br/>Adventurous, Creative,<br/>Intellectual, etc.]
    end

    subgraph "Post Enums"
        PostType[PostType<br/>Photo, TextOnly, Story,<br/>Video, Poll, SharedPost]
        PostCat[PostCategory<br/>Hobby, Personality, Lifestyle,<br/>Family, Friends, Work,<br/>Travel, Food, Fitness, Pets]
    end

    subgraph "Trait Enums"
        InterestCat[InterestCategory<br/>Outdoor, Indoor, Creative,<br/>Athletic, Social, Intellectual]
        LifestyleCat[LifestyleCategory<br/>Activity Level, Social Preference,<br/>Schedule, Living Style]
    end

    subgraph "Criteria Enums"
        ReqLevel[RequirementLevel<br/>Required, Preferred, Avoid]
    end

    ClientProfile -.-> Gender
    ClientProfile -.-> Archetype
    CandidateProfile -.-> Gender
    CandidateProfile -.-> Archetype

    Post -.-> PostType
    Post -.-> PostCat

    InterestSO -.-> InterestCat
    LifestyleSO -.-> LifestyleCat

    TraitReq -.-> ReqLevel
```

---

## Key Takeaways

1. **Central Hub**: Trait ScriptableObjects (InterestSO, PersonalityTraitSO, LifestyleTraitSO) are referenced by nearly every system

2. **Separation of Concerns**:
   - **Quest**: "Who wants what" (client + criteria)
   - **MatchCriteria**: "What they want" (abstract hints + concrete requirements)
   - **CandidateProfile**: "Who they are" (traits + bio)
   - **SocialMediaPost**: "What they show" (content + related traits)

3. **Abstraction Layers**:
   - **Player sees**: Narrative hints ("enjoys fine cuisine")
   - **Backend matches**: Trait references (Cooking InterestSO, Creative PersonalityTraitSO)
   - **Posts reveal**: Trait associations (post about cooking → Cooking interest)

4. **ScriptableObject Strategy**:
   - Traits: Always ScriptableObjects (shared, reusable)
   - Profiles: Wrapped in SOs for curation, but data is [Serializable] for procedural use
   - Quests: Pure runtime classes, no SO wrapper needed

5. **Type Safety**: All relationships use direct ScriptableObject references, never strings - prevents typos and breaks at edit-time, not runtime

---

## File Locations

**Core Data Classes** (Scripts/Data/):
- Quest.cs
- ClientProfile.cs
- CandidateProfile.cs
- MatchCriteria.cs
- SocialMediaPost.cs

**ScriptableObject Templates** (Scripts/Data/):
- ClientProfileSO.cs
- CandidateProfileSO.cs
- InterestSO.cs
- PersonalityTraitSO.cs
- LifestyleTraitSO.cs
- NarrativeHintCollectionSO.cs

**Authored Assets** (Assets/Data/ScriptableObjects/):
- Traits/ (InterestSO, PersonalityTraitSO, LifestyleTraitSO instances)
- NarrativeHints/ (NarrativeHintCollectionSO instances)
- Profiles/ (CandidateProfileSO instances)
- Clients/ (ClientProfileSO instances)

---

For more details, see:
- **PROJECT_SETUP.md** - Implementation status and backend details
- **SAMPLE_DATA.md** - Test data for creating ScriptableObject assets
- **UI_REFERENCE.md** - How these systems connect to the UI
