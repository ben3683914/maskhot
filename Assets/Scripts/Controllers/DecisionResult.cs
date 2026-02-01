using Maskhot.Data;
using Maskhot.Managers;
using Maskhot.Matching;

namespace Maskhot.Controllers
{
    /// <summary>
    /// Represents the outcome of a player's decision on a candidate.
    /// Contains whether the decision was correct and detailed match evaluation data.
    /// </summary>
    public struct DecisionResult
    {
        /// <summary>
        /// The candidate this decision was made for.
        /// </summary>
        public CandidateProfileSO Candidate { get; set; }

        /// <summary>
        /// The decision the player made (Accepted or Rejected).
        /// </summary>
        public CandidateDecision Decision { get; set; }

        /// <summary>
        /// Whether the candidate was actually a valid match according to criteria.
        /// </summary>
        public bool WasActualMatch { get; set; }

        /// <summary>
        /// Whether the player's decision was correct.
        /// Correct = (Accepted a match) or (Rejected a non-match)
        /// </summary>
        public bool IsCorrect { get; set; }

        /// <summary>
        /// The match score (0-100) from MatchEvaluator.
        /// </summary>
        public float MatchScore { get; set; }

        /// <summary>
        /// The full MatchResult from evaluation (for detailed breakdown).
        /// </summary>
        public MatchResult MatchEvaluation { get; set; }

        /// <summary>
        /// Classification of the decision outcome.
        /// </summary>
        public DecisionOutcome Outcome
        {
            get
            {
                if (Decision == CandidateDecision.Accepted)
                {
                    return WasActualMatch ? DecisionOutcome.TruePositive : DecisionOutcome.FalsePositive;
                }
                else if (Decision == CandidateDecision.Rejected)
                {
                    return WasActualMatch ? DecisionOutcome.FalseNegative : DecisionOutcome.TrueNegative;
                }
                return DecisionOutcome.Pending;
            }
        }

        /// <summary>
        /// Human-readable description of why the match passed or failed.
        /// </summary>
        public string MatchReason
        {
            get
            {
                if (MatchEvaluation == null) return "(no evaluation)";

                if (WasActualMatch)
                {
                    return $"Valid match (score: {MatchScore:F0})";
                }
                else
                {
                    return MatchEvaluation.FailureReason ?? "Not a match";
                }
            }
        }
    }

    /// <summary>
    /// Classification of decision outcomes using standard confusion matrix terminology.
    /// </summary>
    public enum DecisionOutcome
    {
        /// <summary>No decision made yet.</summary>
        Pending,

        /// <summary>Accepted a valid match (correct accept).</summary>
        TruePositive,

        /// <summary>Rejected a non-match (correct reject).</summary>
        TrueNegative,

        /// <summary>Accepted a non-match (incorrect accept).</summary>
        FalsePositive,

        /// <summary>Rejected a valid match (incorrect reject).</summary>
        FalseNegative
    }
}
