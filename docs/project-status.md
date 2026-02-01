# Project Status

Track what's complete, in progress, and to do.

## Implementation Status

### Completed

**Data Layer**
- [x] Core data classes (CandidateProfile, ClientProfile, SocialMediaPost, Quest, MatchCriteria)
- [x] Trait ScriptableObjects (InterestSO, PersonalityTraitSO, LifestyleTraitSO)
- [x] Profile ScriptableObjects (CandidateProfileSO, ClientProfileSO)
- [x] NarrativeHintCollectionSO for abstract quest hints
- [x] RandomPostPoolSO for random post storage

**JSON Import System**
- [x] ScriptableObjectImporter editor tool
- [x] Menu: `Tools > Maskhot > Import Data from JSON`
- [x] Imports: Interests, Personality Traits, Lifestyle Traits, Narrative Hints, Random Posts, Candidates, Clients
- [x] Automatic trait reference resolution

**Manager Layer**
- [x] ProfileManager - Loads/serves all profiles and traits from Resources
- [x] PostPoolManager - Random post selection with trait matching
- [x] MatchQueueManager - Queue population, decision tracking, candidate filtering

**Controller Layer**
- [x] MatchListController - Selection state, navigation, fires events for UI

**Matching System**
- [x] MatchEvaluator - Static evaluation logic (currently using **ImplicitSoftening** mode)
- [x] MatchResult - Detailed result data with score breakdown
- [x] Three algorithm modes (ExplicitThreshold, ImplicitSoftening, ScoringOnly)
- [x] Full pipeline: gender/age -> dealbreakers -> requirements -> flags -> scoring

**Test Data**
- [x] 25 interests across categories
- [x] 15 personality traits (including "Controversial" for red flags)
- [x] 10 lifestyle traits
- [x] 10 candidate profiles with guaranteed posts
- [x] 5 client profiles with match criteria
- [x] 1000 random posts in pool
- [x] 15 narrative hint collections

**Testing Tools**
- [x] ProfileTester - Verify profile data
- [x] RandomPostTester - Verify random post generation
- [x] ClientTester - Verify client profiles and criteria
- [x] MatchingTester - Verify matching algorithm
- [x] MatchQueueTester - Verify queue population and decisions
- [x] MatchListTester - Verify selection, navigation, events

### In Progress

*Nothing currently in progress*

### To Do

**Manager Layer**
- [ ] GameManager - Overall game state, level progression
- [ ] QuestManager - Current quest management, validation

**Controllers**
- [ ] DecisionController - Accept/reject decisions and scoring

**Procedural Generation**
- [ ] QuestGenerator - Creates Quest instances from ClientProfileSO or procedural data

**UI Implementation** *(handled by separate developer, not using Claude)*
- [ ] Three-panel layout (left: queue, center: feed, right: criteria)
- [ ] Profile cards and social media post prefabs
- [ ] Decision buttons (Accept/Reject)
- [ ] Client header display
- [ ] Quest criteria display

---

## Current Priorities

1. **Remaining Managers** - GameManager, QuestManager
2. **Remaining Controllers** - DecisionController
3. **Integration** - Connect backend systems to UI (once UI is ready)

---

## Build Order Recommendation

1. ~~Data Structures~~ COMPLETE
2. ~~ScriptableObjects~~ COMPLETE
3. ~~Manager Layer (ProfileManager, PostPoolManager, MatchQueueManager)~~ COMPLETE
4. ~~Matching System~~ COMPLETE
5. ~~Controller Layer (MatchListController)~~ COMPLETE
6. UI Implementation *(separate developer)*
7. **Remaining Managers (GameManager, QuestManager)** <- CURRENT FOCUS
8. Remaining Controllers (DecisionController)
9. QuestGenerator for procedural quests
10. Integration and polish

---

## Notes

*Add development notes here as the project progresses*
