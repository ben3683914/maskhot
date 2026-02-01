using System.Collections.Generic;

namespace Maskhot.Matching
{
    /// <summary>
    /// Holds the complete result of evaluating a candidate against match criteria.
    /// Used to determine if a match is valid and calculate a match score.
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Overall pass/fail - true if candidate is a valid match
        /// A candidate fails if: dealbreaker found, gender mismatch,
        /// required trait missing, or flag tolerance exceeded
        /// Note: Age outside range is a soft requirement (affects score, not pass/fail)
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// Numeric score from 0-100 representing match quality
        /// Higher scores indicate better trait alignment
        /// Only meaningful if IsMatch is true
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// True if candidate has a dealbreaker trait
        /// </summary>
        public bool HasDealbreaker { get; set; }

        /// <summary>
        /// The specific dealbreaker trait name that caused rejection (if any)
        /// </summary>
        public string DealbreakerTrait { get; set; }

        /// <summary>
        /// True if candidate's gender doesn't match criteria
        /// </summary>
        public bool GenderMismatch { get; set; }

        /// <summary>
        /// True if candidate's age is outside the acceptable range
        /// (soft requirement - affects score, not pass/fail)
        /// </summary>
        public bool AgeMismatch { get; set; }

        /// <summary>
        /// Number of years the candidate's age is outside the acceptable range
        /// Used to calculate age penalty in scoring
        /// </summary>
        public int YearsOutsideAgeRange { get; set; }

        /// <summary>
        /// True if candidate exceeds the maximum allowed red flags
        /// </summary>
        public bool TooManyRedFlags { get; set; }

        /// <summary>
        /// True if candidate doesn't have enough green flags
        /// </summary>
        public bool NotEnoughGreenFlags { get; set; }

        /// <summary>
        /// True if candidate failed the Required trait threshold check
        /// (Only set when Required traits actually caused the rejection)
        /// </summary>
        public bool RequiredCheckFailed { get; set; }

        /// <summary>
        /// Number of red flag posts found
        /// </summary>
        public int RedFlagCount { get; set; }

        /// <summary>
        /// Number of green flag posts found
        /// </summary>
        public int GreenFlagCount { get; set; }

        /// <summary>
        /// List of Required trait requirements that were NOT met
        /// Contains the narrative hint text for display
        /// </summary>
        public List<string> FailedRequirements { get; set; } = new List<string>();

        /// <summary>
        /// List of Required trait requirements that WERE met
        /// Contains the narrative hint text for display
        /// </summary>
        public List<string> MetRequirements { get; set; } = new List<string>();

        /// <summary>
        /// List of Preferred trait requirements that were met (bonus points)
        /// Contains the narrative hint text for display
        /// </summary>
        public List<string> MetPreferences { get; set; } = new List<string>();

        /// <summary>
        /// List of Avoid trait requirements that were matched (penalty)
        /// Contains the narrative hint text for display
        /// </summary>
        public List<string> MatchedAvoids { get; set; } = new List<string>();

        /// <summary>
        /// Breakdown of how the score was calculated
        /// </summary>
        public ScoreBreakdown Breakdown { get; set; } = new ScoreBreakdown();

        /// <summary>
        /// Human-readable reason why the match failed (if IsMatch is false)
        /// </summary>
        public string FailureReason
        {
            get
            {
                if (IsMatch) return null;
                if (HasDealbreaker) return $"Dealbreaker: {DealbreakerTrait}";
                if (GenderMismatch) return "Gender preference not met";
                if (RequiredCheckFailed) return $"Missing required trait: {FailedRequirements[0]}";
                if (TooManyRedFlags) return $"Too many red flags ({RedFlagCount})";
                if (NotEnoughGreenFlags) return $"Not enough green flags ({GreenFlagCount})";
                return "Unknown";
            }
        }
    }

    /// <summary>
    /// Detailed breakdown of how the match score was calculated
    /// </summary>
    public class ScoreBreakdown
    {
        /// <summary>
        /// Score contribution from personality trait matches (0-100)
        /// </summary>
        public float PersonalityScore { get; set; }

        /// <summary>
        /// Score contribution from interest matches (0-100)
        /// </summary>
        public float InterestsScore { get; set; }

        /// <summary>
        /// Score contribution from lifestyle trait matches (0-100)
        /// </summary>
        public float LifestyleScore { get; set; }

        /// <summary>
        /// Bonus points from meeting Preferred requirements
        /// </summary>
        public float PreferredBonus { get; set; }

        /// <summary>
        /// Penalty points from matching Avoid requirements
        /// </summary>
        public float AvoidPenalty { get; set; }

        /// <summary>
        /// Bonus points from meeting Required requirements (ScoringOnly mode)
        /// </summary>
        public float RequiredBonus { get; set; }

        /// <summary>
        /// Penalty points from failing Required requirements (ScoringOnly mode)
        /// </summary>
        public float RequiredPenalty { get; set; }

        /// <summary>
        /// Penalty points for age outside the acceptable range
        /// </summary>
        public float AgePenalty { get; set; }

        /// <summary>
        /// Weight applied to personality score
        /// </summary>
        public float PersonalityWeight { get; set; }

        /// <summary>
        /// Weight applied to interests score
        /// </summary>
        public float InterestsWeight { get; set; }

        /// <summary>
        /// Weight applied to lifestyle score
        /// </summary>
        public float LifestyleWeight { get; set; }
    }
}
