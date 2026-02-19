#nullable enable
using System;
using System.Collections.Generic;

namespace GuildReceptionist.GameDesign.Domain
{
    /// <summary>
    /// 개별 퀘스트의 런타임 인스턴스.
    /// QuestData(기획 데이터)를 기반으로 생성되며,
    /// 퀘스트의 상태 전이(Pending → Assigned → InProgress → Resolved → Archived),
    /// 난이도 평가, 파티 배정, 결과 기록 등 실시간 변화를 추적한다.
    /// </summary>
    public sealed class QuestInstance
    {
        // ── 기본 정보 ──

        /// <summary>퀘스트 인스턴스 고유 ID (런타임에서 생성된 고유 키)</summary>
        public string QuestId { get; }

        /// <summary>원본 기획 데이터(QuestData)의 템플릿 ID</summary>
        public string TemplateId { get; }

        /// <summary>퀘스트 제목 (UI 표시용)</summary>
        public string Title { get; }

        /// <summary>퀘스트 카테고리 (사냥, 호위, 탐험, 배달, 특수)</summary>
        public QuestCategory Category { get; }

        /// <summary>현재 퀘스트 진행 상태</summary>
        public QuestState State { get; private set; }

        // ── 난이도 & 리스크 ──

        /// <summary>기본 난이도 (기획 데이터에서 설정된 고정값)</summary>
        public float BaseDifficulty { get; }

        /// <summary>평가된(실제 적용되는) 난이도 – ApplyAssessment로 갱신 가능</summary>
        public float AssessedDifficulty { get; private set; }

        /// <summary>권장 퀘스트 등급 – 평가 후 결정됨</summary>
        public QuestRank RecommendedRank { get; private set; }

        /// <summary>위험 점수 – 파티 편성 시 참고하는 리스크 수치</summary>
        public float RiskScore { get; private set; }

        // ── 시간 관련 ──

        /// <summary>퀘스트가 발행된 게임 일차</summary>
        public int IssuedDay { get; }

        /// <summary>퀘스트 만료 일차 (이 날까지 미완료 시 자동 만료)</summary>
        public int ExpireDay { get; }

        /// <summary>퀘스트 수행 제한 일수</summary>
        public int TimeLimitDays { get; }

        // ── 장소 정보 ──

        /// <summary>퀘스트가 진행되는 장소 ID</summary>
        public string LocationId { get; }

        /// <summary>장소 환경 태그 목록 (예: "forest", "underground", "snow")</summary>
        public IReadOnlyList<string> EnvironmentTags { get; }

        // ── 보상 ──

        /// <summary>기본 보상 패키지 (기획 데이터 기반 고정 보상)</summary>
        public RewardPackage BaseReward { get; }

        /// <summary>예상 보상 패키지 – 평가 후 조정된 기대 보상</summary>
        public RewardPackage ExpectedReward { get; private set; }

        // ── 배정 & 결과 ──

        /// <summary>배정된 파티 ID (미배정 시 null)</summary>
        public string? AssignedPartyId { get; private set; }

        /// <summary>퀘스트 해결 결과 (미완료 시 null)</summary>
        public MissionOutcome? Resolution { get; private set; }

        /// <summary>데이터 버전 – 상태가 변경될 때마다 1씩 증가 (낙관적 동시성 제어용)</summary>
        public int Version { get; private set; }

        /// <summary>
        /// 생성자 – 퀘스트 인스턴스를 초기화한다.
        /// 초기 상태는 Pending, 평가 난이도는 기본 난이도와 동일, 파티 미배정.
        /// </summary>
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
            EnvironmentTags = environmentTags ?? Array.Empty<string>();
            BaseReward = baseReward;
            ExpectedReward = baseReward;
            AssignedPartyId = null;
            Resolution = null;
            Version = 0;
        }

        /// <summary>
        /// 퀘스트 난이도 평가 결과를 적용한다.
        /// 평가된 난이도, 권장 등급, 리스크 점수, 예상 보상을 갱신하고 버전을 증가시킨다.
        /// </summary>
        public void ApplyAssessment(float assessedDifficulty, QuestRank recommendedRank, float riskScore, RewardPackage expectedReward)
        {
            AssessedDifficulty = assessedDifficulty;
            RecommendedRank = recommendedRank;
            RiskScore = riskScore;
            ExpectedReward = expectedReward;
            Version++;
        }

        /// <summary>
        /// 퀘스트를 특정 파티에 배정한다.
        /// 상태를 Pending → Assigned로 전환한다.
        /// 유효하지 않은 상태 전이 시 InvalidOperationException을 발생시킨다.
        /// </summary>
        /// <param name="partyId">배정할 파티 ID</param>
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

        /// <summary>
        /// 퀘스트를 진행중(InProgress) 상태로 전환한다.
        /// Assigned → InProgress 전이만 허용된다.
        /// </summary>
        public void MarkInProgress()
        {
            if (!CanTransitionTo(QuestState.InProgress))
            {
                throw new InvalidOperationException($"Cannot transition {State} -> {QuestState.InProgress}");
            }

            State = QuestState.InProgress;
            Version++;
        }

        /// <summary>
        /// 퀘스트를 해결(Resolved) 상태로 전환하고 결과를 기록한다.
        /// InProgress → Resolved 전이만 허용된다.
        /// </summary>
        /// <param name="outcome">퀘스트 해결 결과 (성공 여부, 보상, 부상 등)</param>
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

        /// <summary>
        /// 퀘스트를 보관(Archived) 상태로 전환한다.
        /// Resolved → Archived 또는 Pending → Archived(만료/취소) 전이가 허용된다.
        /// </summary>
        public void Archive()
        {
            if (!CanTransitionTo(QuestState.Archived))
            {
                throw new InvalidOperationException($"Cannot transition {State} -> {QuestState.Archived}");
            }

            State = QuestState.Archived;
            Version++;
        }

        /// <summary>
        /// 지정된 상태로의 전이가 가능한지 검사한다.
        /// 허용되는 전이 경로:
        ///   Pending → Assigned, Assigned → InProgress,
        ///   InProgress → Resolved, Resolved → Archived,
        ///   Pending → Archived (만료/취소 처리)
        /// </summary>
        /// <param name="next">전이 대상 상태</param>
        /// <returns>전이 가능하면 true</returns>
        public bool CanTransitionTo(QuestState next)
        {
            return (State, next) switch
            {
                (QuestState.Pending, QuestState.Assigned) => true,
                (QuestState.Assigned, QuestState.InProgress) => true,
                (QuestState.InProgress, QuestState.Resolved) => true,
                (QuestState.Resolved, QuestState.Archived) => true,
                (QuestState.Pending, QuestState.Archived) => true,   // 만료·취소
                _ => false,
            };
        }
    }
}
