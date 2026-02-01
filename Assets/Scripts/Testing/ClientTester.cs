using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Maskhot.Data;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify client profiles and match criteria are working correctly.
    /// Use the Context Menu to run tests on client profiles.
    /// </summary>
    public class ClientTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Specific client to test (leave empty to test all)")]
        public ClientProfileSO specificClient;

        [Tooltip("Enable detailed logging for match criteria")]
        public bool verboseMatchCriteria = true;

        [Header("Quick Test Buttons")]
        [Tooltip("Drag clients here for batch testing")]
        public ClientProfileSO[] clientsToTest;

        /// <summary>
        /// Tests a specific client's profile and match criteria
        /// </summary>
        [ContextMenu("Test Specific Client")]
        public void TestSpecificClient()
        {
            if (specificClient == null)
            {
                Debug.LogError("ClientTester: No specific client assigned!");
                return;
            }

            LogClientTest(specificClient);
        }

        /// <summary>
        /// Tests all clients in the clientsToTest array
        /// </summary>
        [ContextMenu("Test All Assigned Clients")]
        public void TestAllAssignedClients()
        {
            if (clientsToTest == null || clientsToTest.Length == 0)
            {
                Debug.LogError("ClientTester: No clients assigned to test array!");
                return;
            }

            Debug.Log($"═══ CLIENT TESTER - {clientsToTest.Length} CLIENTS ═══");

            foreach (var client in clientsToTest)
            {
                if (client != null)
                {
                    LogClientTest(client);
                }
            }

            Debug.Log($"═══ COMPLETE: {clientsToTest.Length} clients tested ═══");
        }

        /// <summary>
        /// Tests all clients from Resources folder
        /// </summary>
        [ContextMenu("Test All Clients (from Resources)")]
        public void TestAllClientsFromResources()
        {
            ClientProfileSO[] allClients = Resources.LoadAll<ClientProfileSO>("GameData/Clients");

            if (allClients == null || allClients.Length == 0)
            {
                Debug.LogError("ClientTester: No clients found in Resources/GameData/Clients!");
                return;
            }

            Debug.Log($"═══ CLIENT TESTER - ALL {allClients.Length} CLIENTS (from Resources) ═══");

            foreach (var client in allClients)
            {
                LogClientTest(client);
            }

            Debug.Log($"═══ COMPLETE: {allClients.Length} clients tested ═══");
        }

        private void LogClientTest(ClientProfileSO client)
        {
            StringBuilder sb = new StringBuilder();
            BuildClientTestLog(client, sb);
            Debug.Log(sb.ToString());
        }

        private void BuildClientTestLog(ClientProfileSO client, StringBuilder sb)
        {
            var profile = client.profile;

            sb.AppendLine($"┌─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ {profile.clientName}");
            sb.AppendLine($"├─────────────────────────────────────────────────────────────");

            // Basic info
            sb.AppendLine($"│ Age: {profile.age} | Gender: {profile.gender} | Archetype: {profile.archetype}");
            sb.AppendLine($"│ Relationship: {profile.relationship}");
            sb.AppendLine($"│ Story Client: {client.isStoryClient} | Level: {client.suggestedLevel}");

            // Backstory (truncated)
            if (!string.IsNullOrEmpty(profile.backstory))
            {
                string backstory = profile.backstory.Length > 80
                    ? profile.backstory.Substring(0, 77) + "..."
                    : profile.backstory;
                sb.AppendLine($"│ Backstory: {backstory}");
            }

            // Client's own traits
            if (profile.personalityTraits != null && profile.personalityTraits.Length > 0)
            {
                string traits = string.Join(", ", System.Array.ConvertAll(profile.personalityTraits, t => t?.displayName ?? "?"));
                sb.AppendLine($"│ Personality: {traits}");
            }

            if (profile.interests != null && profile.interests.Length > 0)
            {
                string interests = string.Join(", ", System.Array.ConvertAll(profile.interests, i => i?.displayName ?? "?"));
                sb.AppendLine($"│ Interests: {interests}");
            }

            if (profile.lifestyleTraits != null && profile.lifestyleTraits.Length > 0)
            {
                string lifestyle = string.Join(", ", System.Array.ConvertAll(profile.lifestyleTraits, l => l?.displayName ?? "?"));
                sb.AppendLine($"│ Lifestyle: {lifestyle}");
            }

            // Introduction (truncated)
            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            if (!string.IsNullOrEmpty(client.introduction))
            {
                string intro = client.introduction.Length > 100
                    ? client.introduction.Substring(0, 97) + "..."
                    : client.introduction;
                sb.AppendLine($"│ INTRODUCTION: \"{intro}\"");
            }
            else
            {
                sb.AppendLine($"│ INTRODUCTION: (none)");
            }

            // Match Criteria
            if (client.matchCriteria != null)
            {
                var mc = client.matchCriteria;
                sb.AppendLine($"├─────────────────────────────────────────────────────────────");
                sb.AppendLine($"│ MATCH CRITERIA:");

                // Gender and age preferences
                if (mc.acceptableGenders != null && mc.acceptableGenders.Length > 0)
                {
                    string genders = string.Join(", ", mc.acceptableGenders);
                    sb.AppendLine($"│   Genders: {genders}");
                }
                sb.AppendLine($"│   Age Range: {mc.minAge}-{mc.maxAge}");

                // Flag tolerance
                sb.AppendLine($"│   Red Flags: max {mc.maxRedFlags} | Green Flags: min {mc.minGreenFlags}");

                // Scoring weights
                sb.AppendLine($"│   Weights: Personality {mc.personalityWeight:P0}, Interests {mc.interestsWeight:P0}, Lifestyle {mc.lifestyleWeight:P0}");

                if (verboseMatchCriteria)
                {
                    // Trait requirements
                    if (mc.traitRequirements != null && mc.traitRequirements.Length > 0)
                    {
                        sb.AppendLine($"│");
                        sb.AppendLine($"│   REQUIREMENTS ({mc.traitRequirements.Length}):");
                        foreach (var req in mc.traitRequirements)
                        {
                            string level = req.level.ToString().ToUpper();
                            string hint = req.narrativeHints != null ? req.narrativeHints.GetRandomHint() : "(no hint)";
                            sb.AppendLine($"│     [{level}] \"{hint}\"");

                            // Show what traits satisfy this requirement
                            List<string> acceptableTraits = new List<string>();
                            if (req.acceptableInterests != null)
                            {
                                foreach (var interest in req.acceptableInterests)
                                {
                                    if (interest != null) acceptableTraits.Add(interest.displayName);
                                }
                            }
                            if (req.acceptablePersonalityTraits != null)
                            {
                                foreach (var trait in req.acceptablePersonalityTraits)
                                {
                                    if (trait != null) acceptableTraits.Add(trait.displayName);
                                }
                            }
                            if (req.acceptableLifestyleTraits != null)
                            {
                                foreach (var trait in req.acceptableLifestyleTraits)
                                {
                                    if (trait != null) acceptableTraits.Add(trait.displayName);
                                }
                            }
                            if (acceptableTraits.Count > 0)
                            {
                                sb.AppendLine($"│       Accepts: {string.Join(", ", acceptableTraits)}");
                            }
                        }
                    }

                    // Dealbreakers
                    List<string> dealbreakers = new List<string>();
                    if (mc.dealbreakerPersonalityTraits != null)
                    {
                        foreach (var trait in mc.dealbreakerPersonalityTraits)
                        {
                            if (trait != null) dealbreakers.Add(trait.displayName);
                        }
                    }
                    if (mc.dealbreakerInterests != null)
                    {
                        foreach (var interest in mc.dealbreakerInterests)
                        {
                            if (interest != null) dealbreakers.Add(interest.displayName);
                        }
                    }
                    if (mc.dealbreakerLifestyleTraits != null)
                    {
                        foreach (var trait in mc.dealbreakerLifestyleTraits)
                        {
                            if (trait != null) dealbreakers.Add(trait.displayName);
                        }
                    }

                    if (dealbreakers.Count > 0)
                    {
                        sb.AppendLine($"│");
                        sb.AppendLine($"│   DEALBREAKERS: {string.Join(", ", dealbreakers)}");
                    }
                }
            }
            else
            {
                sb.AppendLine($"├─────────────────────────────────────────────────────────────");
                sb.AppendLine($"│ MATCH CRITERIA: (none defined)");
            }

            sb.AppendLine($"└─────────────────────────────────────────────────────────────");
        }
    }
}
