#nullable enable
using GuildReceptionist.GameDesign.Domain;
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/QuestData")]
    public sealed class QuestData : ScriptableObject
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private QuestCategory category = QuestCategory.Hunt;
        [SerializeField] private QuestRank baseRank = QuestRank.F;
        [SerializeField] private int recommendedPower;
        [SerializeField] private RewardTable rewardTable = null!;
        [SerializeField] private LocationProfileData locationProfile = null!;
        [SerializeField] private int timeLimitDays = 1;

        public string QuestId => questId;
        public string DisplayName => displayName;
        public QuestCategory Category => category;
        public QuestRank BaseRank => baseRank;
        public int RecommendedPower => recommendedPower;
        public RewardTable RewardTable => rewardTable;
        public LocationProfileData LocationProfile => locationProfile;
        public int TimeLimitDays => timeLimitDays;

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
