#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    public sealed class AdventurerState
    {
        public string AdventurerId { get; }
        public string Name { get; }
        public RoleType Role { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public StatBlock Stats { get; private set; }
        public IReadOnlyList<TraitRuntime> Traits { get; }

        public int Fatigue { get; private set; }
        public InjuryStatus Injury { get; private set; }
        public AdventurerAvailability Availability { get; private set; }
        public string? LastQuestId { get; private set; }

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

        public void ApplyFatigue(int amount)
        {
            Fatigue = Math.Clamp(Fatigue + amount, 0, 100);
        }

        public void ApplyInjury(in InjuryInfo info)
        {
            Injury = new InjuryStatus(true, Math.Max(Injury.Severity, info.Severity));
            Availability = AdventurerAvailability.Recovery;
        }

        public void ApplyRewardExperience(int exp)
        {
            Experience = Math.Max(0, Experience + exp);
        }

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

            if (Fatigue == 0 && Injury.IsInjured && Injury.Severity <= 1)
            {
                Injury = new InjuryStatus(false, 0);
            }

            if (!Injury.IsInjured)
            {
                Availability = AdventurerAvailability.Idle;
            }
        }

        public void AssignToQuest(string questId)
        {
            if (!IsDeployable())
            {
                throw new InvalidOperationException("Adventurer is not deployable.");
            }

            LastQuestId = questId;
            Availability = AdventurerAvailability.Assigned;
        }

        public void ReleaseFromQuest()
        {
            LastQuestId = null;
            Availability = Injury.IsInjured ? AdventurerAvailability.Recovery : AdventurerAvailability.Idle;
        }

        public bool IsDeployable()
        {
            return Availability == AdventurerAvailability.Idle && Fatigue < 100 && !Injury.IsInjured;
        }
    }
}
