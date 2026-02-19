#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    /// <summary>
    /// 지역(Location) 프로필 데이터를 정의하는 ScriptableObject.
    /// 퀘스트가 진행되는 장소의 이름, 난이도 가중치, 환경 태그 등을 관리한다.
    /// QuestData에서 참조하여 퀘스트-장소 연결에 사용된다.
    /// 메뉴 경로: GuildReceptionist/GameDesign/LocationProfileData
    /// </summary>
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/LocationProfileData")]
    public sealed class LocationProfileData : ScriptableObject
    {
        /// <summary>지역 고유 식별자 (예: "LOC_DARK_FOREST")</summary>
        [SerializeField] private string locationId = string.Empty;

        /// <summary>지역 이름 (UI에 표시되는 이름, 예: "어둠의 숲")</summary>
        [SerializeField] private string locationName = string.Empty;

        /// <summary>
        /// 난이도 가중치 – 이 지역에서 진행되는 퀘스트의 난이도에 곱해지는 배율.
        /// 1.0이 기본이며, 값이 높을수록 해당 지역이 더 위험하다.
        /// </summary>
        [SerializeField] private float difficultyMultiplier = 1.0f;

        /// <summary>
        /// 기본 환경 태그 목록 – 이 지역의 환경적 특징을 나타낸다.
        /// 예: "forest", "underground", "snow", "volcanic"
        /// 퀘스트 인스턴스 생성 시 EnvironmentTags로 전달되어
        /// 파티 편성·난이도 평가 등에 활용된다.
        /// </summary>
        [SerializeField] private List<string> defaultEnvironmentTags = new();

        // ── 외부에서 읽기 전용으로 접근할 수 있는 프로퍼티들 ──

        /// <summary>지역 고유 ID를 반환한다.</summary>
        public string LocationId => locationId;

        /// <summary>지역 이름을 반환한다.</summary>
        public string LocationName => locationName;

        /// <summary>난이도 가중치를 반환한다.</summary>
        public float DifficultyMultiplier => difficultyMultiplier;

        /// <summary>기본 환경 태그 목록을 읽기 전용 리스트로 반환한다.</summary>
        public IReadOnlyList<string> DefaultEnvironmentTags => defaultEnvironmentTags;

        /// <summary>
        /// 데이터 유효성 검증 메서드.
        /// locationId가 비어있거나, difficultyMultiplier가 0 이하이면 경고 로그를 출력한다.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                Debug.LogWarning("LocationProfileData.locationId should not be empty.", this);
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                Debug.LogWarning("LocationProfileData.locationName should not be empty.", this);
            }

            if (difficultyMultiplier <= 0f)
            {
                Debug.LogWarning("LocationProfileData.difficultyMultiplier should be greater than zero.", this);
            }
        }
    }
}
