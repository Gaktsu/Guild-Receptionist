#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    public readonly struct ResolveRequest
    {
        public QuestInstance Quest { get; init; }
        public Party Party { get; init; }
        public WorldStateSnapshot World { get; init; }
        public int DayIndex { get; init; }
        public int Seed { get; init; }
        public ResolveOptions Options { get; init; }
    }

    public readonly struct ResolveResult
    {
        public MissionOutcome Outcome { get; init; }
        public float FinalSuccessChance { get; init; }
        public OutcomeGrade Grade { get; init; }
        public RewardPackage Rewards { get; init; }
        public InjuryPackage Injuries { get; init; }
        public FatiguePackage Fatigue { get; init; }
        public IReadOnlyList<ResolveLogEntry> Logs { get; init; }
        public int ConsumedSeed { get; init; }
    }

    public interface IMissionResolver
    {
        ResolveResult Resolve(in ResolveRequest request);
    }

    /// <summary>
    /// 핵심 로직:
    /// 1) 파티 종합 전력(가중합) 계산
    /// 2) 난이도와 전력 비율로 성공 확률 계산
    /// 3) 랜덤 롤로 등급(Critical/Success/Partial/Fail) 판정
    /// </summary>
    public sealed class MissionResolver : IMissionResolver
    {
        private const float MinSuccessChance = 0.05f;
        private const float MaxSuccessChance = 0.95f;

        public ResolveResult Resolve(in ResolveRequest request)
        {
            if (request.Quest is null)
            {
                throw new ArgumentNullException(nameof(request.Quest));
            }

            if (request.Party is null)
            {
                throw new ArgumentNullException(nameof(request.Party));
            }

            var logs = new List<ResolveLogEntry>();
            var effectiveDifficulty = Math.Max(1f, request.Quest.AssessedDifficulty * request.Options.GlobalDifficultyMultiplier);

            var partyPower = CalculatePartyPower(request.Party, logs);
            var statScore = partyPower / effectiveDifficulty;

            // 시그모이드 기반 확률화: score=1.0일 때 50%, 1.3 부근에서 70%대
            var logistic = 1f / (1f + MathF.Exp(-(statScore - 1f) * 3.2f));

            // 기한 압박 페널티: 만료일이 임박할수록 -최대 15%
            var daysLeft = request.Quest.ExpireDay - request.DayIndex;
            var deadlinePenalty = daysLeft <= 0 ? 0.15f : MathF.Min(0.15f, 0.03f / MathF.Max(daysLeft, 1));

            var finalSuccessChance = Math.Clamp(logistic - deadlinePenalty, MinSuccessChance, MaxSuccessChance);

            logs.Add(new ResolveLogEntry($"Difficulty={effectiveDifficulty:F2}, PartyPower={partyPower:F2}, Score={statScore:F2}"));
            logs.Add(new ResolveLogEntry($"Logistic={logistic:F3}, DeadlinePenalty={deadlinePenalty:F3}, FinalChance={finalSuccessChance:F3}"));

            var random = new Random(request.Seed);
            var rollValue = (float)random.NextDouble();
            var grade = DetermineGrade(finalSuccessChance, rollValue, request.Options.CriticalSuccessBonus);

            var rewards = BuildRewardPackage(request.Quest.BaseReward, grade);
            var injuries = BuildInjuryPackage(request.Party, grade, random, request.Options.EnableInjurySimulation);
            var fatigue = BuildFatiguePackage(request.Party, request.Quest, grade);

            var outcome = new MissionOutcome
            {
                QuestId = request.Quest.QuestId,
                PartyId = request.Party.PartyId,
                IsSuccess = grade != OutcomeGrade.Fail,
                Grade = grade,
                SuccessChance = finalSuccessChance,
                RollValue = rollValue,
                Rewards = rewards,
                Injuries = injuries,
                ResolvedDay = request.DayIndex
            };

            return new ResolveResult
            {
                Outcome = outcome,
                FinalSuccessChance = finalSuccessChance,
                Grade = grade,
                Rewards = rewards,
                Injuries = injuries,
                Fatigue = fatigue,
                Logs = logs,
                ConsumedSeed = request.Seed
            };
        }

        private static float CalculatePartyPower(Party party, ICollection<ResolveLogEntry> logs)
        {
            if (party.Members.Count == 0)
            {
                logs.Add(new ResolveLogEntry("Party has no members. Fallback power=10."));
                return 10f;
            }

            float totalPower = 0f;
            foreach (var member in party.Members)
            {
                var s = member.Stats;

                // 상세 설계 기반 예시 수식:
                // 전투력 계수(공격/방어/마력/지원) + 탐사지표(탐지/기동/생존) + 안정성(사기/체력/피로)
                var combat = (s.AttackPower * 1.25f) + (s.DefensePower * 0.90f) + (s.MagicPower * 1.10f) + (s.SupportPower * 0.80f);
                var exploration = (s.Detection * 1.20f) + (s.Mobility * 1.00f) + (s.Survival * 1.05f);
                var stability = (s.Morale * 0.70f) + ((s.CurrentHp / (float)Math.Max(1, s.MaxHp)) * 25f) + (s.Stamina * 0.40f);

                var fatiguePenalty = 1f - (member.Fatigue / 140f);         // 피로 70이면 약 -50%
                var injuryPenalty = member.Injury.IsInjured ? (1f - (member.Injury.Severity * 0.12f)) : 1f;
                var levelBonus = 1f + (member.Level * 0.02f);

                var memberPower = (combat + exploration + stability) * fatiguePenalty * injuryPenalty * levelBonus;
                totalPower += Math.Max(1f, memberPower);
            }

            // 파티 시너지: 인원 증가에 따라 최대 +15%
            var synergy = 1f + MathF.Min(0.15f, party.Members.Count * 0.03f);
            return totalPower * synergy;
        }

        private static OutcomeGrade DetermineGrade(float successChance, float rollValue, float criticalSuccessBonus)
        {
            if (rollValue <= (successChance * 0.20f + criticalSuccessBonus))
            {
                return OutcomeGrade.CriticalSuccess;
            }

            if (rollValue <= successChance)
            {
                return OutcomeGrade.Success;
            }

            // 부분 성공: 실패 구간 중 하위 40%는 수습 성공
            var failMargin = rollValue - successChance;
            var failBand = 1f - successChance;
            if (failMargin <= failBand * 0.4f)
            {
                return OutcomeGrade.PartialSuccess;
            }

            return OutcomeGrade.Fail;
        }

        private static RewardPackage BuildRewardPackage(RewardPackage baseReward, OutcomeGrade grade)
        {
            var multiplier = grade switch
            {
                OutcomeGrade.CriticalSuccess => 1.50f,
                OutcomeGrade.Success => 1.00f,
                OutcomeGrade.PartialSuccess => 0.55f,
                _ => 0.10f
            };

            return new RewardPackage(
                gold: (int)MathF.Round(baseReward.Gold * multiplier),
                reputation: (int)MathF.Round(baseReward.Reputation * multiplier),
                items: baseReward.Items);
        }

        private static InjuryPackage BuildInjuryPackage(Party party, OutcomeGrade grade, Random random, bool enableInjurySimulation)
        {
            if (!enableInjurySimulation || grade == OutcomeGrade.CriticalSuccess)
            {
                return new InjuryPackage();
            }

            var injuries = new List<InjuryInfo>();
            var baseSeverity = grade switch
            {
                OutcomeGrade.Success => 1,
                OutcomeGrade.PartialSuccess => 2,
                _ => 3
            };

            foreach (var member in party.Members)
            {
                var resistFactor = 1f - (member.Stats.InjuryResist / 200f);
                var roll = (float)random.NextDouble();
                if (roll < 0.20f * resistFactor)
                {
                    var severity = Math.Clamp(baseSeverity + random.Next(-1, 2), 1, 5);
                    injuries.Add(new InjuryInfo(severity, $"{member.Name} suffered mission injury."));
                }
            }

            return new InjuryPackage(injuries);
        }

        private static FatiguePackage BuildFatiguePackage(Party party, QuestInstance quest, OutcomeGrade grade)
        {
            var missionLoad = quest.AssessedDifficulty * 4f;
            var gradeLoad = grade switch
            {
                OutcomeGrade.CriticalSuccess => 6f,
                OutcomeGrade.Success => 10f,
                OutcomeGrade.PartialSuccess => 14f,
                _ => 20f
            };

            var partyLoadReduction = MathF.Min(8f, party.Members.Count * 1.5f);
            var fatigueDelta = Math.Max(3, (int)MathF.Round(missionLoad + gradeLoad - partyLoadReduction));
            return new FatiguePackage(fatigueDelta);
        }
    }
}
