# Maskhot - Claude Code Context

## CRITICAL: Working with Claude

**Read this section first. These are blocking requirements.**

### Validation Workflow for New Components

**DO NOT mark new managers or controllers as "Completed" until the user explicitly validates them.**

1. Create the component and its tester script
2. Mark as **"In Progress"** in project-status.md (NOT completed)
3. Tell the user how to test it
4. **WAIT** for the user to run tests and confirm everything works
5. Only mark as **"Completed"** AFTER the user says it's validated

### Verify Documentation Compliance

**Before marking any implementation task as done, you MUST verify you have followed ALL instructions in the relevant documentation.**

1. Read the relevant doc (e.g., `docs/testing.md` for testers, `docs/architecture.md` for managers)
2. Follow ALL conventions and requirements listed there
3. Create ALL required files (e.g., testers require BOTH the tester script AND the custom editor)
4. Update ALL documentation that needs updating

**Do not rely on memory. Re-read the docs if unsure.**

### Always Check Project Status

Before starting work, check [docs/project-status.md](docs/project-status.md) for current implementation status and priorities.

### Ask Clarifying Questions

When requirements are ambiguous or there are multiple valid approaches, ask clarifying questions before proceeding.

### Propose Plans Before Executing

For multi-step tasks or significant changes, propose a plan and wait for confirmation before executing.

---

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
Assets/Scripts/
├── Controllers/     # UI state controllers
├── Data/            # Data classes, ScriptableObjects
├── Editor/          # Editor scripts (importers, tester editors)
├── Managers/        # Data/logic managers
├── Matching/        # MatchEvaluator, MatchResult
└── Testing/         # Tester scripts
```

## Documentation

| Doc | When to Read |
|-----|--------------|
| [project-status.md](docs/project-status.md) | **Always** - Check before starting work |
| [architecture.md](docs/architecture.md) | When creating managers/controllers |
| [testing.md](docs/testing.md) | When creating/updating testers |
| [ui-reference.md](docs/ui-reference.md) | For UI implementation |
| [templates.md](docs/templates.md) | Code templates for new components |
| [overview.md](docs/overview.md) | System diagrams |
| [profiles-and-traits.md](docs/profiles-and-traits.md) | Working with profiles/traits |
| [matching-system.md](docs/matching-system.md) | MatchEvaluator details |
| [quest-system.md](docs/quest-system.md) | Quest/criteria details |
| [random-post-system.md](docs/random-post-system.md) | PostPoolManager details |
| [data-import.md](docs/data-import.md) | JSON import workflow |
| [redaction-system.md](docs/redaction-system.md) | Post redaction mechanics |

## Key Systems

**Managers** (data/logic, singleton, `Maskhot.Managers`):
ProfileManager, PostPoolManager, MatchQueueManager, QuestManager, GameManager, RedactionManager

**Controllers** (UI state/events, singleton, `Maskhot.Controllers`):
MatchListController, QuestController, DecisionController, RedactionController

**Other**: MatchEvaluator (static), ScriptableObjectImporter (`Tools > Maskhot > Import Data from JSON`)

## Development Guidelines

1. **Data changes**: Edit JSON in `JSONData/`, reimport via Unity menu
2. **Testing**: Use tester scripts via Inspector buttons (Play Mode)
3. **ScriptableObjects**: Never create manually - use JSON import
4. **Managers/Controllers**: Follow singleton pattern, use events for UI
5. **No git**: User handles version control
6. **Keep docs updated**: Update relevant docs after changes
