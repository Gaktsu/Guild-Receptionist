#nullable enable
using System;

namespace GuildReceptionist.GameDesign.Domain
{
    public sealed class AssignmentPlanner
    {
        private readonly IMissionResolver _missionResolver;
        private readonly int _minimumPartySize;
        private readonly int _defaultDayIndex;
        private readonly ResolveOptions _previewOptions;

        public AssignmentPlanner(
            IMissionResolver missionResolver,
            int minimumPartySize = 1,
            int defaultDayIndex = 0,
            ResolveOptions? previewOptions = null)
        {
            _missionResolver = missionResolver ?? throw new ArgumentNullException(nameof(missionResolver));
            _minimumPartySize = Math.Max(1, minimumPartySize);
            _defaultDayIndex = Math.Max(0, defaultDayIndex);
            _previewOptions = previewOptions ?? new ResolveOptions(
                enableTraitEffects: true,
                enableInjurySimulation: false,
                globalDifficultyMultiplier: 1f,
                criticalSuccessBonus: 0f);
        }

        public bool CanAssign(QuestInstance quest, Party party)
        {
            if (quest is null)
            {
                throw new ArgumentNullException(nameof(quest));
            }

            if (party is null)
            {
                throw new ArgumentNullException(nameof(party));
            }

            if (quest.State != QuestState.Pending)
            {
                return false;
            }

            var requiredMembers = GetRequiredMinimumMembers(quest);
            if (party.Members.Count < requiredMembers)
            {
                return false;
            }

            foreach (var member in party.Members)
            {
                if (!member.IsDeployable())
                {
                    return false;
                }
            }

            return true;
        }

        public float EvaluateMatch(QuestInstance quest, Party party)
        {
            if (!CanAssign(quest, party))
            {
                return 0f;
            }

            var request = new ResolveRequest
            {
                Quest = quest,
                Party = party,
                World = new WorldStateSnapshot(dayIndex: _defaultDayIndex),
                DayIndex = _defaultDayIndex,
                Seed = 1337,
                Options = _previewOptions
            };

            var result = _missionResolver.Resolve(request);
            return result.FinalSuccessChance;
        }

        private int GetRequiredMinimumMembers(QuestInstance quest)
        {
            var difficultyBased = quest.AssessedDifficulty switch
            {
                < 30f => 1,
                < 60f => 2,
                < 90f => 3,
                _ => 4
            };

            return Math.Max(_minimumPartySize, difficultyBased);
        }
    }
}
