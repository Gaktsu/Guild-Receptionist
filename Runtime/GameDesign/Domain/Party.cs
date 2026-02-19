#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildReceptionist.GameDesign.Domain
{
    [Serializable]
    public sealed class Party
    {
        private readonly List<AdventurerState> _members;

        public string PartyId { get; }
        public IReadOnlyList<AdventurerState> Members => _members;

        public Party(string partyId, IReadOnlyList<AdventurerState>? members = null)
        {
            if (string.IsNullOrWhiteSpace(partyId))
            {
                throw new ArgumentException("PartyId is required.", nameof(partyId));
            }

            PartyId = partyId;
            _members = members is null ? new List<AdventurerState>() : new List<AdventurerState>(members);
        }

        public bool AddMember(AdventurerState member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (_members.Any(m => m.AdventurerId == member.AdventurerId))
            {
                return false;
            }

            _members.Add(member);
            return true;
        }

        public bool RemoveMember(string adventurerId)
        {
            var index = _members.FindIndex(m => m.AdventurerId == adventurerId);
            if (index < 0)
            {
                return false;
            }

            _members.RemoveAt(index);
            return true;
        }

        public float CalculateAverageCondition()
        {
            if (_members.Count == 0)
            {
                return 0f;
            }

            float total = 0f;
            foreach (var member in _members)
            {
                var hpRatio = member.Stats.MaxHp <= 0 ? 0f : member.Stats.CurrentHp / (float)member.Stats.MaxHp;
                var fatigueFactor = 1f - (member.Fatigue / 100f);
                var injuryFactor = member.Injury.IsInjured ? MathF.Max(0f, 1f - member.Injury.Severity * 0.2f) : 1f;
                total += Math.Clamp(hpRatio * fatigueFactor * injuryFactor, 0f, 1f);
            }

            return total / _members.Count;
        }


        public float GetAverageFatigue()
        {
            if (_members.Count == 0)
            {
                return 0f;
            }

            float totalFatigue = 0f;
            foreach (var member in _members)
            {
                totalFatigue += member.Fatigue;
            }

            return totalFatigue / _members.Count;
        }

        public StatBlock CalculateTotalStats()
        {
            var total = new StatBlock();

            foreach (var s in _members.Select(m => m.Stats))
            {
                total = new StatBlock
                {
                    AttackPower = total.AttackPower + s.AttackPower,
                    DefensePower = total.DefensePower + s.DefensePower,
                    MagicPower = total.MagicPower + s.MagicPower,
                    SupportPower = total.SupportPower + s.SupportPower,
                    Detection = total.Detection + s.Detection,
                    Mobility = total.Mobility + s.Mobility,
                    Survival = total.Survival + s.Survival,
                    Morale = total.Morale + s.Morale,
                    MaxHp = total.MaxHp + s.MaxHp,
                    CurrentHp = total.CurrentHp + s.CurrentHp,
                    Stamina = total.Stamina + s.Stamina,
                    StressResist = total.StressResist + s.StressResist,
                    InjuryResist = total.InjuryResist + s.InjuryResist,
                    CarryCapacity = total.CarryCapacity + s.CarryCapacity
                };
            }

            return total;
        }
    }
}
