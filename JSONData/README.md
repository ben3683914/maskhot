# JSON Data Import System

This folder contains JSON files that define all ScriptableObject data for the game.

## Quick Start

1. **Edit JSON files** in this folder to add/modify game data
2. **In Unity**, go to menu: `Tools > Maskhot > Import Data from JSON`
3. **Click "Yes"** to confirm the import
4. **Done!** All ScriptableObjects are created and populated automatically

## Current Dataset

**25 Interests** - Hobbies and activities (Hiking, Gaming, Yoga, Photography, etc.)
**15 Personality Traits** - Character traits (Outgoing, Creative, Controversial, etc.)
**10 Lifestyle Traits** - Daily life patterns (Night Owl, Career Driven, Eco Friendly, etc.)
**10 Candidate Profiles** - Complete dating profiles with 1-5 posts each (29 total posts)

## File Descriptions

### Interests.json
Defines interest traits (hobbies, activities, etc.)

**Current Count:** 25 interests

**Fields:**
- `assetName`: Unique identifier for the asset file
- `displayName`: Name shown in-game
- `description`: Explanation of the interest
- `category`: Category grouping (Outdoor, Creative, Athletic, Entertainment, etc.)
- `matchWeight`: Importance for matching (1-10)
- `relatedInterests`: Array of related interest asset names

**Example:**
```json
{
  "assetName": "Yoga",
  "displayName": "Yoga",
  "description": "Practices yoga for physical and mental wellness",
  "category": "Athletic",
  "matchWeight": 6,
  "relatedInterests": ["Fitness", "Meditation"]
}
```

### PersonalityTraits.json
Defines personality traits (outgoing, creative, etc.)

**Current Count:** 15 personality traits

**Fields:**
- `assetName`: Unique identifier for the asset file
- `displayName`: Name shown in-game
- `description`: Explanation of the trait
- `isPositive`: Whether this is a positive trait (true/false)
- `matchWeight`: Importance for matching (1-10)
- `oppositeTraits`: **Array** of opposite trait asset names (empty array if none)
- `complementaryTraits`: Array of trait asset names that work well together

**Special Trait - Controversial:**
The "Controversial" trait (`isPositive: false`) is used to identify characters who make divisive or polarizing statements. This replaces the old category-based system for flagging controversial posts. Use this trait in `relatedPersonalityTraits` for red flag posts.

**Example:**
```json
{
  "assetName": "Controversial",
  "displayName": "Controversial",
  "description": "Tends to express strong or divisive opinions publicly",
  "isPositive": false,
  "matchWeight": 9,
  "oppositeTraits": ["Thoughtful"],
  "complementaryTraits": []
}
```

### LifestyleTraits.json
Defines lifestyle traits (night owl, fitness focused, etc.)

**Current Count:** 10 lifestyle traits

**Fields:**
- `assetName`: Unique identifier for the asset file
- `displayName`: Name shown in-game
- `description`: Explanation of the trait
- `category`: Category grouping (Schedule, Social_Preference, Health_Wellness, Work_Life, etc.)
- `matchWeight`: Importance for matching (1-10)
- `conflictingTraits`: Array of traits that conflict with this one
- `compatibleTraits`: Array of traits that work well together

**Example:**
```json
{
  "assetName": "Career_Driven",
  "displayName": "Career Driven",
  "description": "Focused on professional growth and success",
  "category": "Work_Life",
  "matchWeight": 7,
  "conflictingTraits": ["Spontaneous"],
  "compatibleTraits": ["EarlyRiser", "Ambitious"]
}
```

### Candidates.json
Defines candidate profiles with their posts

**Current Count:** 10 candidates (varying post counts: 1-5 posts each)

**Profile Fields:**
- `assetName`: Unique identifier for the asset file
- `characterName`: Full character name
- `gender`: Character's gender ("Male", "Female", "Non-binary", "NonBinary")
- `age`: Character's age
- `bio`: Character bio/description
- `archetype`: Main character archetype (Adventurous, Intellectual, Creative, Athletic, etc.)
- `personalityTraits`: Array of personality trait asset names
- `interests`: Array of interest asset names
- `lifestyleTraits`: Array of lifestyle trait asset names
- `randomPostCount`: Number of random posts to add during gameplay
- `randomPostTagFilter`: Tags for filtering random posts (empty array = any)
- `guaranteedPosts`: Array of post objects

**Post Fields:**
- `postType`: Type of post ("Photo", "TextOnly", "Video", "Story", "SharedPost", "Poll")
- `content`: Text content of the post
- `daysSincePosted`: Integer days ago (for sorting, 1 = yesterday)
- `likes`: Number of likes
- `comments`: Number of comments
- `isGreenFlag`: Boolean - is this a green flag post?
- `isRedFlag`: Boolean - is this a red flag post?
- `relatedInterests`: Array of interest asset names (empty array if none)
- `relatedPersonalityTraits`: Array of personality trait asset names (empty array if none)
- `relatedLifestyleTraits`: Array of lifestyle trait asset names (empty array if none)

**Example Candidate:**
```json
{
  "assetName": "PriyaSharma",
  "characterName": "Priya Sharma",
  "gender": "Female",
  "age": 29,
  "bio": "Yoga instructor and wellness coach. Starting each day with gratitude and intention.",
  "archetype": "Free_Spirit",
  "personalityTraits": ["Optimistic", "Patient", "Empathetic", "Thoughtful"],
  "interests": ["Yoga", "Meditation", "Gardening", "Cooking", "Baking"],
  "lifestyleTraits": ["EarlyRiser", "Health_Conscious", "Eco_Friendly"],
  "randomPostCount": 5,
  "randomPostTagFilter": [],
  "guaranteedPosts": [
    {
      "postType": "Photo",
      "content": "Morning practice overlooking the ocean. Grateful for these peaceful moments 🧘‍♀️☀️",
      "daysSincePosted": 1,
      "likes": 312,
      "comments": 45,
      "isGreenFlag": true,
      "isRedFlag": false,
      "relatedInterests": ["Yoga", "Meditation"],
      "relatedPersonalityTraits": ["Optimistic", "Patient"],
      "relatedLifestyleTraits": ["EarlyRiser", "Health_Conscious"]
    }
  ]
}
```

## Important Notes

### Asset References
When one object references another (e.g., a profile references an interest), use the `assetName` field as the identifier. The import script will automatically resolve these references.

**Example:**
```json
"interests": ["Hiking", "Cooking", "Reading"]
```
This will link to the Hiking.asset, Cooking.asset, and Reading.asset files created from Interests.json.

### Enum Values

**Gender:**
- "Male"
- "Female"
- "Non-binary" or "NonBinary" (both formats accepted)

**Archetype:**
- Adventurous, Intellectual, Creative, Athletic, Homebody, Social, Career_Focused, Free_Spirit, Traditional, Quirky

**PostType:**
- Photo, TextOnly, Video, Story, SharedPost, Poll

### Import Order
The importer automatically handles dependencies:
1. Creates all trait ScriptableObjects first
2. Resolves trait-to-trait references
3. Creates candidate profiles and links them to traits

### Overwriting
The import system will **overwrite existing assets** with the same names. If you've made manual changes in Unity's inspector, they will be lost on the next import.

**Recommendation:** Keep all data in JSON files and reimport when changes are needed.

### File Locations
Generated assets are saved to `Assets/Resources/GameData/` for runtime loading:
- `Assets/Resources/GameData/Traits/Interests/`
- `Assets/Resources/GameData/Traits/Personality/`
- `Assets/Resources/GameData/Traits/Lifestyle/`
- `Assets/Resources/GameData/Profiles/`

**Note:** Assets are in the Resources folder so `ProfileManager` can auto-load them at runtime using `Resources.LoadAll<T>()`.

## Adding New Data

### To add a new Interest:
1. Open `Interests.json`
2. Add a new object to the `interests` array
3. Fill in all required fields
4. Run the import tool: `Tools > Maskhot > Import Data from JSON`

### To add a new Personality Trait:
1. Open `PersonalityTraits.json`
2. Add a new object to the `personalityTraits` array
3. Set `isPositive` appropriately (most traits are true, except controversial/negative ones)
4. Define relationships with `oppositeTraits` and `complementaryTraits` arrays
5. Run the import tool

### To add a new Candidate:
1. Open `Candidates.json`
2. Add a new object to the `candidates` array
3. Fill in all profile fields
4. Add 1-5 posts in the `guaranteedPosts` array
5. For red flag posts, reference "Controversial" in `relatedPersonalityTraits` if applicable
6. Run the import tool

## Design Guidelines

### Creating Balanced Candidates
- **Mix of flags:** Each candidate should have mostly green flags with 1-2 red flags
- **Post variety:** Vary post types (Photo vs TextOnly) and engagement levels
- **Trait coherence:** Ensure personality traits match the bio and posts
- **Red flags:** Use the "Controversial" trait for divisive opinions, or leave traits empty for general red flags

### Match Weights
- **1-3:** Low importance (minor interests or traits)
- **4-7:** Medium importance (typical traits and hobbies)
- **8-10:** High importance (core personality traits, major interests)

### Post Engagement Guidelines
- **Green flag posts:** Generally 100-600 likes
- **Red flag posts:** Usually lower engagement (10-200 likes) with high comment counts
- **Days since posted:** 1-15 days for variety in timeline

## Troubleshooting

**Import fails with "file not found"**
- Make sure JSON files are in the `JSONData/` folder (same level as `Assets/`)
- Check that filenames are exact: `Interests.json`, `PersonalityTraits.json`, etc.

**References not resolving**
- Check that `assetName` values match exactly (case-sensitive)
- Make sure referenced assets exist in their respective JSON files
- Arrays should use asset names, not display names

**Compilation errors**
- Verify all enum values match the allowed values listed above
- Check that array fields use `[]` not empty strings
- Ensure boolean fields are `true` or `false`, not strings

**Changes not appearing in Unity**
- Remember to run `Tools > Maskhot > Import Data from JSON` after editing JSON
- Check the Console for import results and errors
- Verify the JSON is valid (use a JSON validator if needed)

## Next Steps After Import

1. **Set up ProfileManager:** Create an empty GameObject, add the `ProfileManager` component - data loads automatically at runtime
2. **Test with ProfileManager:** Enable `verboseLogging` on ProfileManager to see a full data dump in the console
3. **Or use ProfileTester:** Attach the ProfileTester component to a GameObject and drag in profile assets for manual testing
4. **Verify references:** Check that all trait references are properly linked in the inspector
5. **Review generated assets:** Browse `Assets/Resources/GameData/` folders to ensure everything imported correctly
6. **Iterate:** Edit JSON files and reimport as needed to refine your data
