#nullable enable
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    /// <summary>
    /// 모험가 특성(Trait)의 기획 데이터를 정의하는 ScriptableObject.
    /// 각 특성은 이름, 설명, 그리고 다양한 스탯 보너스를 가지며,
    /// AdventurerData의 DefaultTraits 목록에서 참조되어 모험가에게 부여된다.
    /// 메뉴 경로: GuildReceptionist/GameDesign/TraitData
    /// </summary>
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/TraitData")]
    public sealed class TraitData : ScriptableObject
    {
        // ── 기본 정보 ──

        /// <summary>특성 고유 식별자 (예: "TRAIT_BERSERKER")</summary>
        [SerializeField] private string traitId = string.Empty;

        /// <summary>특성 이름 (UI에 표시되는 이름, 예: "광전사")</summary>
        [SerializeField] private string traitName = string.Empty;

        /// <summary>특성에 대한 상세 설명 (툴팁 등에서 사용)</summary>
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;

        // ── 전투 스탯 보너스 ──

        /// <summary>공격력 보너스 (양수: 증가, 음수: 감소)</summary>
        [Header("전투 스탯 보너스")]
        [SerializeField] private int attackBonus;

        /// <summary>방어력 보너스</summary>
        [SerializeField] private int defenseBonus;

        /// <summary>마법력 보너스</summary>
        [SerializeField] private int magicBonus;

        /// <summary>지원력 보너스 (힐·버프 계열)</summary>
        [SerializeField] private int supportBonus;

        // ── 탐험 스탯 보너스 ──

        /// <summary>탐지력 보너스 (함정·보물 발견)</summary>
        [Header("탐험 스탯 보너스")]
        [SerializeField] private int detectionBonus;

        /// <summary>기동력 보너스 (이동 속도·회피)</summary>
        [SerializeField] private int mobilityBonus;

        /// <summary>생존력 보너스 (환경 저항·야영 능력)</summary>
        [SerializeField] private int survivalBonus;

        /// <summary>사기 보너스 (전투 의지·도주 확률 영향)</summary>
        [SerializeField] private int moraleBonus;

        // ── 내구 스탯 보너스 ──

        /// <summary>최대 HP 보너스</summary>
        [Header("내구 스탯 보너스")]
        [SerializeField] private int maxHpBonus;

        /// <summary>스태미나 보너스 (행동 자원)</summary>
        [SerializeField] private int staminaBonus;

        /// <summary>스트레스 저항 보너스</summary>
        [SerializeField] private int stressResistBonus;

        /// <summary>부상 저항 보너스</summary>
        [SerializeField] private int injuryResistBonus;

        /// <summary>운반 능력 보너스</summary>
        [SerializeField] private int carryCapacityBonus;

        // ── 특수 계수 ──

        /// <summary>
        /// 피로도 소모율 변동 계수.
        /// 1.0 = 기본 소모, 0.8 = 20% 감소, 1.3 = 30% 증가.
        /// 퀘스트 수행 시 피로도 계산에 곱해져 적용된다.
        /// </summary>
        [Header("특수 계수")]
        [SerializeField] private float fatigueRateModifier = 1.0f;

        // ══════════════════════════════════════════════════════════
        //  읽기 전용 프로퍼티
        // ══════════════════════════════════════════════════════════

        // ── 기본 정보 ──
        /// <summary>특성 고유 ID를 반환한다.</summary>
        public string TraitId => traitId;

        /// <summary>특성 이름을 반환한다.</summary>
        public string TraitName => traitName;

        /// <summary>특성 상세 설명을 반환한다.</summary>
        public string Description => description;

        // ── 전투 스탯 보너스 ──
        /// <summary>공격력 보너스를 반환한다.</summary>
        public int AttackBonus => attackBonus;

        /// <summary>방어력 보너스를 반환한다.</summary>
        public int DefenseBonus => defenseBonus;

        /// <summary>마법력 보너스를 반환한다.</summary>
        public int MagicBonus => magicBonus;

        /// <summary>지원력 보너스를 반환한다.</summary>
        public int SupportBonus => supportBonus;

        // ── 탐험 스탯 보너스 ──
        /// <summary>탐지력 보너스를 반환한다.</summary>
        public int DetectionBonus => detectionBonus;

        /// <summary>기동력 보너스를 반환한다.</summary>
        public int MobilityBonus => mobilityBonus;

        /// <summary>생존력 보너스를 반환한다.</summary>
        public int SurvivalBonus => survivalBonus;

        /// <summary>사기 보너스를 반환한다.</summary>
        public int MoraleBonus => moraleBonus;

        // ── 내구 스탯 보너스 ──
        /// <summary>최대 HP 보너스를 반환한다.</summary>
        public int MaxHpBonus => maxHpBonus;

        /// <summary>스태미나 보너스를 반환한다.</summary>
        public int StaminaBonus => staminaBonus;

        /// <summary>스트레스 저항 보너스를 반환한다.</summary>
        public int StressResistBonus => stressResistBonus;

        /// <summary>부상 저항 보너스를 반환한다.</summary>
        public int InjuryResistBonus => injuryResistBonus;

        /// <summary>운반 능력 보너스를 반환한다.</summary>
        public int CarryCapacityBonus => carryCapacityBonus;

        // ── 특수 계수 ──
        /// <summary>피로도 소모율 변동 계수를 반환한다.</summary>
        public float FatigueRateModifier => fatigueRateModifier;

        /// <summary>
        /// 데이터 유효성 검증 메서드.
        /// traitId나 traitName이 비어있거나,
        /// fatigueRateModifier가 0 이하이면 경고 로그를 출력한다.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(traitId))
            {
                Debug.LogWarning("TraitData.traitId should not be empty.", this);
            }

            if (string.IsNullOrWhiteSpace(traitName))
            {
                Debug.LogWarning("TraitData.traitName should not be empty.", this);
            }

            if (fatigueRateModifier <= 0f)
            {
                Debug.LogWarning("TraitData.fatigueRateModifier should be greater than zero.", this);
            }
        }
    }
}
