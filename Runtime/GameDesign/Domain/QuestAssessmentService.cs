#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    public interface IEnvironmentDifficultyModifier
    {
        float GetModifier(QuestInstance quest, WorldStateSnapshot world);
    }

    public interface IQuestRiskModel
    {
        float EvaluateRisk(QuestInstance quest, WorldStateSnapshot world, float assessedDifficulty);
    }

    public interface IQuestAssessmentService
    {
        float Assess(QuestInstance quest, WorldStateSnapshot world);
        QuestRank RecommendRank(QuestInstance quest, float assessedDifficulty);
        void ApplyAssessment(QuestInstance quest, WorldStateSnapshot world);
    }

    /// <summary>
    /// QuestInstance의 기본 난이도에 세계 상태(날씨/지역 위험도/월드 태그)를 반영해
    /// 체감 난이도와 추천 랭크를 계산하고 QuestInstance.ApplyAssessment를 호출한다.
    /// </summary>
    public sealed class QuestAssessmentService : IQuestAssessmentService
    {
        private readonly IEnvironmentDifficultyModifier _environmentModifier;
        private readonly IQuestRiskModel _riskModel;

        public QuestAssessmentService(
            IEnvironmentDifficultyModifier? environmentModifier = null,
            IQuestRiskModel? riskModel = null)
        {
            _environmentModifier = environmentModifier ?? new DefaultEnvironmentDifficultyModifier();
            _riskModel = riskModel ?? new DefaultQuestRiskModel();
        }

        public float Assess(QuestInstance quest, WorldStateSnapshot world)
        {
            if (quest is null)
            {
                throw new ArgumentNullException(nameof(quest));
            }

            var baseDifficulty = Math.Max(1f, quest.BaseDifficulty);
            var environmentModifier = _environmentModifier.GetModifier(quest, world);
            var assessedDifficulty = Math.Max(1f, baseDifficulty * environmentModifier);
            return assessedDifficulty;
        }

        public QuestRank RecommendRank(QuestInstance quest, float assessedDifficulty)
        {
            if (quest is null)
            {
                throw new ArgumentNullException(nameof(quest));
            }

            return assessedDifficulty switch
            {
                < 15f => QuestRank.F,
                < 30f => QuestRank.E,
                < 45f => QuestRank.D,
                < 65f => QuestRank.C,
                < 85f => QuestRank.B,
                < 110f => QuestRank.A,
                _ => QuestRank.S
            };
        }

        public void ApplyAssessment(QuestInstance quest, WorldStateSnapshot world)
        {
            if (quest is null)
            {
                throw new ArgumentNullException(nameof(quest));
            }

            var assessedDifficulty = Assess(quest, world);
            var recommendedRank = RecommendRank(quest, assessedDifficulty);
            var riskScore = _riskModel.EvaluateRisk(quest, world, assessedDifficulty);
            var expectedReward = BuildExpectedReward(quest.BaseReward, riskScore, assessedDifficulty, quest.BaseDifficulty);

            quest.ApplyAssessment(
                assessedDifficulty: assessedDifficulty,
                recommendedRank: recommendedRank,
                riskScore: riskScore,
                expectedReward: expectedReward);
        }

        private static RewardPackage BuildExpectedReward(RewardPackage baseReward, float riskScore, float assessedDifficulty, float baseDifficulty)
        {
            var difficultyScale = assessedDifficulty / Math.Max(1f, baseDifficulty);
            var riskScale = 1f + (riskScore * 0.35f);
            var totalScale = Math.Clamp(difficultyScale * riskScale, 0.6f, 2.4f);

            return new RewardPackage(
                gold: (int)MathF.Round(baseReward.Gold * totalScale),
                reputation: (int)MathF.Round(baseReward.Reputation * (0.85f + riskScore * 0.45f)),
                items: baseReward.Items);
        }
    }

    public sealed class DefaultEnvironmentDifficultyModifier : IEnvironmentDifficultyModifier
    {
        private static readonly IReadOnlyDictionary<string, float> EmptyLocationRiskById =
            new Dictionary<string, float>();

        public float GetModifier(QuestInstance quest, WorldStateSnapshot world)
        {
            var weatherWeight = 1f + (world.WeatherSeverity * 0.15f);
            var globalRiskWeight = 1f + (world.GlobalRiskLevel * 0.20f);

            var locationRiskById = world.LocationRiskById ?? EmptyLocationRiskById;
            var activeWorldTags = world.ActiveWorldTags ?? Array.Empty<string>();

            var locationRisk = 0f;
            if (!string.IsNullOrWhiteSpace(quest.LocationId) && locationRiskById.TryGetValue(quest.LocationId, out var foundRisk))
            {
                locationRisk = foundRisk;
            }

            var locationWeight = 1f + (locationRisk * 0.30f);
            var tagWeight = 1f + (CountThreatTags(quest.EnvironmentTags, activeWorldTags) * 0.05f);

            return Math.Clamp(weatherWeight * globalRiskWeight * locationWeight * tagWeight, 0.75f, 2.5f);
        }

        private static int CountThreatTags(IReadOnlyList<string> questTags, IReadOnlyList<string> worldTags)
        {
            if (questTags.Count == 0 || worldTags.Count == 0)
            {
                return 0;
            }

            var set = new HashSet<string>(worldTags, StringComparer.OrdinalIgnoreCase);
            var count = 0;
            foreach (var tag in questTags)
            {
                if (set.Contains(tag))
                {
                    count++;
                }
            }

            return count;
        }
    }

    public sealed class DefaultQuestRiskModel : IQuestRiskModel
    {
        public float EvaluateRisk(QuestInstance quest, WorldStateSnapshot world, float assessedDifficulty)
        {
            var difficultyRisk = assessedDifficulty / 120f;
            var timePressureRisk = quest.TimeLimitDays <= 0 ? 0.35f : Math.Clamp(1f / quest.TimeLimitDays, 0.02f, 0.35f);
            var expirationRisk = world.DayIndex >= quest.ExpireDay ? 0.25f : 0f;
            var categoryRisk = quest.Category == QuestCategory.Special ? 0.12f : 0f;

            return Math.Clamp(difficultyRisk + timePressureRisk + expirationRisk + categoryRisk, 0f, 1f);
        }
    }
}
