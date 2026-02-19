#nullable enable
using System.Collections.Generic;
using GuildReceptionist.GameDesign.Domain;
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    /// <summary>
    /// 모험가(Adventurer)의 기본 데이터를 정의하는 ScriptableObject.
    /// Unity 에디터에서 에셋으로 생성하여 모험가의 정적(기획) 데이터를 관리한다.
    /// 메뉴 경로: GuildReceptionist/GameDesign/AdventurerData
    /// </summary>
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/AdventurerData")]
    public sealed class AdventurerData : ScriptableObject
    {
        /// <summary>모험가 고유 식별자 (예: "ADV_001")</summary>
        [SerializeField] private string adventurerId = string.Empty;

        /// <summary>모험가 이름 (UI에 표시되는 이름)</summary>
        [SerializeField] private string adventurerName = string.Empty;

        /// <summary>모험가의 기본 역할(탱커, 딜러, 서포터, 정찰, 유틸리티)</summary>
        [SerializeField] private RoleType baseRole = RoleType.Utility;

        /// <summary>모험가의 초기 능력치 블록 (공격력, 방어력, HP 등)</summary>
        [SerializeField] private StatBlock baseStats;

        /// <summary>모험가가 기본적으로 보유하는 특성(Trait) 목록</summary>
        [SerializeField] private List<TraitData> defaultTraits = new();

        // ── 외부에서 읽기 전용으로 접근할 수 있는 프로퍼티들 ──

        /// <summary>모험가 고유 ID를 반환한다.</summary>
        public string AdventurerId => adventurerId;

        /// <summary>모험가 이름을 반환한다.</summary>
        public string Name => adventurerName;

        /// <summary>모험가의 기본 역할을 반환한다.</summary>
        public RoleType BaseRole => baseRole;

        /// <summary>모험가의 기본 능력치를 반환한다.</summary>
        public StatBlock BaseStats => baseStats;

        /// <summary>모험가의 기본 특성 목록을 읽기 전용 리스트로 반환한다.</summary>
        public IReadOnlyList<TraitData> DefaultTraits => defaultTraits;

        /// <summary>
        /// 데이터 유효성 검증 메서드.
        /// adventurerId가 비어있거나, MaxHp가 0 이하인 경우 경고 로그를 출력한다.
        /// 에디터에서 에셋 생성 후 잘못된 값 입력을 방지하기 위해 사용한다.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(adventurerId))
            {
                Debug.LogWarning("AdventurerData.adventurerId should not be empty.", this);
            }

            if (baseStats.MaxHp <= 0)
            {
                Debug.LogWarning("AdventurerData.baseStats.MaxHp should be greater than zero.", this);
            }
        }
    }
}
