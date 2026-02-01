# Data Import System

All game data is managed through JSON files. Never create ScriptableObjects manually.

## Workflow

1. **Edit JSON files** in `JSONData/` folder
2. **In Unity**: `Tools > Maskhot > Import Data from JSON`
3. **Confirm import** - All ScriptableObjects are created in `Assets/Resources/GameData/`
4. **Test** - Use ProfileManager (enable `verboseLogging`) or tester scripts

## JSON Files

| File | Content | Output Location |
|------|---------|-----------------|
| `Interests.json` | 25 hobbies/activities | `GameData/Traits/Interests/` |
| `PersonalityTraits.json` | 15 character traits | `GameData/Traits/Personality/` |
| `LifestyleTraits.json` | 10 daily life patterns | `GameData/Traits/Lifestyle/` |
| `NarrativeHints.json` | 15 hint collections | `GameData/NarrativeHints/` |
| `Candidates.json` | 10 candidate profiles | `GameData/Profiles/` |
| `Clients.json` | 5 story clients | `GameData/Clients/` |
| `RandomPosts.json` | 1000 random posts | `GameData/PostPool/RandomPostPool.asset` |

## Import Order

The importer automatically handles dependencies:

1. Interests, Personality Traits, Lifestyle Traits
2. Resolve trait cross-references (opposites, related, etc.)
3. Narrative Hints (references traits)
4. Random Posts (references traits)
5. Candidates (references traits, contains posts)
6. Clients (references traits, hints, contains match criteria)

## JSON Structures

### Interest

```json
{
  "displayName": "Hiking",
  "description": "Loves outdoor adventures and nature trails",
  "category": "Outdoor",
  "matchWeight": 7,
  "relatedInterests": ["Photography", "Travel"]
}
```

**Categories**: `Outdoor`, `Indoor`, `Creative`, `Athletic`, `Social`, `Intellectual`, `Entertainment`, `Food`

### Personality Trait

```json
{
  "displayName": "Outgoing",
  "description": "Energetic and social, enjoys meeting new people",
  "isPositiveTrait": true,
  "matchWeight": 7,
  "oppositeTraits": ["Introverted"],
  "complementaryTraits": ["Adventurous", "Empathetic"]
}
```

### Lifestyle Trait

```json
{
  "displayName": "Night Owl",
  "description": "Most active and energetic in the evening",
  "category": "Schedule",
  "matchWeight": 6,
  "conflictingTraits": ["EarlyRiser"],
  "compatibleTraits": []
}
```

**Categories**: `Schedule`, `SocialPreference`, `HealthWellness`, `LivingStyle`

### Narrative Hint Collection

```json
{
  "name": "Cooking_Hints",
  "relatedInterests": ["Cooking"],
  "relatedPersonalityTraits": [],
  "relatedLifestyleTraits": [],
  "hints": ["enjoys fine cuisine", "loves gourmet food", "foodie at heart"]
}
```

### Candidate

```json
{
  "characterName": "Sarah Mitchell",
  "gender": "Female",
  "age": 26,
  "bio": "Coffee enthusiast and weekend hiker...",
  "archetype": "Adventurous",
  "personalityTraits": ["Outgoing", "Adventurous", "Creative"],
  "interests": ["Hiking", "Cooking", "Reading"],
  "lifestyleTraits": ["EarlyRiser", "SocialButterfly", "FitnessFocused"],
  "randomPostRange": [2, 5],
  "friendsCountRange": [100, 500],
  "guaranteedPosts": [
    {
      "postType": "Photo",
      "content": "Summit sunrise this morning!",
      "daysSincePosted": 2,
      "likes": 247,
      "comments": 18,
      "relatedInterests": ["Hiking", "Fitness"],
      "relatedPersonalityTraits": ["Adventurous"],
      "relatedLifestyleTraits": ["EarlyRiser", "FitnessFocused"],
      "isGreenFlag": true,
      "isRedFlag": false
    }
  ]
}
```

**Genders**: `Male`, `Female`, `NonBinary`
**Archetypes**: `Adventurous`, `Creative`, `Intellectual`, `Nurturing`, `Analytical`, `Spontaneous`, `Ambitious`
**Post Types**: `Photo`, `TextOnly`, `Story`, `Video`, `Poll`, `SharedPost`

### Client

```json
{
  "clientName": "Marcus",
  "gender": "Male",
  "age": 28,
  "relationship": "College roommate",
  "backstory": "You've been friends since freshman year...",
  "archetype": "Adventurous",
  "personalityTraits": ["Outgoing", "Adventurous"],
  "interests": ["Hiking", "Travel"],
  "lifestyleTraits": ["FitnessFocused"],
  "isStoryClient": true,
  "suggestedLevel": 1,
  "introduction": "Hey! I'm looking for someone active...",
  "matchCriteria": {
    "acceptableGenders": ["Female", "NonBinary"],
    "minAge": 24,
    "maxAge": 32,
    "traitRequirements": [
      {
        "narrativeHints": "ActiveLifestyle_Hints",
        "acceptableInterests": ["Hiking", "Fitness", "Travel"],
        "acceptablePersonalityTraits": ["Adventurous"],
        "acceptableLifestyleTraits": ["FitnessFocused"],
        "level": "Required"
      }
    ],
    "dealbreakerPersonalityTraits": ["Lazy"],
    "maxRedFlags": 2,
    "minGreenFlags": 1
  }
}
```

**Requirement Levels**: `Required`, `Preferred`, `Avoid`

### Random Post

```json
{
  "postType": "Photo",
  "content": "Beautiful day for a walk in the park!",
  "relatedInterests": ["Hiking", "Photography"],
  "relatedPersonalityTraits": ["Optimistic"],
  "relatedLifestyleTraits": ["FitnessFocused"],
  "isGreenFlag": true,
  "isRedFlag": false
}
```

## Adding New Data

### Add a new trait

1. Add to appropriate JSON file (`Interests.json`, `PersonalityTraits.json`, or `LifestyleTraits.json`)
2. Run import
3. Update any profiles/posts that should reference it

### Add a new candidate

1. Add to `Candidates.json`
2. Reference existing traits by `displayName`
3. Add guaranteed posts with trait references
4. Run import

### Add random posts

1. Add to `RandomPosts.json`
2. Reference existing traits by `displayName`
3. Run import - posts are added to the global pool

## Troubleshooting

**"Reference not found" errors**: Trait name in profile/post doesn't match any loaded trait. Check spelling and that the trait exists in the traits JSON.

**Missing assets after import**: Check Unity console for errors. The importer stops on first error.

**Want to start fresh**: Delete `Assets/Resources/GameData/` folder, then reimport.

## Code Reference

**Importer**: `Assets/Scripts/Editor/ScriptableObjectImporter.cs`

Key method: `ImportAllData()` - orchestrates the full import pipeline

Menu location: `Tools > Maskhot > Import Data from JSON`
