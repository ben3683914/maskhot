using UnityEngine;
using Maskhot.Data;
using System.Text;

namespace Maskhot
{
	/// <summary>
	/// Simple test script to verify CandidateProfileSO data is set up correctly
	/// Attach to a GameObject and drag in a profile to test
	/// </summary>
	public class ProfileTester : MonoBehaviour
	{
		[Header("Test Data")]
		[Tooltip("Drag CandidateProfileSO assets here to test (tests all in the array)")]
		public CandidateProfileSO[] testProfiles;

		[Header("Settings")]
		[Tooltip("Run test on Start?")]
		public bool testOnStart = true;

		void Start()
		{
			if (testOnStart && testProfiles != null && testProfiles.Length > 0)
			{
				TestAllProfiles();
			}
		}

		[ContextMenu("Test All Profiles")]
		public void TestAllProfiles()
		{
			if (testProfiles == null || testProfiles.Length == 0)
			{
				Debug.LogError("No test profiles assigned!");
				return;
			}

			Debug.Log($"=== TESTING {testProfiles.Length} CANDIDATE PROFILES ===");

			for (int profileIndex = 0; profileIndex < testProfiles.Length; profileIndex++)
			{
				var testProfile = testProfiles[profileIndex];
				if (testProfile == null)
				{
					Debug.LogWarning($"[Profile {profileIndex + 1}] NULL - Skipped");
					continue;
				}

				var sb = new StringBuilder();
				TestProfile(testProfile, profileIndex + 1, sb);

				// Output each profile as a separate log
				Debug.Log(sb.ToString());
			}

			Debug.Log("=== ALL TESTS COMPLETE ===");
		}

		private void TestProfile(CandidateProfileSO testProfile, int profileNumber, StringBuilder sb)
		{

			sb.AppendLine($"=== PROFILE #{profileNumber}: {testProfile.name} ===");
			sb.AppendLine();

			// Basic Info
			sb.AppendLine("--- BASIC INFO ---");
			sb.AppendLine($"Name: {testProfile.profile.characterName}");
			sb.AppendLine($"Gender: {testProfile.profile.gender}");
			sb.AppendLine($"Age: {testProfile.profile.age}");
			sb.AppendLine($"Bio: {testProfile.profile.bio}");
			sb.AppendLine($"Archetype: {testProfile.profile.archetype}");
			sb.AppendLine();

			// Personality Traits
			sb.AppendLine("--- PERSONALITY TRAITS ---");
			if (testProfile.profile.personalityTraits != null && testProfile.profile.personalityTraits.Length > 0)
			{
				foreach (var trait in testProfile.profile.personalityTraits)
				{
					if (trait != null)
					{
						sb.AppendLine($"  - {trait.displayName} (Weight: {trait.matchWeight})");
					}
				}
			}
			else
			{
				sb.AppendLine("  (None)");
			}
			sb.AppendLine();

			// Interests
			sb.AppendLine("--- INTERESTS ---");
			if (testProfile.profile.interests != null && testProfile.profile.interests.Length > 0)
			{
				foreach (var interest in testProfile.profile.interests)
				{
					if (interest != null)
					{
						sb.AppendLine($"  - {interest.displayName} ({interest.category}) [Weight: {interest.matchWeight}]");
					}
				}
			}
			else
			{
				sb.AppendLine("  (None)");
			}
			sb.AppendLine();

			// Lifestyle Traits
			sb.AppendLine("--- LIFESTYLE TRAITS ---");
			if (testProfile.profile.lifestyleTraits != null && testProfile.profile.lifestyleTraits.Length > 0)
			{
				foreach (var lifestyle in testProfile.profile.lifestyleTraits)
				{
					if (lifestyle != null)
					{
						sb.AppendLine($"  - {lifestyle.displayName} ({lifestyle.category}) [Weight: {lifestyle.matchWeight}]");
					}
				}
			}
			else
			{
				sb.AppendLine("  (None)");
			}
			sb.AppendLine();

			// Guaranteed Posts
			sb.AppendLine("--- GUARANTEED POSTS ---");
			sb.AppendLine($"Total Posts: {testProfile.guaranteedPosts.Count}");
			for (int i = 0; i < testProfile.guaranteedPosts.Count; i++)
			{
				var post = testProfile.guaranteedPosts[i];
				sb.AppendLine();
				sb.AppendLine($"Post {i + 1}:");
				sb.AppendLine($"  Type: {post.postType}");
				sb.AppendLine($"  Content: {post.content}");
				sb.AppendLine($"  Days Since Posted: {post.daysSincePosted}");
				sb.AppendLine($"  Engagement: {post.likes} likes, {post.comments} comments");
				sb.AppendLine($"  Flags: {(post.isGreenFlag ? "Green Flag" : "")} {(post.isRedFlag ? "Red Flag" : "")}");

				// Related traits
				if (post.relatedInterests != null && post.relatedInterests.Length > 0)
				{
					sb.AppendLine($"  Related Interests: {string.Join(", ", System.Array.ConvertAll(post.relatedInterests, i => i != null ? i.displayName : "null"))}");
				}
				if (post.relatedPersonalityTraits != null && post.relatedPersonalityTraits.Length > 0)
				{
					sb.AppendLine($"  Related Personality: {string.Join(", ", System.Array.ConvertAll(post.relatedPersonalityTraits, p => p != null ? p.displayName : "null"))}");
				}
				if (post.relatedLifestyleTraits != null && post.relatedLifestyleTraits.Length > 0)
				{
					sb.AppendLine($"  Related Lifestyle: {string.Join(", ", System.Array.ConvertAll(post.relatedLifestyleTraits, l => l != null ? l.displayName : "null"))}");
				}
			}
		}
	}
}
