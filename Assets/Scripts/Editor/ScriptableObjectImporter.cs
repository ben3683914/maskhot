using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Maskhot.Data;

namespace Maskhot.Editor
{
	/// <summary>
	/// Editor tool to import ScriptableObjects from JSON data files
	/// Menu: Tools > Maskhot > Import Data from JSON
	/// </summary>
	public class ScriptableObjectImporter : EditorWindow
	{
		private const string JSON_DATA_PATH = "JSONData";
		private const string ASSET_BASE_PATH = "Assets/Data/ScriptableObjects";

		// Dictionaries to store created assets for reference resolution
		private static Dictionary<string, InterestSO> interests = new Dictionary<string, InterestSO>();
		private static Dictionary<string, PersonalityTraitSO> personalityTraits = new Dictionary<string, PersonalityTraitSO>();
		private static Dictionary<string, LifestyleTraitSO> lifestyleTraits = new Dictionary<string, LifestyleTraitSO>();

		[MenuItem("Tools/Maskhot/Import Data from JSON")]
		public static void ImportAllData()
		{
			if (!EditorUtility.DisplayDialog("Import ScriptableObjects",
				"This will create ScriptableObjects from JSON files. Any existing assets with the same names will be overwritten.\n\nContinue?",
				"Yes", "Cancel"))
			{
				return;
			}

			try
			{
				// Clear dictionaries
				interests.Clear();
				personalityTraits.Clear();
				lifestyleTraits.Clear();

				// Create folder structure
				CreateFolderStructure();

				// Import in order: traits first, then profiles (which reference traits)
				ImportInterests();
				ImportPersonalityTraits();
				ImportLifestyleTraits();

				// Resolve trait references (for traits that reference other traits)
				ResolveTraitReferences();

				// Import candidate profiles (which reference all the traits)
				ImportCandidates();

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				EditorUtility.DisplayDialog("Import Complete",
					$"Successfully imported:\n• {interests.Count} Interests\n• {personalityTraits.Count} Personality Traits\n• {lifestyleTraits.Count} Lifestyle Traits\n• Check Console for candidate import results",
					"OK");
			}
			catch (System.Exception e)
			{
				EditorUtility.DisplayDialog("Import Failed", $"Error during import:\n{e.Message}", "OK");
				Debug.LogError($"Import failed: {e}");
			}
		}

		private static void CreateFolderStructure()
		{
			CreateFolderIfNeeded("Assets/Data");
			CreateFolderIfNeeded("Assets/Data/ScriptableObjects");
			CreateFolderIfNeeded("Assets/Data/ScriptableObjects/Traits");
			CreateFolderIfNeeded("Assets/Data/ScriptableObjects/Traits/Interests");
			CreateFolderIfNeeded("Assets/Data/ScriptableObjects/Traits/PersonalityTraits");
			CreateFolderIfNeeded("Assets/Data/ScriptableObjects/Traits/LifestyleTraits");
			CreateFolderIfNeeded("Assets/Data/ScriptableObjects/Profiles");
		}

		private static void CreateFolderIfNeeded(string path)
		{
			if (!AssetDatabase.IsValidFolder(path))
			{
				string parentFolder = Path.GetDirectoryName(path).Replace('\\', '/');
				string folderName = Path.GetFileName(path);
				AssetDatabase.CreateFolder(parentFolder, folderName);
			}
		}

		private static void ImportInterests()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "Interests.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogError($"Interests.json not found at {jsonPath}");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			InterestDataList data = JsonUtility.FromJson<InterestDataList>(json);

			foreach (var interestData in data.interests)
			{
				string assetPath = $"{ASSET_BASE_PATH}/Traits/Interests/{interestData.assetName}.asset";

				InterestSO asset = AssetDatabase.LoadAssetAtPath<InterestSO>(assetPath);
				if (asset == null)
				{
					asset = ScriptableObject.CreateInstance<InterestSO>();
					AssetDatabase.CreateAsset(asset, assetPath);
				}

				asset.displayName = interestData.displayName;
				asset.description = interestData.description;
				asset.category = ParseInterestCategory(interestData.category);
				asset.matchWeight = interestData.matchWeight;

				EditorUtility.SetDirty(asset);
				interests[interestData.assetName] = asset;
			}

			Debug.Log($"Imported {data.interests.Length} interests");
		}

		private static void ImportPersonalityTraits()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "PersonalityTraits.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogError($"PersonalityTraits.json not found at {jsonPath}");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			PersonalityTraitDataList data = JsonUtility.FromJson<PersonalityTraitDataList>(json);

			foreach (var traitData in data.personalityTraits)
			{
				string assetPath = $"{ASSET_BASE_PATH}/Traits/PersonalityTraits/{traitData.assetName}.asset";

				PersonalityTraitSO asset = AssetDatabase.LoadAssetAtPath<PersonalityTraitSO>(assetPath);
				if (asset == null)
				{
					asset = ScriptableObject.CreateInstance<PersonalityTraitSO>();
					AssetDatabase.CreateAsset(asset, assetPath);
				}

				asset.displayName = traitData.displayName;
				asset.description = traitData.description;
				asset.isPositiveTrait = traitData.isPositive;
				asset.matchWeight = traitData.matchWeight;

				EditorUtility.SetDirty(asset);
				personalityTraits[traitData.assetName] = asset;
			}

			Debug.Log($"Imported {data.personalityTraits.Length} personality traits");
		}

		private static void ImportLifestyleTraits()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "LifestyleTraits.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogError($"LifestyleTraits.json not found at {jsonPath}");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			LifestyleTraitDataList data = JsonUtility.FromJson<LifestyleTraitDataList>(json);

			foreach (var traitData in data.lifestyleTraits)
			{
				string assetPath = $"{ASSET_BASE_PATH}/Traits/LifestyleTraits/{traitData.assetName}.asset";

				LifestyleTraitSO asset = AssetDatabase.LoadAssetAtPath<LifestyleTraitSO>(assetPath);
				if (asset == null)
				{
					asset = ScriptableObject.CreateInstance<LifestyleTraitSO>();
					AssetDatabase.CreateAsset(asset, assetPath);
				}

				asset.displayName = traitData.displayName;
				asset.description = traitData.description;
				asset.category = ParseLifestyleCategory(traitData.category);
				asset.matchWeight = traitData.matchWeight;

				EditorUtility.SetDirty(asset);
				lifestyleTraits[traitData.assetName] = asset;
			}

			Debug.Log($"Imported {data.lifestyleTraits.Length} lifestyle traits");
		}

		private static void ResolveTraitReferences()
		{
			// Resolve Interest references
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "Interests.json");
			string json = File.ReadAllText(jsonPath);
			InterestDataList interestData = JsonUtility.FromJson<InterestDataList>(json);

			foreach (var data in interestData.interests)
			{
				if (interests.TryGetValue(data.assetName, out InterestSO asset))
				{
					List<InterestSO> related = new List<InterestSO>();
					foreach (string relatedName in data.relatedInterests)
					{
						if (interests.TryGetValue(relatedName, out InterestSO relatedAsset))
						{
							related.Add(relatedAsset);
						}
					}
					asset.relatedInterests = related.ToArray();
					EditorUtility.SetDirty(asset);
				}
			}

			// Resolve PersonalityTrait references
			jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "PersonalityTraits.json");
			json = File.ReadAllText(jsonPath);
			PersonalityTraitDataList personalityData = JsonUtility.FromJson<PersonalityTraitDataList>(json);

			foreach (var data in personalityData.personalityTraits)
			{
				if (personalityTraits.TryGetValue(data.assetName, out PersonalityTraitSO asset))
				{
					// Opposite traits (array)
					List<PersonalityTraitSO> opposites = new List<PersonalityTraitSO>();
					foreach (string oppositeName in data.oppositeTraits)
					{
						if (!string.IsNullOrEmpty(oppositeName) && personalityTraits.TryGetValue(oppositeName, out PersonalityTraitSO opposite))
						{
							opposites.Add(opposite);
						}
					}
					asset.oppositeTraits = opposites.ToArray();

					// Complementary traits
					List<PersonalityTraitSO> complementary = new List<PersonalityTraitSO>();
					foreach (string compName in data.complementaryTraits)
					{
						if (personalityTraits.TryGetValue(compName, out PersonalityTraitSO compAsset))
						{
							complementary.Add(compAsset);
						}
					}
					asset.complementaryTraits = complementary.ToArray();
					EditorUtility.SetDirty(asset);
				}
			}

			// Resolve LifestyleTrait references
			jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "LifestyleTraits.json");
			json = File.ReadAllText(jsonPath);
			LifestyleTraitDataList lifestyleData = JsonUtility.FromJson<LifestyleTraitDataList>(json);

			foreach (var data in lifestyleData.lifestyleTraits)
			{
				if (lifestyleTraits.TryGetValue(data.assetName, out LifestyleTraitSO asset))
				{
					// Conflicting traits
					List<LifestyleTraitSO> conflicting = new List<LifestyleTraitSO>();
					foreach (string conflictName in data.conflictingTraits)
					{
						if (lifestyleTraits.TryGetValue(conflictName, out LifestyleTraitSO conflictAsset))
						{
							conflicting.Add(conflictAsset);
						}
					}
					asset.conflictingTraits = conflicting.ToArray();

					// Compatible traits
					List<LifestyleTraitSO> compatible = new List<LifestyleTraitSO>();
					foreach (string compatName in data.compatibleTraits)
					{
						if (lifestyleTraits.TryGetValue(compatName, out LifestyleTraitSO compatAsset))
						{
							compatible.Add(compatAsset);
						}
					}
					asset.compatibleTraits = compatible.ToArray();
					EditorUtility.SetDirty(asset);
				}
			}
		}

		private static void ImportCandidates()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "Candidates.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogError($"Candidates.json not found at {jsonPath}");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			CandidateDataList data = JsonUtility.FromJson<CandidateDataList>(json);

			foreach (var candidateData in data.candidates)
			{
				string assetPath = $"{ASSET_BASE_PATH}/Profiles/{candidateData.assetName}.asset";

				CandidateProfileSO asset = AssetDatabase.LoadAssetAtPath<CandidateProfileSO>(assetPath);
				if (asset == null)
				{
					asset = ScriptableObject.CreateInstance<CandidateProfileSO>();
					AssetDatabase.CreateAsset(asset, assetPath);
				}

				// Basic profile data
				asset.profile.characterName = candidateData.characterName;
				asset.profile.gender = ParseGender(candidateData.gender);
				asset.profile.age = candidateData.age;
				asset.profile.bio = candidateData.bio;
				asset.profile.archetype = ParseArchetype(candidateData.archetype);

				// Resolve personality traits
				List<PersonalityTraitSO> personalities = new List<PersonalityTraitSO>();
				foreach (string traitName in candidateData.personalityTraits)
				{
					if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
					{
						personalities.Add(trait);
					}
				}
				asset.profile.personalityTraits = personalities.ToArray();

				// Resolve interests
				List<InterestSO> interestsList = new List<InterestSO>();
				foreach (string interestName in candidateData.interests)
				{
					if (interests.TryGetValue(interestName, out InterestSO interest))
					{
						interestsList.Add(interest);
					}
				}
				asset.profile.interests = interestsList.ToArray();

				// Resolve lifestyle traits
				List<LifestyleTraitSO> lifestyles = new List<LifestyleTraitSO>();
				foreach (string lifestyleName in candidateData.lifestyleTraits)
				{
					if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
					{
						lifestyles.Add(lifestyle);
					}
				}
				asset.profile.lifestyleTraits = lifestyles.ToArray();

				// Random post settings
				asset.randomPostCount = candidateData.randomPostCount;
				asset.randomPostTagFilter = candidateData.randomPostTagFilter;

				// Create posts
				asset.guaranteedPosts.Clear();
				foreach (var postData in candidateData.guaranteedPosts)
				{
					SocialMediaPost post = new SocialMediaPost();
					post.postType = ParsePostType(postData.postType);
					post.content = postData.content;
					post.daysSincePosted = postData.daysSincePosted;
					post.likes = postData.likes;
					post.comments = postData.comments;
					post.isGreenFlag = postData.isGreenFlag;
					post.isRedFlag = postData.isRedFlag;

					// Resolve post trait references
					List<InterestSO> postInterests = new List<InterestSO>();
					foreach (string interestName in postData.relatedInterests)
					{
						if (interests.TryGetValue(interestName, out InterestSO interest))
						{
							postInterests.Add(interest);
						}
					}
					post.relatedInterests = postInterests.ToArray();

					List<PersonalityTraitSO> postPersonalities = new List<PersonalityTraitSO>();
					foreach (string traitName in postData.relatedPersonalityTraits)
					{
						if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
						{
							postPersonalities.Add(trait);
						}
					}
					post.relatedPersonalityTraits = postPersonalities.ToArray();

					List<LifestyleTraitSO> postLifestyles = new List<LifestyleTraitSO>();
					foreach (string lifestyleName in postData.relatedLifestyleTraits)
					{
						if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
						{
							postLifestyles.Add(lifestyle);
						}
					}
					post.relatedLifestyleTraits = postLifestyles.ToArray();

					asset.guaranteedPosts.Add(post);
				}

				EditorUtility.SetDirty(asset);
			}

			Debug.Log($"Imported {data.candidates.Length} candidate profiles");
		}

		// ===== ENUM PARSING HELPERS =====

		private static InterestCategory ParseInterestCategory(string category)
		{
			if (System.Enum.TryParse(category, true, out InterestCategory result))
			{
				return result;
			}
			Debug.LogWarning($"Unknown InterestCategory '{category}', defaulting to Other");
			return InterestCategory.Other;
		}

		private static LifestyleCategory ParseLifestyleCategory(string category)
		{
			if (System.Enum.TryParse(category, true, out LifestyleCategory result))
			{
				return result;
			}
			Debug.LogWarning($"Unknown LifestyleCategory '{category}', defaulting to Other");
			return LifestyleCategory.Other;
		}

		private static Gender ParseGender(string gender)
		{
			// Handle different string formats
			switch (gender.ToLower())
			{
				case "male": return Gender.Male;
				case "female": return Gender.Female;
				case "non-binary":
				case "nonbinary":
				case "non binary": return Gender.NonBinary;
				default:
					Debug.LogWarning($"Unknown Gender '{gender}', defaulting to NonBinary");
					return Gender.NonBinary;
			}
		}

		private static PersonalityArchetype ParseArchetype(string archetype)
		{
			if (System.Enum.TryParse(archetype, true, out PersonalityArchetype result))
			{
				return result;
			}
			Debug.LogWarning($"Unknown PersonalityArchetype '{archetype}', defaulting to Quirky");
			return PersonalityArchetype.Quirky;
		}

		private static PostType ParsePostType(string typeString)
		{
			switch (typeString)
			{
				case "Photo": return PostType.Photo;
				case "TextOnly": return PostType.TextOnly;
				case "Video": return PostType.Video;
				case "Story": return PostType.Story;
				case "SharedPost": return PostType.SharedPost;
				case "Poll": return PostType.Poll;
				default: return PostType.Photo;
			}
		}
	}

	// ===== DATA CLASSES FOR JSON DESERIALIZATION =====

	[System.Serializable]
	public class InterestDataList
	{
		public InterestData[] interests;
	}

	[System.Serializable]
	public class InterestData
	{
		public string assetName;
		public string displayName;
		public string description;
		public string category;
		public int matchWeight;
		public string[] relatedInterests;
	}

	[System.Serializable]
	public class PersonalityTraitDataList
	{
		public PersonalityTraitData[] personalityTraits;
	}

	[System.Serializable]
	public class PersonalityTraitData
	{
		public string assetName;
		public string displayName;
		public string description;
		public bool isPositive;
		public int matchWeight;
		public string[] oppositeTraits;
		public string[] complementaryTraits;
	}

	[System.Serializable]
	public class LifestyleTraitDataList
	{
		public LifestyleTraitData[] lifestyleTraits;
	}

	[System.Serializable]
	public class LifestyleTraitData
	{
		public string assetName;
		public string displayName;
		public string description;
		public string category;
		public int matchWeight;
		public string[] conflictingTraits;
		public string[] compatibleTraits;
	}

	[System.Serializable]
	public class CandidateDataList
	{
		public CandidateData[] candidates;
	}

	[System.Serializable]
	public class CandidateData
	{
		public string assetName;
		public string characterName;
		public string gender;
		public int age;
		public string bio;
		public string archetype;
		public string[] personalityTraits;
		public string[] interests;
		public string[] lifestyleTraits;
		public int randomPostCount;
		public string[] randomPostTagFilter;
		public PostData[] guaranteedPosts;
	}

	[System.Serializable]
	public class PostData
	{
		public string postType;
		public string content;
		public int daysSincePosted;
		public int likes;
		public int comments;
		public bool isGreenFlag;
		public bool isRedFlag;
		public string[] relatedInterests;
		public string[] relatedPersonalityTraits;
		public string[] relatedLifestyleTraits;
	}
}
