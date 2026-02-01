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
		private const string ASSET_BASE_PATH = "Assets/Resources/GameData";

		// Dictionaries to store created assets for reference resolution
		private static Dictionary<string, InterestSO> interests = new Dictionary<string, InterestSO>();
		private static Dictionary<string, PersonalityTraitSO> personalityTraits = new Dictionary<string, PersonalityTraitSO>();
		private static Dictionary<string, LifestyleTraitSO> lifestyleTraits = new Dictionary<string, LifestyleTraitSO>();
		private static Dictionary<string, NarrativeHintCollectionSO> narrativeHints = new Dictionary<string, NarrativeHintCollectionSO>();
		private static int randomPostCount = 0;
		private static int candidateCount = 0;
		private static int clientCount = 0;

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
				// Clear dictionaries and counters
				interests.Clear();
				personalityTraits.Clear();
				lifestyleTraits.Clear();
				narrativeHints.Clear();
				randomPostCount = 0;
				candidateCount = 0;
				clientCount = 0;

				// Create folder structure
				CreateFolderStructure();

				// Import in order: traits first, then profiles (which reference traits)
				ImportInterests();
				ImportPersonalityTraits();
				ImportLifestyleTraits();

				// Resolve trait references (for traits that reference other traits)
				ResolveTraitReferences();

				// Import narrative hints (after traits, before clients which reference them)
				ImportNarrativeHints();

				// Import random post pool (before candidates, so we can reference it)
				ImportRandomPosts();

				// Import candidate profiles (which reference all the traits)
				ImportCandidates();

				// Import client profiles (which reference traits and narrative hints)
				ImportClients();

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				EditorUtility.DisplayDialog("Import Complete",
					$"Successfully imported:\n• {interests.Count} Interests\n• {personalityTraits.Count} Personality Traits\n• {lifestyleTraits.Count} Lifestyle Traits\n• {narrativeHints.Count} Narrative Hint Collections\n• {randomPostCount} Random Posts\n• {candidateCount} Candidates\n• {clientCount} Clients",
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
			CreateFolderIfNeeded("Assets/Resources");
			CreateFolderIfNeeded("Assets/Resources/GameData");
			CreateFolderIfNeeded("Assets/Resources/GameData/Traits");
			CreateFolderIfNeeded("Assets/Resources/GameData/Traits/Interests");
			CreateFolderIfNeeded("Assets/Resources/GameData/Traits/Personality");
			CreateFolderIfNeeded("Assets/Resources/GameData/Traits/Lifestyle");
			CreateFolderIfNeeded("Assets/Resources/GameData/NarrativeHints");
			CreateFolderIfNeeded("Assets/Resources/GameData/Profiles");
			CreateFolderIfNeeded("Assets/Resources/GameData/Clients");
			CreateFolderIfNeeded("Assets/Resources/GameData/PostPool");
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
				string assetPath = $"{ASSET_BASE_PATH}/Traits/Personality/{traitData.assetName}.asset";

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
				string assetPath = $"{ASSET_BASE_PATH}/Traits/Lifestyle/{traitData.assetName}.asset";

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

		private static void ImportNarrativeHints()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "NarrativeHints.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogWarning($"NarrativeHints.json not found at {jsonPath} - skipping narrative hints import");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			NarrativeHintDataList data = JsonUtility.FromJson<NarrativeHintDataList>(json);

			foreach (var hintData in data.narrativeHints)
			{
				string assetPath = $"{ASSET_BASE_PATH}/NarrativeHints/{hintData.assetName}.asset";

				NarrativeHintCollectionSO asset = AssetDatabase.LoadAssetAtPath<NarrativeHintCollectionSO>(assetPath);
				if (asset == null)
				{
					asset = ScriptableObject.CreateInstance<NarrativeHintCollectionSO>();
					AssetDatabase.CreateAsset(asset, assetPath);
				}

				// Resolve related interests
				List<InterestSO> relatedInterestsList = new List<InterestSO>();
				if (hintData.relatedInterests != null)
				{
					foreach (string interestName in hintData.relatedInterests)
					{
						if (interests.TryGetValue(interestName, out InterestSO interest))
						{
							relatedInterestsList.Add(interest);
						}
					}
				}
				asset.relatedInterests = relatedInterestsList.ToArray();

				// Resolve related personality traits
				List<PersonalityTraitSO> relatedPersonalitiesList = new List<PersonalityTraitSO>();
				if (hintData.relatedPersonalityTraits != null)
				{
					foreach (string traitName in hintData.relatedPersonalityTraits)
					{
						if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
						{
							relatedPersonalitiesList.Add(trait);
						}
					}
				}
				asset.relatedPersonalityTraits = relatedPersonalitiesList.ToArray();

				// Resolve related lifestyle traits
				List<LifestyleTraitSO> relatedLifestylesList = new List<LifestyleTraitSO>();
				if (hintData.relatedLifestyleTraits != null)
				{
					foreach (string lifestyleName in hintData.relatedLifestyleTraits)
					{
						if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
						{
							relatedLifestylesList.Add(lifestyle);
						}
					}
				}
				asset.relatedLifestyleTraits = relatedLifestylesList.ToArray();

				// Set hints
				asset.hints = hintData.hints ?? new string[0];

				EditorUtility.SetDirty(asset);
				narrativeHints[hintData.assetName] = asset;
			}

			Debug.Log($"Imported {data.narrativeHints.Length} narrative hint collections");
		}

		private static void ImportRandomPosts()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "RandomPosts.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogWarning($"RandomPosts.json not found at {jsonPath} - skipping random post pool import");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			RandomPostDataList data = JsonUtility.FromJson<RandomPostDataList>(json);

			string assetPath = $"{ASSET_BASE_PATH}/PostPool/RandomPostPool.asset";

			RandomPostPoolSO asset = AssetDatabase.LoadAssetAtPath<RandomPostPoolSO>(assetPath);
			if (asset == null)
			{
				asset = ScriptableObject.CreateInstance<RandomPostPoolSO>();
				AssetDatabase.CreateAsset(asset, assetPath);
			}

			asset.posts.Clear();

			foreach (var postData in data.posts)
			{
				SocialMediaPost post = new SocialMediaPost();
				post.postType = ParsePostType(postData.postType);
				post.content = postData.content;
				post.isGreenFlag = postData.isGreenFlag;
				post.isRedFlag = postData.isRedFlag;

				// Resolve post trait references
				List<InterestSO> postInterests = new List<InterestSO>();
				if (postData.relatedInterests != null)
				{
					foreach (string interestName in postData.relatedInterests)
					{
						if (interests.TryGetValue(interestName, out InterestSO interest))
						{
							postInterests.Add(interest);
						}
					}
				}
				post.relatedInterests = postInterests.ToArray();

				List<PersonalityTraitSO> postPersonalities = new List<PersonalityTraitSO>();
				if (postData.relatedPersonalityTraits != null)
				{
					foreach (string traitName in postData.relatedPersonalityTraits)
					{
						if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
						{
							postPersonalities.Add(trait);
						}
					}
				}
				post.relatedPersonalityTraits = postPersonalities.ToArray();

				List<LifestyleTraitSO> postLifestyles = new List<LifestyleTraitSO>();
				if (postData.relatedLifestyleTraits != null)
				{
					foreach (string lifestyleName in postData.relatedLifestyleTraits)
					{
						if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
						{
							postLifestyles.Add(lifestyle);
						}
					}
				}
				post.relatedLifestyleTraits = postLifestyles.ToArray();

				asset.posts.Add(post);
			}

			EditorUtility.SetDirty(asset);
			randomPostCount = asset.posts.Count;
			Debug.Log($"Imported {asset.posts.Count} random posts to pool");
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
				if (candidateData.randomPostRange != null && candidateData.randomPostRange.Length >= 2)
				{
					asset.randomPostMin = candidateData.randomPostRange[0];
					asset.randomPostMax = candidateData.randomPostRange[1];
				}

				// Friends count settings (affects engagement)
				if (candidateData.friendsCountRange != null && candidateData.friendsCountRange.Length >= 2)
				{
					asset.friendsCountMin = candidateData.friendsCountRange[0];
					asset.friendsCountMax = candidateData.friendsCountRange[1];
				}

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

			candidateCount = data.candidates.Length;
			Debug.Log($"Imported {data.candidates.Length} candidate profiles");
		}

		private static void ImportClients()
		{
			string jsonPath = Path.Combine(Application.dataPath, "..", JSON_DATA_PATH, "Clients.json");
			if (!File.Exists(jsonPath))
			{
				Debug.LogWarning($"Clients.json not found at {jsonPath} - skipping client import");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			ClientDataList data = JsonUtility.FromJson<ClientDataList>(json);

			foreach (var clientData in data.clients)
			{
				string assetPath = $"{ASSET_BASE_PATH}/Clients/{clientData.assetName}.asset";

				ClientProfileSO asset = AssetDatabase.LoadAssetAtPath<ClientProfileSO>(assetPath);
				if (asset == null)
				{
					asset = ScriptableObject.CreateInstance<ClientProfileSO>();
					AssetDatabase.CreateAsset(asset, assetPath);
				}

				// Story metadata
				asset.isStoryClient = clientData.isStoryClient;
				asset.suggestedLevel = clientData.suggestedLevel;

				// Basic profile data
				asset.profile.clientName = clientData.profile.clientName;
				asset.profile.gender = ParseGender(clientData.profile.gender);
				asset.profile.age = clientData.profile.age;
				asset.profile.relationship = clientData.profile.relationship;
				asset.profile.backstory = clientData.profile.backstory;
				asset.profile.archetype = ParseArchetype(clientData.profile.archetype);

				// Resolve personality traits
				List<PersonalityTraitSO> personalities = new List<PersonalityTraitSO>();
				if (clientData.profile.personalityTraits != null)
				{
					foreach (string traitName in clientData.profile.personalityTraits)
					{
						if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
						{
							personalities.Add(trait);
						}
					}
				}
				asset.profile.personalityTraits = personalities.ToArray();

				// Resolve interests
				List<InterestSO> interestsList = new List<InterestSO>();
				if (clientData.profile.interests != null)
				{
					foreach (string interestName in clientData.profile.interests)
					{
						if (interests.TryGetValue(interestName, out InterestSO interest))
						{
							interestsList.Add(interest);
						}
					}
				}
				asset.profile.interests = interestsList.ToArray();

				// Resolve lifestyle traits
				List<LifestyleTraitSO> lifestyles = new List<LifestyleTraitSO>();
				if (clientData.profile.lifestyleTraits != null)
				{
					foreach (string lifestyleName in clientData.profile.lifestyleTraits)
					{
						if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
						{
							lifestyles.Add(lifestyle);
						}
					}
				}
				asset.profile.lifestyleTraits = lifestyles.ToArray();

				// Introduction text
				asset.introduction = clientData.introduction ?? "";

				// Match criteria
				if (clientData.matchCriteria != null)
				{
					var mc = clientData.matchCriteria;

					// Initialize matchCriteria if null
					if (asset.matchCriteria == null)
					{
						asset.matchCriteria = new MatchCriteria();
					}

					// Acceptable genders
					List<Gender> genders = new List<Gender>();
					if (mc.acceptableGenders != null)
					{
						foreach (string genderStr in mc.acceptableGenders)
						{
							genders.Add(ParseGender(genderStr));
						}
					}
					asset.matchCriteria.acceptableGenders = genders.ToArray();

					// Age range
					asset.matchCriteria.minAge = mc.minAge;
					asset.matchCriteria.maxAge = mc.maxAge;

					// Minimum required traits to meet (0 = all must be met)
					asset.matchCriteria.minRequiredMet = mc.minRequiredMet;

					// Red flag tolerance
					asset.matchCriteria.maxRedFlags = mc.maxRedFlags;
					asset.matchCriteria.minGreenFlags = mc.minGreenFlags;

					// Scoring weights
					asset.matchCriteria.personalityWeight = mc.personalityWeight;
					asset.matchCriteria.interestsWeight = mc.interestsWeight;
					asset.matchCriteria.lifestyleWeight = mc.lifestyleWeight;

					// Dealbreaker personality traits
					List<PersonalityTraitSO> dealbreakerPersonalities = new List<PersonalityTraitSO>();
					if (mc.dealbreakerPersonalityTraits != null)
					{
						foreach (string traitName in mc.dealbreakerPersonalityTraits)
						{
							if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
							{
								dealbreakerPersonalities.Add(trait);
							}
						}
					}
					asset.matchCriteria.dealbreakerPersonalityTraits = dealbreakerPersonalities.ToArray();

					// Dealbreaker interests
					List<InterestSO> dealbreakerInterestsList = new List<InterestSO>();
					if (mc.dealbreakerInterests != null)
					{
						foreach (string interestName in mc.dealbreakerInterests)
						{
							if (interests.TryGetValue(interestName, out InterestSO interest))
							{
								dealbreakerInterestsList.Add(interest);
							}
						}
					}
					asset.matchCriteria.dealbreakerInterests = dealbreakerInterestsList.ToArray();

					// Dealbreaker lifestyle traits
					List<LifestyleTraitSO> dealbreakerLifestylesList = new List<LifestyleTraitSO>();
					if (mc.dealbreakerLifestyleTraits != null)
					{
						foreach (string lifestyleName in mc.dealbreakerLifestyleTraits)
						{
							if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
							{
								dealbreakerLifestylesList.Add(lifestyle);
							}
						}
					}
					asset.matchCriteria.dealbreakerLifestyleTraits = dealbreakerLifestylesList.ToArray();

					// Trait requirements
					List<TraitRequirement> requirements = new List<TraitRequirement>();
					if (mc.traitRequirements != null)
					{
						foreach (var reqData in mc.traitRequirements)
						{
							TraitRequirement req = new TraitRequirement();

							// Narrative hints
							if (!string.IsNullOrEmpty(reqData.narrativeHints) &&
								narrativeHints.TryGetValue(reqData.narrativeHints, out NarrativeHintCollectionSO hintCollection))
							{
								req.narrativeHints = hintCollection;
							}

							// Acceptable interests
							List<InterestSO> acceptableInterestsList = new List<InterestSO>();
							if (reqData.acceptableInterests != null)
							{
								foreach (string interestName in reqData.acceptableInterests)
								{
									if (interests.TryGetValue(interestName, out InterestSO interest))
									{
										acceptableInterestsList.Add(interest);
									}
								}
							}
							req.acceptableInterests = acceptableInterestsList.ToArray();

							// Acceptable personality traits
							List<PersonalityTraitSO> acceptablePersonalitiesList = new List<PersonalityTraitSO>();
							if (reqData.acceptablePersonalityTraits != null)
							{
								foreach (string traitName in reqData.acceptablePersonalityTraits)
								{
									if (personalityTraits.TryGetValue(traitName, out PersonalityTraitSO trait))
									{
										acceptablePersonalitiesList.Add(trait);
									}
								}
							}
							req.acceptablePersonalityTraits = acceptablePersonalitiesList.ToArray();

							// Acceptable lifestyle traits
							List<LifestyleTraitSO> acceptableLifestylesList = new List<LifestyleTraitSO>();
							if (reqData.acceptableLifestyleTraits != null)
							{
								foreach (string lifestyleName in reqData.acceptableLifestyleTraits)
								{
									if (lifestyleTraits.TryGetValue(lifestyleName, out LifestyleTraitSO lifestyle))
									{
										acceptableLifestylesList.Add(lifestyle);
									}
								}
							}
							req.acceptableLifestyleTraits = acceptableLifestylesList.ToArray();

							// Requirement level
							req.level = ParseRequirementLevel(reqData.level ?? "Preferred");

							requirements.Add(req);
						}
					}
					asset.matchCriteria.traitRequirements = requirements.ToArray();
				}

				EditorUtility.SetDirty(asset);
			}

			clientCount = data.clients.Length;
			Debug.Log($"Imported {data.clients.Length} client profiles");
		}

		private static RequirementLevel ParseRequirementLevel(string level)
		{
			if (System.Enum.TryParse(level, true, out RequirementLevel result))
			{
				return result;
			}
			Debug.LogWarning($"Unknown RequirementLevel '{level}', defaulting to Preferred");
			return RequirementLevel.Preferred;
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
		public int[] randomPostRange;
		public int[] friendsCountRange;
		public PostData[] guaranteedPosts;
	}

	[System.Serializable]
	public class RandomPostDataList
	{
		public PostData[] posts;
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

	[System.Serializable]
	public class NarrativeHintDataList
	{
		public NarrativeHintData[] narrativeHints;
	}

	[System.Serializable]
	public class NarrativeHintData
	{
		public string assetName;
		public string[] relatedInterests;
		public string[] relatedPersonalityTraits;
		public string[] relatedLifestyleTraits;
		public string[] hints;
	}

	[System.Serializable]
	public class ClientDataList
	{
		public ClientData[] clients;
	}

	[System.Serializable]
	public class ClientData
	{
		public string assetName;
		public bool isStoryClient;
		public int suggestedLevel;
		public ClientProfileData profile;
		public MatchCriteriaData matchCriteria;
		public string introduction;
	}

	[System.Serializable]
	public class ClientProfileData
	{
		public string clientName;
		public string gender;
		public int age;
		public string relationship;
		public string backstory;
		public string[] personalityTraits;
		public string[] interests;
		public string[] lifestyleTraits;
		public string archetype;
	}

	[System.Serializable]
	public class MatchCriteriaData
	{
		public string[] acceptableGenders;
		public int minAge;
		public int maxAge;
		public TraitRequirementData[] traitRequirements;
		public int minRequiredMet;
		public string[] dealbreakerPersonalityTraits;
		public string[] dealbreakerInterests;
		public string[] dealbreakerLifestyleTraits;
		public int maxRedFlags;
		public int minGreenFlags;
		public float personalityWeight;
		public float interestsWeight;
		public float lifestyleWeight;
	}

	[System.Serializable]
	public class TraitRequirementData
	{
		public string narrativeHints;
		public string[] acceptableInterests;
		public string[] acceptablePersonalityTraits;
		public string[] acceptableLifestyleTraits;
		public string level;
	}
}
