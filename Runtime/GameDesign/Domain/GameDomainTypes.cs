#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    // ══════════════════════════════════════════════════════════════
    //  열거형(Enum) 정의
    // ══════════════════════════════════════════════════════════════

    /// <summary>퀘스트의 진행 상태를 나타내는 열거형</summary>
    /// <remarks>
    /// Pending(대기) → Assigned(배정) → InProgress(진행중) → Resolved(완료) → Archived(보관)
    /// Pending → Archived 로의 직접 전환도 허용된다 (만료·취소 등).
    /// </remarks>
    public enum QuestState { Pending, Assigned, InProgress, Resolved, Archived }

    /// <summary>퀘스트 난이도 등급 (F가 최저, S가 최고)</summary>
    public enum QuestRank { F, E, D, C, B, A, S }

    /// <summary>퀘스트 결과 등급 – 대성공, 성공, 부분 성공, 실패</summary>
    public enum OutcomeGrade { CriticalSuccess, Success, PartialSuccess, Fail }

    /// <summary>모험가의 역할 유형 (탱커, 딜러, 서포터, 정찰, 유틸리티)</summary>
    public enum RoleType { Tank, Dealer, Support, Scout, Utility }

    /// <summary>모험가의 가용 상태 (대기, 배정됨, 진행중, 회복중)</summary>
    public enum AdventurerAvailability { Idle, Assigned, InProgress, Recovery }

    /// <summary>퀘스트 카테고리 (사냥, 호위, 탐험, 배달, 특수)</summary>
    public enum QuestCategory { Hunt, Escort, Explore, Delivery, Special }

    // ══════════════════════════════════════════════════════════════
    //  구조체(Struct) 정의
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 퀘스트 클리어 보상 패키지.
    /// 골드, 명성, 획득 아이템 ID 목록을 포함한다.
    /// </summary>
    [Serializable]
    public readonly struct RewardPackage
    {
        /// <summary>획득 골드</summary>
        public readonly int Gold;

        /// <summary>획득 명성</summary>
        public readonly int Reputation;

        /// <summary>획득 아이템 ID 목록</summary>
        public readonly IReadOnlyList<string> Items;

        public RewardPackage(int gold, int reputation, IReadOnlyList<string>? items = null)
        {
            Gold = gold;
            Reputation = reputation;
            Items = items ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// 개별 부상 정보.
    /// 심각도(Severity)와 설명(Description)을 가진다.
    /// </summary>
    [Serializable]
    public readonly struct InjuryInfo
    {
        /// <summary>부상 심각도 (숫자가 클수록 심각)</summary>
        public readonly int Severity;

        /// <summary>부상에 대한 텍스트 설명</summary>
        public readonly string Description;

        public InjuryInfo(int severity, string description)
        {
            Severity = severity;
            Description = description;
        }
    }

    /// <summary>
    /// 부상 패키지 – 퀘스트 결과로 발생하는 부상 목록을 묶어서 전달한다.
    /// </summary>
    [Serializable]
    public readonly struct InjuryPackage
    {
        /// <summary>발생한 부상 정보 리스트</summary>
        public readonly IReadOnlyList<InjuryInfo> Injuries;

        public InjuryPackage(IReadOnlyList<InjuryInfo>? injuries = null)
        {
            Injuries = injuries ?? Array.Empty<InjuryInfo>();
        }
    }

    /// <summary>
    /// 피로도 변화 패키지.
    /// 퀘스트 수행 후 모험가에게 적용할 피로 증감값을 담는다.
    /// </summary>
    [Serializable]
    public readonly struct FatiguePackage
    {
        /// <summary>피로도 변화량 (양수: 증가, 음수: 감소)</summary>
        public readonly int FatigueDelta;

        public FatiguePackage(int fatigueDelta)
        {
            FatigueDelta = fatigueDelta;
        }
    }

    /// <summary>
    /// 퀘스트 진행 로그 항목.
    /// 퀘스트 해결(Resolve) 과정에서 기록되는 단일 이벤트 메시지.
    /// </summary>
    [Serializable]
    public readonly struct ResolveLogEntry
    {
        /// <summary>로그 메시지 내용</summary>
        public readonly string Message;

        public ResolveLogEntry(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// 퀘스트 해결(Resolve) 시 적용할 옵션 설정.
    /// 특성 효과, 부상 시뮬레이션, 난이도 배율, 대성공 보너스 등을 제어한다.
    /// </summary>
    [Serializable]
    public readonly struct ResolveOptions
    {
        /// <summary>특성(Trait) 효과 적용 여부</summary>
        public readonly bool EnableTraitEffects;

        /// <summary>부상 시뮬레이션 활성화 여부</summary>
        public readonly bool EnableInjurySimulation;

        /// <summary>전역 난이도 배율 (1.0 = 기본)</summary>
        public readonly float GlobalDifficultyMultiplier;

        /// <summary>대성공 시 추가 보너스 배율</summary>
        public readonly float CriticalSuccessBonus;

        public ResolveOptions(bool enableTraitEffects, bool enableInjurySimulation, float globalDifficultyMultiplier, float criticalSuccessBonus)
        {
            EnableTraitEffects = enableTraitEffects;
            EnableInjurySimulation = enableInjurySimulation;
            GlobalDifficultyMultiplier = globalDifficultyMultiplier;
            CriticalSuccessBonus = criticalSuccessBonus;
        }
    }

    /// <summary>
    /// 퀘스트 해결 결과(Mission Outcome).
    /// 퀘스트 성공 여부, 등급, 보상, 부상, 판정 확률 등 최종 결과를 담는다.
    /// </summary>
    [Serializable]
    public readonly struct MissionOutcome
    {
        /// <summary>해당 퀘스트 ID</summary>
        public string QuestId { get; init; }

        /// <summary>참여한 파티 ID</summary>
        public string PartyId { get; init; }

        /// <summary>성공 여부</summary>
        public bool IsSuccess { get; init; }

        /// <summary>결과 등급 (대성공 / 성공 / 부분성공 / 실패)</summary>
        public OutcomeGrade Grade { get; init; }

        /// <summary>계산된 성공 확률 (0.0 ~ 1.0)</summary>
        public float SuccessChance { get; init; }

        /// <summary>실제 판정 주사위 값 (0.0 ~ 1.0)</summary>
        public float RollValue { get; init; }

        /// <summary>지급될 보상 패키지</summary>
        public RewardPackage Rewards { get; init; }

        /// <summary>발생한 부상 패키지</summary>
        public InjuryPackage Injuries { get; init; }

        /// <summary>해결된 게임 일차(Day)</summary>
        public int ResolvedDay { get; init; }
    }

    /// <summary>
    /// 모험가의 능력치 블록.
    /// 공격력, 방어력, 마법력, 지원력, 탐지, 기동, 생존, 사기, HP, 스태미나,
    /// 스트레스 저항, 부상 저항, 운반 능력 등 전투·탐험 관련 수치를 모두 포함한다.
    /// </summary>
    [Serializable]
    public readonly struct StatBlock
    {
        /// <summary>공격력</summary>
        public int AttackPower { get; init; }

        /// <summary>방어력</summary>
        public int DefensePower { get; init; }

        /// <summary>마법력</summary>
        public int MagicPower { get; init; }

        /// <summary>지원력 (힐·버프 계열)</summary>
        public int SupportPower { get; init; }

        /// <summary>탐지력 (함정·보물 발견)</summary>
        public int Detection { get; init; }

        /// <summary>기동력 (이동 속도·회피)</summary>
        public int Mobility { get; init; }

        /// <summary>생존력 (환경 저항·야영 능력)</summary>
        public int Survival { get; init; }

        /// <summary>사기 (전투 의지·도주 확률 영향)</summary>
        public int Morale { get; init; }

        /// <summary>최대 HP</summary>
        public int MaxHp { get; init; }

        /// <summary>현재 HP</summary>
        public int CurrentHp { get; init; }

        /// <summary>스태미나 (행동 자원)</summary>
        public int Stamina { get; init; }

        /// <summary>스트레스 저항력</summary>
        public int StressResist { get; init; }

        /// <summary>부상 저항력</summary>
        public int InjuryResist { get; init; }

        /// <summary>운반 능력 (아이템 휴대 한계)</summary>
        public int CarryCapacity { get; init; }
    }

    /// <summary>
    /// 부상 상태 – 부상 여부와 심각도를 나타내는 값 타입.
    /// </summary>
    [Serializable]
    public readonly struct InjuryStatus
    {
        /// <summary>현재 부상 중인지 여부</summary>
        public readonly bool IsInjured;

        /// <summary>부상 심각도 (0 = 미부상, 숫자가 클수록 심각)</summary>
        public readonly int Severity;

        public InjuryStatus(bool isInjured, int severity)
        {
            IsInjured = isInjured;
            Severity = severity;
        }
    }

    /// <summary>
    /// 회복 패키지 – 모험가에게 적용할 피로 회복량과 HP 회복량을 담는다.
    /// </summary>
    [Serializable]
    public readonly struct RecoveryPackage
    {
        /// <summary>피로 회복량</summary>
        public readonly int FatigueRecovery;

        /// <summary>HP 회복량</summary>
        public readonly int HpRecovery;

        public RecoveryPackage(int fatigueRecovery, int hpRecovery)
        {
            FatigueRecovery = fatigueRecovery;
            HpRecovery = hpRecovery;
        }
    }

    /// <summary>
    /// 런타임 특성(Trait) – 모험가가 보유한 특성의 ID와 효과 크기.
    /// </summary>
    [Serializable]
    public readonly struct TraitRuntime
    {
        /// <summary>특성 고유 ID</summary>
        public readonly string TraitId;

        /// <summary>특성 효과의 크기 (배율 또는 고정값)</summary>
        public readonly float Magnitude;

        public TraitRuntime(string traitId, float magnitude)
        {
            TraitId = traitId;
            Magnitude = magnitude;
        }
    }

    /// <summary>
    /// 파티(Party) – 모험가 그룹을 나타내는 클래스.
    /// 현재는 파티 ID만 보유하며, 추후 멤버 관리 등으로 확장 가능하다.
    /// </summary>
    [Serializable]
    public readonly struct WorldStateSnapshot
    {
        /// <summary>현재 게임 진행 일차 (0-indexed)</summary>
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
