#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    public sealed class QuestInstance
    {
        public string QuestId { get; }
        public string TemplateId { get; }
        public string Title { get; }
        public QuestCategory Category { get; }
        public QuestState State { get; private set; }

        public float BaseDifficulty { get; }
        public float AssessedDifficulty { get; private set; }
        public QuestRank RecommendedRank { get; private set; }
        public float RiskScore { get; private set; }

        public int IssuedDay { get; }
        public int ExpireDay { get; }
        public int TimeLimitDays { get; }

        public string LocationId { get; }
        public IReadOnlyList<string> EnvironmentTags { get; }

        public RewardPackage BaseReward { get; }
        public RewardPackage ExpectedReward { get; private set; }

        public string? AssignedPartyId { get; private set; }
        public MissionOutcome? Resolution { get; private set; }
        public int Version { get; private set; }

        public QuestInstance(
            string questId,
            string templateId,
            string title,
            QuestCategory category,
            float baseDifficulty,
            int issuedDay,
            int expireDay,
            int timeLimitDays,
            string locationId,
            IReadOnlyList<string> environmentTags,
            RewardPackage baseReward)
        {
            QuestId = questId;
            TemplateId = templateId;
            Title = title;
            Category = category;
            State = QuestState.Pending;
            BaseDifficulty = baseDifficulty;
            AssessedDifficulty = baseDifficulty;
            RecommendedRank = QuestRank.F;
            RiskScore = 0f;
            IssuedDay = issuedDay;
            ExpireDay = expireDay;
            TimeLimitDays = timeLimitDays;
            LocationId = locationId;
            EnvironmentTags = environmentTags;
            BaseReward = baseReward;
            ExpectedReward = baseReward;
            AssignedPartyId = null;
            Resolution = null;
            Version = 0;
        }

        public void ApplyAssessment(float assessedDifficulty, QuestRank recommendedRank, float riskScore, RewardPackage expectedReward)
        {
            AssessedDifficulty = assessedDifficulty;
            RecommendedRank = recommendedRank;
            RiskScore = riskScore;
            ExpectedReward = expectedReward;
            Version++;
        }

        public void AssignToParty(string partyId)
        {
            if (!CanTransitionTo(QuestState.Assigned))
            {
                throw new InvalidOperationException($"Cannot transition {State} -> {QuestState.Assigned}");
            }

            AssignedPartyId = partyId;
            State = QuestState.Assigned;
            Version++;
        }

        public void MarkInProgress()
        {
            if (!CanTransitionTo(QuestState.InProgress))
            {
                throw new InvalidOperationException($"Cannot transition {State} -> {QuestState.InProgress}");
            }

            State = QuestState.InProgress;
            Version++;
        }

        public void Resolve(MissionOutcome outcome)
        {
            if (!CanTransitionTo(QuestState.Resolved))
            {
                throw new InvalidOperationException($"Cannot transition {State} -> {QuestState.Resolved}");
            }

            Resolution = outcome;
            State = QuestState.Resolved;
            Version++;
        }

        public void Archive()
        {
            if (!CanTransitionTo(QuestState.Archived))
            {
                throw new InvalidOperationException($"Cannot transition {State} -> {QuestState.Archived}");
            }

            State = QuestState.Archived;
            Version++;
        }

        public bool CanTransitionTo(QuestState next)
        {
            return (State, next) switch
            {
                (QuestState.Pending, QuestState.Assigned) => true,
                (QuestState.Assigned, QuestState.InProgress) => true,
                (QuestState.InProgress, QuestState.Resolved) => true,
                (QuestState.Resolved, QuestState.Archived) => true,
                (QuestState.Pending, QuestState.Archived) => true,
                _ => false,
            };
        }
    }
}
