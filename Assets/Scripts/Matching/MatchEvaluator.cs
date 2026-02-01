using System.Collections.Generic;
using System.Linq;
using Maskhot.Data;

namespace Maskhot.Matching
{
    /// <summary>
    /// Defines how Required trait requirements are handled during matching.
    /// </summary>
    public enum RequirementMode
    {
        /// <summary>
        /// Option 1: Uses minRequiredMet from MatchCriteria (0 = all must be met)
        /// Most strict - respects explicit per-client thresholds
        /// </summary>
        ExplicitThreshold,

        /// <summary>
        /// Option 2: 1 Required = always passes, 2+ Required = need at least 1
        /// Medium strictness - implicit softening based on count
        /// </summary>
        ImplicitSoftening,

        /// <summary>
        /// Option 3: Required traits never cause rejection, only affect score
        /// Least strict - only dealbreakers can reject
        /// </summary>
        ScoringOnly
    }

    /// <summary>
    /// Static utility class that evaluates candidates against match criteria.
    /// Performs all matching logic including dealbreaker checks, trait evaluation,
    /// flag counting, and score calculation.
    /// </summary>
    public static class MatchEvaluator
    {
        // Points awarded for meeting a Preferred requirement
        private const float PREFERRED_BONUS = 5f;

        // Points deducted for matching an Avoid requirement
        private const float AVOID_PENALTY = 10f;

        // Base score for a valid match (before bonuses/penalties)
        private const float BASE_SCORE = 50f;

        // Points awarded for meeting a Required requirement (used in ScoringOnly mode)
        private const float REQUIRED_MET_BONUS = 15f;

        // Points deducted for failing a Required requirement (used in ScoringOnly mode)
        private const float REQUIRED_FAILED_PENALTY = 10f;

        // Points deducted per year outside the preferred age range
        private const float AGE_PENALTY_PER_YEAR = 3f;

        /// <summary>
        /// Controls how Required trait requirements are handled.
        /// Default: ExplicitThreshold (uses minRequiredMet from criteria)
        /// </summary>
        public static RequirementMode Mode { get; set; } = RequirementMode.ExplicitThreshold;

        /// <summary>
        /// Legacy property for backwards compatibility.
        /// Setting to true = ImplicitSoftening, false = ExplicitThreshold
        /// </summary>
        public static bool UseImplicitSoftening
        {
            get => Mode == RequirementMode.ImplicitSoftening;
            set => Mode = value ? RequirementMode.ImplicitSoftening : RequirementMode.ExplicitThreshold;
        }

        /// <summary>
        /// Evaluates a candidate against the given match criteria.
        /// Returns a MatchResult with pass/fail status and detailed scoring.
        /// </summary>
        /// <param name="candidate">The candidate to evaluate</param>
        /// <param name="criteria">The match criteria to evaluate against</param>
        /// <returns>A MatchResult with evaluation details</returns>
        public static MatchResult Evaluate(CandidateProfileSO candidate, MatchCriteria criteria)
        {
            var result = new MatchResult();
            var profile = candidate.profile;

            // Step 1: Check gender
            if (!CheckGender(profile.gender, criteria.acceptableGenders))
            {
                result.GenderMismatch = true;
                result.IsMatch = false;
                return result;
            }

            // Step 2: Check age (soft requirement - affects score, not pass/fail)
            int yearsOutsideRange = CalculateYearsOutsideRange(profile.age, criteria.minAge, criteria.maxAge);
            if (yearsOutsideRange > 0)
            {
                result.AgeMismatch = true;
                result.YearsOutsideAgeRange = yearsOutsideRange;
            }

            // Step 3: Check dealbreakers
            string dealbreaker = FindDealbreaker(profile, criteria);
            if (dealbreaker != null)
            {
                result.HasDealbreaker = true;
                result.DealbreakerTrait = dealbreaker;
                result.IsMatch = false;
                return result;
            }

            // Step 4: Evaluate trait requirements
            EvaluateTraitRequirements(profile, criteria.traitRequirements, result);

            // Step 4b: Check if enough Required traits were met
            if (!CheckRequiredThreshold(criteria, result))
            {
                result.RequiredCheckFailed = true;
                result.IsMatch = false;
                return result;
            }

            // Step 5: Count flags from posts
            CountFlags(candidate, result);

            // Step 6: Check flag tolerance
            if (result.RedFlagCount > criteria.maxRedFlags)
            {
                result.TooManyRedFlags = true;
                result.IsMatch = false;
                return result;
            }

            if (result.GreenFlagCount < criteria.minGreenFlags)
            {
                result.NotEnoughGreenFlags = true;
                result.IsMatch = false;
                return result;
            }

            // Step 7: Calculate score
            CalculateScore(profile, criteria, result);

            // Match is valid!
            result.IsMatch = true;
            return result;
        }

        /// <summary>
        /// Checks if the candidate's gender is acceptable.
        /// </summary>
        private static bool CheckGender(Gender candidateGender, Gender[] acceptableGenders)
        {
            if (acceptableGenders == null || acceptableGenders.Length == 0)
                return true; // No preference means any gender is acceptable

            return acceptableGenders.Contains(candidateGender);
        }

        /// <summary>
        /// Calculates how many years the candidate's age is outside the acceptable range.
        /// Returns 0 if within range.
        /// </summary>
        private static int CalculateYearsOutsideRange(int candidateAge, int minAge, int maxAge)
        {
            if (candidateAge < minAge)
                return minAge - candidateAge;
            if (candidateAge > maxAge)
                return candidateAge - maxAge;
            return 0;
        }

        /// <summary>
        /// Checks if enough Required traits were met based on the current mode.
        /// Option 1 (ExplicitThreshold): Uses minRequiredMet from criteria
        /// Option 2 (ImplicitSoftening): 1 Required = Preferred, 2+ Required = need at least 1
        /// Option 3 (ScoringOnly): Required traits never cause rejection
        /// </summary>
        private static bool CheckRequiredThreshold(MatchCriteria criteria, MatchResult result)
        {
            int totalRequired = result.MetRequirements.Count + result.FailedRequirements.Count;
            int metCount = result.MetRequirements.Count;

            // No Required traits = always pass
            if (totalRequired == 0)
                return true;

            switch (Mode)
            {
                case RequirementMode.ScoringOnly:
                    // Option 3: Required traits never cause rejection
                    // Score is affected but candidate always passes this check
                    return true;

                case RequirementMode.ImplicitSoftening:
                    // Option 2: Implicit softening
                    // If only 1 Required trait, treat it as Preferred (always pass)
                    if (totalRequired == 1)
                        return true;
                    // If 2+ Required traits, need at least 1 met
                    return metCount >= 1;

                case RequirementMode.ExplicitThreshold:
                default:
                    // Option 1: Explicit threshold from minRequiredMet
                    int threshold = criteria.minRequiredMet;
                    // If threshold is 0 (default), require ALL
                    if (threshold <= 0)
                        return result.FailedRequirements.Count == 0;
                    // Otherwise, require at least the threshold number
                    return metCount >= threshold;
            }
        }

        /// <summary>
        /// Finds the first dealbreaker trait the candidate has.
        /// Returns the trait name if found, null otherwise.
        /// </summary>
        private static string FindDealbreaker(CandidateProfile profile, MatchCriteria criteria)
        {
            // Check personality dealbreakers
            if (criteria.dealbreakerPersonalityTraits != null && profile.personalityTraits != null)
            {
                foreach (var dealbreaker in criteria.dealbreakerPersonalityTraits)
                {
                    if (dealbreaker != null && profile.personalityTraits.Contains(dealbreaker))
                    {
                        return dealbreaker.displayName;
                    }
                }
            }

            // Check interest dealbreakers
            if (criteria.dealbreakerInterests != null && profile.interests != null)
            {
                foreach (var dealbreaker in criteria.dealbreakerInterests)
                {
                    if (dealbreaker != null && profile.interests.Contains(dealbreaker))
                    {
                        return dealbreaker.displayName;
                    }
                }
            }

            // Check lifestyle dealbreakers
            if (criteria.dealbreakerLifestyleTraits != null && profile.lifestyleTraits != null)
            {
                foreach (var dealbreaker in criteria.dealbreakerLifestyleTraits)
                {
                    if (dealbreaker != null && profile.lifestyleTraits.Contains(dealbreaker))
                    {
                        return dealbreaker.displayName;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates all trait requirements and populates the result accordingly.
        /// </summary>
        private static void EvaluateTraitRequirements(CandidateProfile profile, TraitRequirement[] requirements, MatchResult result)
        {
            if (requirements == null) return;

            foreach (var req in requirements)
            {
                if (req == null) continue;

                bool isMet = CheckRequirementMet(profile, req);
                string hintText = req.narrativeHints != null ? req.narrativeHints.GetRandomHint() : "(no hint)";

                switch (req.level)
                {
                    case RequirementLevel.Required:
                        if (isMet)
                            result.MetRequirements.Add(hintText);
                        else
                            result.FailedRequirements.Add(hintText);
                        break;

                    case RequirementLevel.Preferred:
                        if (isMet)
                            result.MetPreferences.Add(hintText);
                        break;

                    case RequirementLevel.Avoid:
                        if (isMet)
                            result.MatchedAvoids.Add(hintText);
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if a single trait requirement is met by the candidate.
        /// A requirement is met if the candidate has AT LEAST ONE of the acceptable traits.
        /// </summary>
        private static bool CheckRequirementMet(CandidateProfile profile, TraitRequirement requirement)
        {
            // Check interests
            if (requirement.acceptableInterests != null && profile.interests != null)
            {
                foreach (var interest in requirement.acceptableInterests)
                {
                    if (interest != null && profile.interests.Contains(interest))
                        return true;
                }
            }

            // Check personality traits
            if (requirement.acceptablePersonalityTraits != null && profile.personalityTraits != null)
            {
                foreach (var trait in requirement.acceptablePersonalityTraits)
                {
                    if (trait != null && profile.personalityTraits.Contains(trait))
                        return true;
                }
            }

            // Check lifestyle traits
            if (requirement.acceptableLifestyleTraits != null && profile.lifestyleTraits != null)
            {
                foreach (var trait in requirement.acceptableLifestyleTraits)
                {
                    if (trait != null && profile.lifestyleTraits.Contains(trait))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts red and green flags from the candidate's posts.
        /// Uses guaranteed posts only (random posts are session-specific).
        /// </summary>
        private static void CountFlags(CandidateProfileSO candidate, MatchResult result)
        {
            // Count from guaranteed posts (consistent across sessions)
            if (candidate.guaranteedPosts != null)
            {
                foreach (var post in candidate.guaranteedPosts)
                {
                    if (post.isRedFlag) result.RedFlagCount++;
                    if (post.isGreenFlag) result.GreenFlagCount++;
                }
            }
        }

        /// <summary>
        /// Calculates the match score based on trait overlap and bonuses/penalties.
        /// </summary>
        private static void CalculateScore(CandidateProfile profile, MatchCriteria criteria, MatchResult result)
        {
            var breakdown = result.Breakdown;

            // Set weights from criteria
            breakdown.PersonalityWeight = criteria.personalityWeight;
            breakdown.InterestsWeight = criteria.interestsWeight;
            breakdown.LifestyleWeight = criteria.lifestyleWeight;

            // Calculate individual scores based on requirement overlap
            breakdown.PersonalityScore = CalculateTraitCategoryScore(profile.personalityTraits, criteria.traitRequirements, TraitCategory.Personality);
            breakdown.InterestsScore = CalculateTraitCategoryScore(profile.interests, criteria.traitRequirements, TraitCategory.Interest);
            breakdown.LifestyleScore = CalculateTraitCategoryScore(profile.lifestyleTraits, criteria.traitRequirements, TraitCategory.Lifestyle);

            // Calculate bonuses and penalties for Preferred/Avoid
            breakdown.PreferredBonus = result.MetPreferences.Count * PREFERRED_BONUS;
            breakdown.AvoidPenalty = result.MatchedAvoids.Count * AVOID_PENALTY;

            // In ScoringOnly mode, Required traits affect score instead of pass/fail
            if (Mode == RequirementMode.ScoringOnly)
            {
                breakdown.RequiredBonus = result.MetRequirements.Count * REQUIRED_MET_BONUS;
                breakdown.RequiredPenalty = result.FailedRequirements.Count * REQUIRED_FAILED_PENALTY;
            }
            else
            {
                breakdown.RequiredBonus = 0;
                breakdown.RequiredPenalty = 0;
            }

            // Age penalty (soft requirement)
            breakdown.AgePenalty = result.YearsOutsideAgeRange * AGE_PENALTY_PER_YEAR;

            // Calculate weighted score
            float weightedScore = (breakdown.PersonalityScore * breakdown.PersonalityWeight)
                                + (breakdown.InterestsScore * breakdown.InterestsWeight)
                                + (breakdown.LifestyleScore * breakdown.LifestyleWeight);

            // Apply base score, bonuses, and penalties
            result.Score = BASE_SCORE + (weightedScore * 0.5f)
                         + breakdown.PreferredBonus - breakdown.AvoidPenalty
                         + breakdown.RequiredBonus - breakdown.RequiredPenalty
                         - breakdown.AgePenalty;

            // Clamp to 0-100
            if (result.Score < 0) result.Score = 0;
            if (result.Score > 100) result.Score = 100;
        }

        /// <summary>
        /// Internal enum for categorizing traits during scoring.
        /// </summary>
        private enum TraitCategory
        {
            Personality,
            Interest,
            Lifestyle
        }

        /// <summary>
        /// Calculates score (0-100) for a specific trait category based on requirement overlap.
        /// </summary>
        private static float CalculateTraitCategoryScore(object[] candidateTraits, TraitRequirement[] requirements, TraitCategory category)
        {
            if (candidateTraits == null || candidateTraits.Length == 0 || requirements == null)
                return 50f; // Neutral score if no data

            int totalRelevantRequirements = 0;
            int metRequirements = 0;

            foreach (var req in requirements)
            {
                if (req == null || req.level == RequirementLevel.Avoid)
                    continue;

                object[] acceptableTraits = GetAcceptableTraitsForCategory(req, category);
                if (acceptableTraits == null || acceptableTraits.Length == 0)
                    continue;

                totalRelevantRequirements++;

                // Check if any acceptable trait matches candidate's traits
                foreach (var acceptable in acceptableTraits)
                {
                    if (acceptable != null && candidateTraits.Contains(acceptable))
                    {
                        metRequirements++;
                        break;
                    }
                }
            }

            if (totalRelevantRequirements == 0)
                return 50f; // Neutral if no relevant requirements

            return (metRequirements / (float)totalRelevantRequirements) * 100f;
        }

        /// <summary>
        /// Gets the acceptable traits array for a specific category from a requirement.
        /// </summary>
        private static object[] GetAcceptableTraitsForCategory(TraitRequirement req, TraitCategory category)
        {
            switch (category)
            {
                case TraitCategory.Personality:
                    return req.acceptablePersonalityTraits?.Cast<object>().ToArray();
                case TraitCategory.Interest:
                    return req.acceptableInterests?.Cast<object>().ToArray();
                case TraitCategory.Lifestyle:
                    return req.acceptableLifestyleTraits?.Cast<object>().ToArray();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convenience method to evaluate a candidate against a client's criteria.
        /// </summary>
        public static MatchResult Evaluate(CandidateProfileSO candidate, ClientProfileSO client)
        {
            if (client.matchCriteria == null)
            {
                // No criteria defined - return a default "pass" result
                return new MatchResult
                {
                    IsMatch = true,
                    Score = 50f
                };
            }

            return Evaluate(candidate, client.matchCriteria);
        }
    }
}
