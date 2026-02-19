#nullable enable
using GuildReceptionist.GameDesign.Domain;
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    /// <summary>
    /// 퀘스트(Quest)의 기본 기획 데이터를 정의하는 ScriptableObject.
    /// Unity 에디터에서 에셋으로 생성하여 퀘스트의 정적 데이터를 관리한다.
    /// 메뉴 경로: GuildReceptionist/GameDesign/QuestData
    /// </summary>
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/QuestData")]
    public sealed class QuestData : ScriptableObject
    {
        /// <summary>퀘스트 고유 식별자 (예: "QUEST_001")</summary>
        [SerializeField] private string questId = string.Empty;

        /// <summary>퀘스트 표시 이름 (UI에 보여줄 제목)</summary>
        [SerializeField] private string displayName = string.Empty;

        /// <summary>퀘스트 카테고리 (사냥, 호위, 탐험, 배달, 특수)</summary>
        [SerializeField] private QuestCategory category = QuestCategory.Hunt;

        /// <summary>퀘스트 기본 등급 (F ~ S 랭크)</summary>
        [SerializeField] private QuestRank baseRank = QuestRank.F;

        /// <summary>권장 전투력 수치 – 파티 편성 시 참고 기준</summary>
        [SerializeField] private int recommendedPower;

        /// <summary>보상 테이블 참조 – 클리어 시 지급할 보상 정의</summary>
        [SerializeField] private RewardTable rewardTable = null!;

        /// <summary>장소 프로필 데이터 – 퀘스트가 진행되는 위치 정보</summary>
        [SerializeField] private LocationProfileData locationProfile = null!;

        /// <summary>제한 일수 – 퀘스트를 완료해야 하는 기한(일 단위)</summary>
        [SerializeField] private int timeLimitDays = 1;

        // ── 외부에서 읽기 전용으로 접근할 수 있는 프로퍼티들 ──

        /// <summary>퀘스트 고유 ID를 반환한다.</summary>
        public string QuestId => questId;

        /// <summary>퀘스트 표시 이름을 반환한다.</summary>
        public string DisplayName => displayName;

        /// <summary>퀘스트 카테고리를 반환한다.</summary>
        public QuestCategory Category => category;

        /// <summary>퀘스트 기본 등급을 반환한다.</summary>
        public QuestRank BaseRank => baseRank;

        /// <summary>권장 전투력을 반환한다.</summary>
        public int RecommendedPower => recommendedPower;

        /// <summary>보상 테이블 참조를 반환한다.</summary>
        public RewardTable RewardTable => rewardTable;

        /// <summary>장소 프로필 데이터를 반환한다.</summary>
        public LocationProfileData LocationProfile => locationProfile;

        /// <summary>제한 일수를 반환한다.</summary>
        public int TimeLimitDays => timeLimitDays;

        /// <summary>
        /// 데이터 유효성 검증 메서드.
        /// questId가 비어있거나, timeLimitDays가 0 이하이면 경고 로그를 출력한다.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                Debug.LogWarning("QuestData.questId should not be empty.", this);
            }

            if (timeLimitDays <= 0)
            {
                Debug.LogWarning("QuestData.timeLimitDays should be greater than zero.", this);
            }
        }
    }
}
