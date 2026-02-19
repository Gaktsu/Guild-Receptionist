#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    /// <summary>
    /// 모험가의 런타임 상태를 관리하는 클래스.
    /// AdventurerData(기획 데이터)를 기반으로 생성되며,
    /// 게임 진행 중 레벨, 경험치, 피로도, 부상, 퀘스트 배정 등 변화하는 상태를 추적한다.
    /// </summary>
    public sealed class AdventurerState
    {
        /// <summary>모험가 고유 식별자</summary>
        public string AdventurerId { get; }

        /// <summary>모험가 이름</summary>
        public string Name { get; }

        /// <summary>모험가의 역할 (탱커, 딜러, 서포터, 정찰, 유틸리티)</summary>
        public RoleType Role { get; }

        /// <summary>현재 레벨</summary>
        public int Level { get; private set; }

        /// <summary>누적 경험치</summary>
        public int Experience { get; private set; }

        /// <summary>현재 능력치 블록 (회복 등으로 HP가 변동될 수 있음)</summary>
        public StatBlock Stats { get; private set; }

        /// <summary>런타임에 활성화된 특성(Trait) 목록</summary>
        public IReadOnlyList<TraitRuntime> Traits { get; }

        /// <summary>피로도 (0 ~ 100, 100이면 출전 불가)</summary>
        public int Fatigue { get; private set; }

        /// <summary>부상 상태 (부상 여부 및 심각도)</summary>
        public InjuryStatus Injury { get; private set; }

        /// <summary>현재 가용 상태 (대기, 배정됨, 진행중, 회복중)</summary>
        public AdventurerAvailability Availability { get; private set; }

        /// <summary>마지막으로 참여한 퀘스트 ID (없으면 null)</summary>
        public string? LastQuestId { get; private set; }

        /// <summary>
        /// 생성자 – 모험가 상태를 초기화한다.
        /// 초기 피로도는 0, 부상 없음, 가용 상태는 Idle로 설정된다.
        /// </summary>
        public AdventurerState(
            string adventurerId,
            string name,
            RoleType role,
            int level,
            int experience,
            StatBlock stats,
            IReadOnlyList<TraitRuntime> traits)
        {
            AdventurerId = adventurerId;
            Name = name;
            Role = role;
            Level = level;
            Experience = experience;
            Stats = stats;
            Traits = traits;
            Fatigue = 0;
            Injury = new InjuryStatus(false, 0);
            Availability = AdventurerAvailability.Idle;
            LastQuestId = null;
        }

        /// <summary>
        /// 피로도를 증감시킨다. 결과값은 0 ~ 100 사이로 클램프된다.
        /// </summary>
        /// <param name="amount">증가(양수) 또는 감소(음수) 피로 수치</param>
        public void ApplyFatigue(int amount)
        {
            Fatigue = Math.Clamp(Fatigue + amount, 0, 100);
        }

        /// <summary>
        /// 부상을 적용한다. 기존 부상보다 심각도가 높으면 갱신되며,
        /// 가용 상태를 Recovery(회복중)로 전환한다.
        /// </summary>
        /// <param name="info">부상 정보 (심각도 및 설명)</param>
        public void ApplyInjury(in InjuryInfo info)
        {
            Injury = new InjuryStatus(true, Math.Max(Injury.Severity, info.Severity));
            Availability = AdventurerAvailability.Recovery;
        }

        /// <summary>
        /// 퀘스트 보상으로 받은 경험치를 적용한다. 결과값은 0 이상으로 보장된다.
        /// </summary>
        /// <param name="exp">획득 경험치 (음수도 가능)</param>
        public void ApplyRewardExperience(int exp)
        {
            Experience = Math.Max(0, Experience + exp);
        }

        /// <summary>
        /// 회복을 적용한다.
        /// - 피로도를 감소시키고, HP를 회복한다.
        /// - 피로도가 0이고 부상 심각도가 1 이하이면 부상을 완치 처리한다.
        /// - 부상이 없으면 가용 상태를 Idle로 복원한다.
        /// </summary>
        /// <param name="recovery">회복 패키지 (피로 회복량, HP 회복량)</param>
        public void Recover(in RecoveryPackage recovery)
        {
            Fatigue = Math.Clamp(Fatigue - recovery.FatigueRecovery, 0, 100);
            var nextHp = Math.Clamp(Stats.CurrentHp + recovery.HpRecovery, 0, Stats.MaxHp);
            Stats = new StatBlock
            {
                AttackPower = Stats.AttackPower,
                DefensePower = Stats.DefensePower,
                MagicPower = Stats.MagicPower,
                SupportPower = Stats.SupportPower,
                Detection = Stats.Detection,
                Mobility = Stats.Mobility,
                Survival = Stats.Survival,
                Morale = Stats.Morale,
                MaxHp = Stats.MaxHp,
                CurrentHp = nextHp,
                Stamina = Stats.Stamina,
                StressResist = Stats.StressResist,
                InjuryResist = Stats.InjuryResist,
                CarryCapacity = Stats.CarryCapacity
            };

            // 피로도가 완전히 해소되고, 부상 심각도가 낮으면 부상 완치 처리
            if (Fatigue == 0 && Injury.IsInjured && Injury.Severity <= 1)
            {
                Injury = new InjuryStatus(false, 0);
            }

            // 부상이 없으면 다시 대기(Idle) 상태로 전환
            if (!Injury.IsInjured)
            {
                Availability = AdventurerAvailability.Idle;
            }
        }

        /// <summary>
        /// 모험가를 특정 퀘스트에 배정한다.
        /// 출전 가능 상태가 아니면 InvalidOperationException을 발생시킨다.
        /// </summary>
        /// <param name="questId">배정할 퀘스트 ID</param>
        public void AssignToQuest(string questId)
        {
            if (!IsDeployable())
            {
                throw new InvalidOperationException("Adventurer is not deployable.");
            }

            LastQuestId = questId;
            Availability = AdventurerAvailability.Assigned;
        }

        /// <summary>
        /// 퀘스트 배정을 해제한다.
        /// 부상 중이면 Recovery 상태로, 아니면 Idle 상태로 전환한다.
        /// </summary>
        public void ReleaseFromQuest()
        {
            LastQuestId = null;
            Availability = Injury.IsInjured ? AdventurerAvailability.Recovery : AdventurerAvailability.Idle;
        }

        /// <summary>
        /// 모험가가 퀘스트에 출전 가능한지 판단한다.
        /// 조건: Idle 상태 + 피로도 100 미만 + 부상 없음
        /// </summary>
        /// <returns>출전 가능하면 true</returns>
        public bool IsDeployable()
        {
            return Availability == AdventurerAvailability.Idle && Fatigue < 100 && !Injury.IsInjured;
        }
    }
}
