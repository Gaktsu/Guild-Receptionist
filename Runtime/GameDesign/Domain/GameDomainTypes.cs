#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    public enum QuestState { Pending, Assigned, InProgress, Resolved, Archived }
    public enum QuestRank { F, E, D, C, B, A, S }
    public enum OutcomeGrade { CriticalSuccess, Success, PartialSuccess, Fail }
    public enum RoleType { Tank, Dealer, Support, Scout, Utility }
    public enum AdventurerAvailability { Idle, Assigned, InProgress, Recovery }
    public enum QuestCategory { Hunt, Escort, Explore, Delivery, Special }

    [Serializable]
    public readonly struct RewardPackage
    {
        public readonly int Gold;
        public readonly int Reputation;
        public readonly IReadOnlyList<string> Items;

        public RewardPackage(int gold, int reputation, IReadOnlyList<string>? items = null)
        {
            Gold = gold;
            Reputation = reputation;
            Items = items ?? Array.Empty<string>();
        }
    }

    [Serializable]
    public readonly struct InjuryInfo
    {
        public readonly int Severity;
        public readonly string Description;

        public InjuryInfo(int severity, string description)
        {
            Severity = severity;
            Description = description;
        }
    }

    [Serializable]
    public readonly struct InjuryPackage
    {
        public readonly IReadOnlyList<InjuryInfo> Injuries;

        public InjuryPackage(IReadOnlyList<InjuryInfo>? injuries = null)
        {
            Injuries = injuries ?? Array.Empty<InjuryInfo>();
        }
    }

    [Serializable]
    public readonly struct FatiguePackage
    {
        public readonly int FatigueDelta;

        public FatiguePackage(int fatigueDelta)
        {
            FatigueDelta = fatigueDelta;
        }
    }

    [Serializable]
    public readonly struct ResolveLogEntry
    {
        public readonly string Message;

        public ResolveLogEntry(string message)
        {
            Message = message;
        }
    }

    [Serializable]
    public readonly struct ResolveOptions
    {
        public readonly bool EnableTraitEffects;
        public readonly bool EnableInjurySimulation;
        public readonly float GlobalDifficultyMultiplier;
        public readonly float CriticalSuccessBonus;

        public ResolveOptions(bool enableTraitEffects, bool enableInjurySimulation, float globalDifficultyMultiplier, float criticalSuccessBonus)
        {
            EnableTraitEffects = enableTraitEffects;
            EnableInjurySimulation = enableInjurySimulation;
            GlobalDifficultyMultiplier = globalDifficultyMultiplier;
            CriticalSuccessBonus = criticalSuccessBonus;
        }
    }

    [Serializable]
    public readonly struct MissionOutcome
    {
        public string QuestId { get; init; }
        public string PartyId { get; init; }
        public bool IsSuccess { get; init; }
        public OutcomeGrade Grade { get; init; }
        public float SuccessChance { get; init; }
        public float RollValue { get; init; }
        public RewardPackage Rewards { get; init; }
        public InjuryPackage Injuries { get; init; }
        public int ResolvedDay { get; init; }
    }

    [Serializable]
    public readonly struct StatBlock
    {
        public int AttackPower { get; init; }
        public int DefensePower { get; init; }
        public int MagicPower { get; init; }
        public int SupportPower { get; init; }
        public int Detection { get; init; }
        public int Mobility { get; init; }
        public int Survival { get; init; }
        public int Morale { get; init; }
        public int MaxHp { get; init; }
        public int CurrentHp { get; init; }
        public int Stamina { get; init; }
        public int StressResist { get; init; }
        public int InjuryResist { get; init; }
        public int CarryCapacity { get; init; }
    }

    [Serializable]
    public readonly struct InjuryStatus
    {
        public readonly bool IsInjured;
        public readonly int Severity;

        public InjuryStatus(bool isInjured, int severity)
        {
            IsInjured = isInjured;
            Severity = severity;
        }
    }

    [Serializable]
    public readonly struct RecoveryPackage
    {
        public readonly int FatigueRecovery;
        public readonly int HpRecovery;

        public RecoveryPackage(int fatigueRecovery, int hpRecovery)
        {
            FatigueRecovery = fatigueRecovery;
            HpRecovery = hpRecovery;
        }
    }

    [Serializable]
    public readonly struct TraitRuntime
    {
        public readonly string TraitId;
        public readonly float Magnitude;

        public TraitRuntime(string traitId, float magnitude)
        {
            TraitId = traitId;
            Magnitude = magnitude;
        }
    }

    [Serializable]
    public readonly struct WorldStateSnapshot
    {
        public readonly int DayIndex;
        public readonly float WeatherSeverity;
        public readonly float GlobalRiskLevel;
        public readonly IReadOnlyDictionary<string, float> LocationRiskById;
        public readonly IReadOnlyList<string> ActiveWorldTags;

        public WorldStateSnapshot(
            int dayIndex,
            float weatherSeverity = 0f,
            float globalRiskLevel = 0f,
            IReadOnlyDictionary<string, float>? locationRiskById = null,
            IReadOnlyList<string>? activeWorldTags = null)
        {
            DayIndex = dayIndex;
            WeatherSeverity = weatherSeverity;
            GlobalRiskLevel = globalRiskLevel;
            LocationRiskById = locationRiskById ?? new Dictionary<string, float>();
            ActiveWorldTags = activeWorldTags ?? Array.Empty<string>();
        }
    }

}
