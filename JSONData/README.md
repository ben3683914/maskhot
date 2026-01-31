# JSON Data Import System

This folder contains JSON files that define all ScriptableObject data for the game.

## Quick Start

1. **Edit JSON files** in this folder to add/modify game data
2. **In Unity**, go to menu: `Tools > Maskhot > Import Data from JSON`
3. **Click "Yes"** to confirm the import
4. **Done!** All ScriptableObjects are created and populated automatically

## File Descriptions

### Interests.json
Defines interest traits (hobbies, activities, etc.)

**Fields:**
- `assetName`: Unique identifier for the asset file
- `displayName`: Name shown in-game
- `description`: Explanation of the interest
- `category`: Category grouping
- `matchWeight`: Importance for matching (1-10)
- `relatedInterests`: Array of related interest asset names

### PersonalityTraits.json
Defines personality traits (outgoing, creative, etc.)

**Fields:**
- `assetName`: Unique identifier for the asset file
- `displayName`: Name shown in-game
- `description`: Explanation of the trait
- `isPositive`: Whether this is a positive trait
- `matchWeight`: Importance for matching (1-10)
- `oppositeTrait`: Asset name of the opposite trait (empty string if none)
- `complementaryTraits`: Array of trait asset names that work well together

### LifestyleTraits.json
Defines lifestyle traits (night owl, fitness focused, etc.)

**Fields:**
- `assetName`: Unique identifier for the asset file
- `displayName`: Name shown in-game
- `description`: Explanation of the trait
- `category`: Category grouping
- `matchWeight`: Importance for matching (1-10)
- `conflictingTraits`: Array of traits that conflict with this one
- `compatibleTraits`: Array of traits that work well together

### Candidates.json
Defines candidate profiles with their posts

**Profile Fields:**
- `assetName`: Unique identifier for the asset file
- `characterName`: Full character name
- `gender`: Character's gender
- `age`: Character's age
- `bio`: Character bio/description
- `archetype`: Main character archetype
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

## Important Notes

### Asset References
When one object references another (e.g., a profile references an interest), use the `assetName` field as the identifier. The import script will automatically resolve these references.

**Example:**
```json
"interests": ["Hiking", "Cooking", "Reading"]
```
This will link to the Hiking.asset, Cooking.asset, and Reading.asset files created from Interests.json.

### Import Order
The importer automatically handles dependencies:
1. Creates all trait ScriptableObjects first
2. Resolves trait-to-trait references
3. Creates candidate profiles and links them to traits

### Overwriting
The import system will **overwrite existing assets** with the same names. If you've made manual changes in Unity's inspector, they will be lost on the next import.

**Recommendation:** Keep all data in JSON files and reimport when changes are needed.

### File Locations
Generated assets are saved to:
- `Assets/Data/ScriptableObjects/Traits/Interests/`
- `Assets/Data/ScriptableObjects/Traits/PersonalityTraits/`
- `Assets/Data/ScriptableObjects/Traits/LifestyleTraits/`
- `Assets/Data/ScriptableObjects/Profiles/`

## Adding New Data

### To add a new Interest:
1. Open `Interests.json`
2. Add a new object to the `interests` array
3. Run the import tool

### To add a new Candidate:
1. Open `Candidates.json`
2. Add a new object to the `candidates` array
3. Fill in all required fields
4. Run the import tool

## Troubleshooting

**Import fails with "file not found"**
- Make sure JSON files are in the `JSONData/` folder (same level as `Assets/`)
- Check that filenames are exact: `Interests.json`, `PersonalityTraits.json`, etc.

**References not resolving**
- Check that `assetName` values match exactly (case-sensitive)
- Make sure referenced assets exist in their respective JSON files

**Changes not appearing in Unity**
- Remember to run `Tools > Maskhot > Import Data from JSON` after editing JSON
- Check the Console for import results and errors
