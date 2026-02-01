# Maskhot - Claude Code Context

## Project Overview

A **Unity 2D social media matchmaking game** inspired by Papers Please. Players review candidates' social media profiles to find matches for clients based on abstract criteria (narrative hints).

**Core gameplay**: Client gives requirements → Player reviews candidate posts → Accept/Reject → Get scored on accuracy.

## Key Terminology

- **Candidate**: A potential match being reviewed (has social media posts)
- **Client**: The person you're finding a match for (your friend)
- **Trait**: ScriptableObject representing interests, personality, or lifestyle characteristics
- **Narrative Hint**: Abstract player-facing text like "enjoys fine cuisine" that maps to concrete traits
- **Green/Red Flag**: Positive/negative indicators on social media posts

## Technology Stack

- Unity 2D with C#
- ScriptableObjects for data management
- JSON import system for bulk data entry (`JSONData/` → `Assets/Resources/GameData/`)
- Resources folder for runtime loading via `Resources.LoadAll<T>()`

## Folder Structure

```
Assets/
├── Scripts/
│   ├── Data/           # Data classes, SOs (CandidateProfileSO, SocialMediaPost, etc.)
│   ├── Managers/       # Singletons (ProfileManager, PostPoolManager)
│   ├── Matching/       # MatchEvaluator, MatchResult
│   ├── Controllers/    # SocialFeedController, others to be implemented
│   ├── Testing/        # Tester scripts for verification
│   └── Editor/         # ScriptableObjectImporter (JSON → SO)
├── Resources/GameData/ # Runtime-loaded ScriptableObjects
└── Scenes/
JSONData/               # Source JSON files for data import
docs/                   # Detailed documentation (see below)
```

## Documentation Index

Read these docs when you need deeper context:

| Doc | When to Read |
|-----|--------------|
| [docs/overview.md](docs/overview.md) | High-level system diagrams, how all systems connect |
| [docs/architecture.md](docs/architecture.md) | Component patterns (Manager vs Controller), dependency rules, conventions |
| [docs/project-status.md](docs/project-status.md) | Implementation status, what's done/in-progress/to-do |
| [docs/profiles-and-traits.md](docs/profiles-and-traits.md) | Working with CandidateProfile, ClientProfile, trait SOs |
| [docs/matching-system.md](docs/matching-system.md) | MatchEvaluator, scoring algorithm, MatchResult |
| [docs/quest-system.md](docs/quest-system.md) | Quest, MatchCriteria, narrative hints |
| [docs/random-post-system.md](docs/random-post-system.md) | PostPoolManager, random post generation |
| [docs/data-import.md](docs/data-import.md) | JSON import workflow, adding new data |
| [docs/ui-reference.md](docs/ui-reference.md) | UI Toolkit specifics, SocialFeedController events/methods |
| [docs/testing.md](docs/testing.md) | Tester scripts, verification workflow |

## Key Systems

### Managers (Data/Logic)
- **ProfileManager** (singleton): Loads all profiles/traits from Resources, provides lookup methods
- **PostPoolManager** (singleton): Handles random post selection with trait matching
- **MatchQueueManager** (singleton): Manages candidate queue, decision tracking, queue population

### Controllers (UI State/Events)
- **MatchListController** (singleton): Manages current selection, navigation, fires `OnSelectionChanged` event

### Other
- **MatchEvaluator** (static): Evaluates candidates against match criteria, returns MatchResult with score
- **ScriptableObjectImporter**: Editor tool (`Tools > Maskhot > Import Data from JSON`)

## Working with Claude

### Ask Clarifying Questions
When requirements are ambiguous or there are multiple valid approaches, ask clarifying questions before proceeding. It's better to confirm intent than to make assumptions.

### Propose Plans Before Executing
For multi-step tasks or significant changes, propose a plan and wait for confirmation before executing. Outline what you intend to do and ask if this approach works.

### Keep Documentation Updated
1. **After modifying systems**: Update relevant documentation in `docs/` to reflect changes
2. **When adding new features**: Create or update appropriate doc with system design
3. **Update project-status.md**: Mark items complete, add new to-do items as discovered

## Development Guidelines

1. **For data changes**: Edit JSON files in `JSONData/`, then reimport via Unity menu
2. **Testing**: Use tester scripts (ProfileTester, MatchingTester, etc.) via Context Menu
3. **Test maintenance**: When creating new managers or controllers, create a corresponding tester script. When modifying existing systems, update relevant tests. **Always provide testing instructions** (how to run, what to look for) when creating or updating tests. See [docs/testing.md](docs/testing.md) for conventions.
4. **ScriptableObjects**: Never create manually - always use JSON import for consistency
5. **Controllers/Managers**: Follow singleton pattern, use events for UI communication
6. **No git commands**: Do not perform any git operations (commit, push, etc.) - the user handles version control

## Implementation Status

- **Complete**: Data structures, trait SOs, profile SOs, JSON import, matching system, managers (ProfileManager, PostPoolManager, MatchQueueManager), MatchListController
- **In Progress**: None currently
- **To Do**: GameManager, QuestManager, DecisionController, UI implementation

See [docs/project-status.md](docs/project-status.md) for detailed status breakdown.
