using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Maskhot.Data;
using Maskhot.Matching;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify the matching/scoring algorithm.
    /// Use the Context Menu to run matching tests between candidates and clients.
    /// </summary>
    public class MatchingTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Specific candidate to test")]
        public CandidateProfileSO specificCandidate;

        [Tooltip("Specific client to test against")]
        public ClientProfileSO specificClient;

        [Header("Batch Testing")]
        [Tooltip("Candidates to test in batch mode")]
        public CandidateProfileSO[] candidatesToTest;

        [Tooltip("Clients to test against in batch mode")]
        public ClientProfileSO[] clientsToTest;

        [Header("Algorithm Settings")]
        [Tooltip("How Required trait requirements are handled")]
        public RequirementMode requirementMode = RequirementMode.ImplicitSoftening;

        [Header("Output Settings")]
        [Tooltip("Show detailed score breakdown")]
        public bool showScoreBreakdown = true;

        [Tooltip("Show all requirement details (even when passed)")]
        public bool verboseRequirements = false;

        /// <summary>
        /// Syncs the inspector toggle with the static MatchEvaluator setting
        /// </summary>
        private void SyncAlgorithmMode()
        {
            MatchEvaluator.Mode = requirementMode;
        }

        /// <summary>
        /// Gets a string describing the current algorithm mode
        /// </summary>
        private string GetModeDescription()
        {
            switch (requirementMode)
            {
                case RequirementMode.ExplicitThreshold:
                    return "EXPLICIT THRESHOLD (uses minRequiredMet from criteria)";
                case RequirementMode.ImplicitSoftening:
                    return "IMPLICIT SOFTENING (1 Req=Preferred, 2+ Req=need 1)";
                case RequirementMode.ScoringOnly:
                    return "SCORING ONLY (Required affects score, not pass/fail)";
                default:
                    return "UNKNOWN";
            }
        }

        /// <summary>
        /// Cycles through algorithm modes (3 options)
        /// </summary>
        [ContextMenu("Cycle Algorithm Mode")]
        public void CycleAlgorithmMode()
        {
            // Cycle: ExplicitThreshold -> ImplicitSoftening -> ScoringOnly -> ExplicitThreshold
            switch (requirementMode)
            {
                case RequirementMode.ExplicitThreshold:
                    requirementMode = RequirementMode.ImplicitSoftening;
                    break;
                case RequirementMode.ImplicitSoftening:
                    requirementMode = RequirementMode.ScoringOnly;
                    break;
                case RequirementMode.ScoringOnly:
                default:
                    requirementMode = RequirementMode.ExplicitThreshold;
                    break;
            }
            SyncAlgorithmMode();
            Debug.Log($"MatchingTester: Switched to {GetModeDescription()}");
        }

        /// <summary>
        /// Tests a specific candidate against a specific client
        /// </summary>
        [ContextMenu("Test Specific Match")]
        public void TestSpecificMatch()
        {
            SyncAlgorithmMode();

            if (specificCandidate == null)
            {
                Debug.LogError("MatchingTester: No specific candidate assigned!");
                return;
            }
            if (specificClient == null)
            {
                Debug.LogError("MatchingTester: No specific client assigned!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Mode: {GetModeDescription()}");
            BuildMatchTestLog(specificCandidate, specificClient, sb);
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests all assigned candidates against all assigned clients
        /// </summary>
        [ContextMenu("Test All Assigned (Candidates x Clients)")]
        public void TestAllAssigned()
        {
            SyncAlgorithmMode();

            if (candidatesToTest == null || candidatesToTest.Length == 0)
            {
                Debug.LogError("MatchingTester: No candidates assigned to test array!");
                return;
            }
            if (clientsToTest == null || clientsToTest.Length == 0)
            {
                Debug.LogError("MatchingTester: No clients assigned to test array!");
                return;
            }

            Debug.Log($"═══ MATCHING TESTER - {candidatesToTest.Length} CANDIDATES x {clientsToTest.Length} CLIENTS ═══\nMode: {GetModeDescription()}");

            foreach (var client in clientsToTest)
            {
                if (client == null) continue;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"╔═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"║ CLIENT: {client.profile.clientName}");
                sb.AppendLine($"╠═══════════════════════════════════════════════════════════════");

                int matchCount = 0;
                foreach (var candidate in candidatesToTest)
                {
                    if (candidate == null) continue;

                    var result = MatchEvaluator.Evaluate(candidate, client);
                    if (result.IsMatch) matchCount++;

                    BuildMatchTestLog(candidate, client, sb, result);
                }

                sb.AppendLine($"╠═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"║ SUMMARY: {matchCount}/{candidatesToTest.Length} candidates matched");
                sb.AppendLine($"╚═══════════════════════════════════════════════════════════════");

                Debug.Log(sb.ToString());
            }

            Debug.Log($"═══ COMPLETE ═══");
        }

        /// <summary>
        /// Tests all candidates from Resources against all clients from Resources
        /// </summary>
        [ContextMenu("Test All (from Resources)")]
        public void TestAllFromResources()
        {
            SyncAlgorithmMode();

            CandidateProfileSO[] allCandidates = Resources.LoadAll<CandidateProfileSO>("GameData/Profiles");
            ClientProfileSO[] allClients = Resources.LoadAll<ClientProfileSO>("GameData/Clients");

            if (allCandidates == null || allCandidates.Length == 0)
            {
                Debug.LogError("MatchingTester: No candidates found in Resources/GameData/Profiles!");
                return;
            }
            if (allClients == null || allClients.Length == 0)
            {
                Debug.LogError("MatchingTester: No clients found in Resources/GameData/Clients!");
                return;
            }

            Debug.Log($"═══ MATCHING TESTER - ALL {allCandidates.Length} CANDIDATES x {allClients.Length} CLIENTS (from Resources) ═══\nMode: {GetModeDescription()}");

            foreach (var client in allClients)
            {
                if (client == null) continue;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"╔═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"║ CLIENT: {client.profile.clientName} (Level {client.suggestedLevel})");
                sb.AppendLine($"╠═══════════════════════════════════════════════════════════════");

                int matchCount = 0;
                List<string> matchedNames = new List<string>();

                foreach (var candidate in allCandidates)
                {
                    if (candidate == null) continue;

                    var result = MatchEvaluator.Evaluate(candidate, client);
                    if (result.IsMatch)
                    {
                        matchCount++;
                        matchedNames.Add($"{candidate.profile.characterName} ({result.Score:F0})");
                    }

                    BuildMatchTestLog(candidate, client, sb, result);
                }

                sb.AppendLine($"╠═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"║ SUMMARY: {matchCount}/{allCandidates.Length} candidates matched");
                if (matchedNames.Count > 0)
                {
                    sb.AppendLine($"║ MATCHES: {string.Join(", ", matchedNames)}");
                }
                sb.AppendLine($"╚═══════════════════════════════════════════════════════════════");

                Debug.Log(sb.ToString());
            }

            Debug.Log($"═══ COMPLETE ═══");
        }

        /// <summary>
        /// Quick test: Shows a summary of matches per client
        /// </summary>
        [ContextMenu("Quick Summary (from Resources)")]
        public void QuickSummary()
        {
            SyncAlgorithmMode();

            CandidateProfileSO[] allCandidates = Resources.LoadAll<CandidateProfileSO>("GameData/Profiles");
            ClientProfileSO[] allClients = Resources.LoadAll<ClientProfileSO>("GameData/Clients");

            if (allCandidates == null || allCandidates.Length == 0 || allClients == null || allClients.Length == 0)
            {
                Debug.LogError("MatchingTester: No data found in Resources!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"═══ MATCHING SUMMARY ═══");
            sb.AppendLine($"Mode: {GetModeDescription()}");
            sb.AppendLine($"Candidates: {allCandidates.Length} | Clients: {allClients.Length}");
            sb.AppendLine();

            foreach (var client in allClients)
            {
                if (client == null) continue;

                List<string> matches = new List<string>();
                List<string> rejects = new List<string>();

                foreach (var candidate in allCandidates)
                {
                    if (candidate == null) continue;

                    var result = MatchEvaluator.Evaluate(candidate, client);
                    if (result.IsMatch)
                    {
                        matches.Add($"{candidate.profile.characterName} ({result.Score:F0})");
                    }
                    else
                    {
                        rejects.Add($"{candidate.profile.characterName}: {result.FailureReason}");
                    }
                }

                sb.AppendLine($"► {client.profile.clientName} (L{client.suggestedLevel}):");
                sb.AppendLine($"  ✓ Matches ({matches.Count}): {(matches.Count > 0 ? string.Join(", ", matches) : "none")}");
                if (verboseRequirements && rejects.Count > 0)
                {
                    sb.AppendLine($"  ✗ Rejected ({rejects.Count}):");
                    foreach (var reject in rejects)
                    {
                        sb.AppendLine($"    - {reject}");
                    }
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        private void BuildMatchTestLog(CandidateProfileSO candidate, ClientProfileSO client, StringBuilder sb, MatchResult result = null)
        {
            if (result == null)
            {
                result = MatchEvaluator.Evaluate(candidate, client);
            }

            var profile = candidate.profile;
            string status = result.IsMatch ? "✓ MATCH" : "✗ REJECT";
            string scoreText = result.IsMatch ? $" (Score: {result.Score:F1})" : "";

            sb.AppendLine($"├─────────────────────────────────────────────────────────────");
            sb.AppendLine($"│ {profile.characterName} ({profile.gender}, {profile.age}) → {status}{scoreText}");

            if (!result.IsMatch)
            {
                sb.AppendLine($"│   Reason: {result.FailureReason}");
            }
            else if (showScoreBreakdown)
            {
                var bd = result.Breakdown;
                sb.AppendLine($"│   Personality: {bd.PersonalityScore:F0} x {bd.PersonalityWeight:P0} | " +
                             $"Interests: {bd.InterestsScore:F0} x {bd.InterestsWeight:P0} | " +
                             $"Lifestyle: {bd.LifestyleScore:F0} x {bd.LifestyleWeight:P0}");

                // Show bonuses/penalties
                List<string> bonuses = new List<string>();
                if (bd.PreferredBonus > 0) bonuses.Add($"Pref +{bd.PreferredBonus:F0}");
                if (bd.RequiredBonus > 0) bonuses.Add($"Req +{bd.RequiredBonus:F0}");

                List<string> penalties = new List<string>();
                if (bd.AvoidPenalty > 0) penalties.Add($"Avoid -{bd.AvoidPenalty:F0}");
                if (bd.RequiredPenalty > 0) penalties.Add($"Req -{bd.RequiredPenalty:F0}");
                if (bd.AgePenalty > 0) penalties.Add($"Age -{bd.AgePenalty:F0}");

                if (bonuses.Count > 0 || penalties.Count > 0)
                {
                    string bonusStr = bonuses.Count > 0 ? string.Join(", ", bonuses) : "none";
                    string penaltyStr = penalties.Count > 0 ? string.Join(", ", penalties) : "none";
                    sb.AppendLine($"│   Bonuses: {bonusStr} | Penalties: {penaltyStr}");
                }
            }

            if (verboseRequirements || !result.IsMatch)
            {
                // Show age penalty if applicable
                if (result.AgeMismatch)
                {
                    sb.AppendLine($"│   Age: {result.YearsOutsideAgeRange} year(s) outside range (-{result.Breakdown.AgePenalty:F0} pts)");
                }

                // Show flags
                sb.AppendLine($"│   Flags: {result.GreenFlagCount} green, {result.RedFlagCount} red");

                // Show met requirements
                if (result.MetRequirements.Count > 0)
                {
                    sb.AppendLine($"│   ✓ Required met ({result.MetRequirements.Count}): {string.Join(", ", result.MetRequirements)}");
                }

                // Show failed requirements
                if (result.FailedRequirements.Count > 0)
                {
                    sb.AppendLine($"│   ✗ Required failed ({result.FailedRequirements.Count}): {string.Join(", ", result.FailedRequirements)}");
                }

                // Show preferences
                if (result.MetPreferences.Count > 0)
                {
                    sb.AppendLine($"│   ★ Preferences met ({result.MetPreferences.Count}): {string.Join(", ", result.MetPreferences)}");
                }

                // Show avoids
                if (result.MatchedAvoids.Count > 0)
                {
                    sb.AppendLine($"│   ⚠ Avoids matched ({result.MatchedAvoids.Count}): {string.Join(", ", result.MatchedAvoids)}");
                }
            }
        }
    }
}
