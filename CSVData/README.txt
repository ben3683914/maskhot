CSV Data Files for Unity ScriptableObject Creation
=================================================

These CSV files contain all the sample data from SAMPLE_DATA.md in an easy-to-copy format.

USAGE INSTRUCTIONS:
-------------------

1. Open the CSV file in Excel, Google Sheets, or a text editor
2. In Unity, create the ScriptableObject asset
3. Copy values from the CSV and paste into Unity's inspector fields

NOTES:
------

- Pipe character (|) separates multiple values in arrays (e.g., "Hiking|Cooking|Reading")
- Empty cells mean no value or empty array
- For trait references, use the AssetName column to find the correct SO to drag in
- Categories use pipe separators: "Hobby|Lifestyle" means both categories

FILE DESCRIPTIONS:
------------------

Interests.csv - 5 interest traits
  - Create in: Assets/Data/ScriptableObjects/Traits/Interests/

PersonalityTraits.csv - 5 personality traits
  - Create in: Assets/Data/ScriptableObjects/Traits/PersonalityTraits/

LifestyleTraits.csv - 5 lifestyle traits
  - Create in: Assets/Data/ScriptableObjects/Traits/LifestyleTraits/

Candidates.csv - 3 candidate profiles (high-level info only)
  - Create in: Assets/Data/ScriptableObjects/Profiles/

SocialMediaPosts.csv - 9 posts (3 per candidate)
  - Add these to the candidate profile SOs in the "Guaranteed Posts" list

CREATION ORDER:
---------------

1. Create ALL trait SOs first (Interests, PersonalityTraits, LifestyleTraits)
2. Create Candidate Profile SOs (reference the traits you just created)
3. Add posts to each Candidate Profile SO (reference traits as needed)
4. Test with ProfileTester.cs component

IMPORTANT:
----------

When a cell shows trait names (e.g., "Hiking|Fitness"), you need to:
1. Look up those trait SOs you created earlier
2. Drag them into the array field in Unity's inspector
3. The CSV just tells you WHICH ones to add, you still need to drag references
