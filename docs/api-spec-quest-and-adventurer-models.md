# 단계 4: 클래스 상세 명세서 (API 수준)

> 목적: 구현 전에 클래스의 필드/메서드 시그니처를 고정해 팀 간 해석 차이를 줄인다.

## 0) 공통 설계 원칙
- **데이터/로직 분리**: `QuestInstance`, `AdventurerState`는 상태 보유 중심, 계산은 서비스(`MissionResolver`) 담당
- **불변 입력/출력 DTO 우선**: `Resolve()`의 입출력은 가능한 불변 구조로 정의
- **명시적 타입 사용**: `float` 난이도, `enum` 결과 등급, `struct` 스탯 블록으로 의미 분리

---

## 1) QuestInstance 상세 명세

### 1-1. 책임
- 당일 활성 퀘스트의 **런타임 상태**를 보유
- 상태 전이(`Pending → Assigned → InProgress → Resolved → Archived`)의 단일 진입점 제공
- 결과 계산 로직은 가지지 않음 (SRP)

### 1-2. 필드(변수) 명세

| 필드명 | 타입 | 접근 수준 | 설명 |
|---|---|---|---|
| `QuestId` | `string` | `public get; private set;` | 인스턴스 식별자 (세션 내 유니크) |
| `TemplateId` | `string` | `public get; private set;` | 원본 `QuestData` 식별자 |
| `Title` | `string` | `public get; private set;` | UI 표기용 이름 |
| `Category` | `QuestCategory` | `public get; private set;` | 토벌/호위/탐사 등 |
| `State` | `QuestState` | `public get; private set;` | 현재 라이프사이클 상태 |
| `BaseDifficulty` | `float` | `public get; private set;` | 원본 난이도(데이터 기준) |
| `AssessedDifficulty` | `float` | `public get; private set;` | 환경 보정 후 난이도 |
| `RecommendedRank` | `QuestRank` | `public get; private set;` | 평가 서비스 추천 등급 |
| `RiskScore` | `float` | `public get; private set;` | 실패/손실 리스크 점수(0~1 또는 0~100) |
| `TimeLimitDays` | `int` | `public get; private set;` | 마감까지 남은 일수 기준 값 |
| `IssuedDay` | `int` | `public get; private set;` | 발행 일차 |
| `ExpireDay` | `int` | `public get; private set;` | 만료 일차 |
| `LocationId` | `string` | `public get; private set;` | 지역 프로필 참조 키 |
| `EnvironmentTags` | `IReadOnlyList<string>` | `public get; private set;` | 날씨/지형/사건 태그 |
| `BaseReward` | `RewardPackage` | `public get; private set;` | 기본 보상(골드/아이템/평판) |
| `ExpectedReward` | `RewardPackage` | `public get; private set;` | 현재 평가 기준 기대 보상 |
| `AssignedPartyId` | `string?` | `public get; private set;` | 배정 파티 ID (없으면 null) |
| `Resolution` | `MissionOutcome?` | `public get; private set;` | 결과 확정 데이터 |
| `Version` | `int` | `public get; private set;` | 동시성/갱신 충돌 방지 버전 |

### 1-3. 메서드 시그니처

```csharp
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

    public void ApplyAssessment(float assessedDifficulty, QuestRank recommendedRank, float riskScore, RewardPackage expectedReward);
    public void AssignToParty(string partyId);
    public void MarkInProgress();
    public void Resolve(MissionOutcome outcome);
    public void Archive();
    public bool CanTransitionTo(QuestState next);
}
```

---

## 2) MissionResolver 상세 명세

### 2-1. 책임
- `QuestInstance + Party + WorldState`를 받아 성공률/결과를 계산
- 보상/부상 패키지 생성
- 결과를 `MissionOutcome`으로 반환
- UI 직접 갱신 금지 (상위 계층에서 이벤트 발행)

### 2-2. Resolve() 입력 파라미터 구조

```csharp
public readonly struct ResolveRequest
{
    public QuestInstance Quest { get; init; }
    public Party Party { get; init; }
    public WorldStateSnapshot World { get; init; }
    public int DayIndex { get; init; }
    public int Seed { get; init; }                    // 재현 가능한 RNG
    public ResolveOptions Options { get; init; }      // 난이도 규칙/튜닝 옵션
}
```

`ResolveOptions` 예시 필드:
- `bool EnableTraitEffects`
- `bool EnableInjurySimulation`
- `float GlobalDifficultyMultiplier`
- `float CriticalSuccessBonus`

### 2-3. Resolve() 반환 데이터 구조

```csharp
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
```

### 2-4. MissionOutcome 구조

```csharp
public readonly struct MissionOutcome
{
    public string QuestId { get; init; }
    public string PartyId { get; init; }
    public bool IsSuccess { get; init; }
    public OutcomeGrade Grade { get; init; }          // CriticalSuccess / Success / Partial / Fail
    public float SuccessChance { get; init; }         // 최종 계산 확률
    public float RollValue { get; init; }             // RNG 샘플 값
    public RewardPackage Rewards { get; init; }
    public InjuryPackage Injuries { get; init; }
    public int ResolvedDay { get; init; }
}
```

### 2-5. MissionResolver 메서드 시그니처

```csharp
public interface IMissionResolver
{
    ResolveResult Resolve(in ResolveRequest request);
}

public sealed class MissionResolver : IMissionResolver
{
    public MissionResolver(
        ISuccessChanceCalculator successChanceCalculator,
        IOutcomeRoller outcomeRoller,
        IRewardPolicy rewardPolicy,
        IInjuryPolicy injuryPolicy,
        ITraitEffectResolver traitEffectResolver);

    public ResolveResult Resolve(in ResolveRequest request);
}
```

---

## 3) AdventurerState 상세 명세

### 3-1. 책임
- 모험가 개인의 현재 상태(스탯/특성/피로/부상)를 관리
- 배정/결과 계산에서 참조 가능한 **정규화된 스탯 API** 제공

### 3-2. 관리할 구체 스탯 목록

#### 핵심 전투/탐사 스탯
- `AttackPower` (공격력)
- `DefensePower` (방어력)
- `MagicPower` (마력/주문 효율)
- `SupportPower` (치유/버프 효율)
- `Detection` (탐지력)
- `Mobility` (기동력)
- `Survival` (생존력)
- `Morale` (사기/압박 저항)

#### 보조 운영 스탯
- `MaxHp`
- `CurrentHp`
- `Stamina`
- `StressResist`
- `InjuryResist`
- `CarryCapacity`

### 3-3. 필드(변수) 명세

| 필드명 | 타입 | 접근 수준 | 설명 |
|---|---|---|---|
| `AdventurerId` | `string` | `public get; private set;` | 유닛 식별자 |
| `Name` | `string` | `public get; private set;` | 표시 이름 |
| `Role` | `RoleType` | `public get; private set;` | 기본 역할군 |
| `Level` | `int` | `public get; private set;` | 성장 단계 |
| `Experience` | `int` | `public get; private set;` | 누적 경험치 |
| `Stats` | `StatBlock` | `public get; private set;` | 현재 스탯 묶음 |
| `Traits` | `IReadOnlyList<TraitRuntime>` | `public get; private set;` | 특성 목록 |
| `Fatigue` | `int` | `public get; private set;` | 피로도(0~100) |
| `Injury` | `InjuryStatus` | `public get; private set;` | 부상 상태 |
| `Availability` | `AdventurerAvailability` | `public get; private set;` | 대기/원정중/치료중 |
| `LastQuestId` | `string?` | `public get; private set;` | 마지막 배정 퀘스트 |

### 3-4. StatBlock 구조

```csharp
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
```

### 3-5. AdventurerState 메서드 시그니처

```csharp
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

    public void ApplyFatigue(int amount);
    public void ApplyInjury(in InjuryInfo info);
    public void ApplyRewardExperience(int exp);
    public void Recover(in RecoveryPackage recovery);
    public void AssignToQuest(string questId);
    public void ReleaseFromQuest();
    public bool IsDeployable();
}
```

---

## 4) 타입/열거형 요약

```csharp
public enum QuestState { Pending, Assigned, InProgress, Resolved, Archived }
public enum QuestRank { F, E, D, C, B, A, S }
public enum OutcomeGrade { CriticalSuccess, Success, PartialSuccess, Fail }
public enum RoleType { Tank, Dealer, Support, Scout, Utility }
public enum AdventurerAvailability { Idle, Assigned, InProgress, Recovery }
```

---

## 5) API 사용 예시 (요약)

```csharp
var request = new ResolveRequest
{
    Quest = questInstance,
    Party = selectedParty,
    World = worldSnapshot,
    DayIndex = currentDay,
    Seed = rngSeed,
    Options = resolveOptions
};

ResolveResult result = missionResolver.Resolve(in request);
questInstance.Resolve(result.Outcome);
```

---

## 6) 검증 체크포인트
- `QuestInstance`는 계산 함수 없이 상태/전이만 담당하는가?
- `ResolveRequest/ResolveResult`만으로 재현 가능한 시뮬레이션이 가능한가?
- `AdventurerState` 스탯이 배정/결과 계산 요구를 모두 커버하는가?
- UI가 `MissionResolver`를 직접 참조하지 않고 결과 DTO/이벤트만 소비하는가?
