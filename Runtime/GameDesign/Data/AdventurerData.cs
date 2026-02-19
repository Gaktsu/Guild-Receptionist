#nullable enable
using System.Collections.Generic;
using GuildReceptionist.GameDesign.Domain;
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/AdventurerData")]
    public sealed class AdventurerData : ScriptableObject
    {
        [SerializeField] private string adventurerId = string.Empty;
        [SerializeField] private string adventurerName = string.Empty;
        [SerializeField] private RoleType baseRole = RoleType.Utility;
        [SerializeField] private StatBlock baseStats;
        [SerializeField] private List<TraitData> defaultTraits = new();

        public string AdventurerId => adventurerId;
        public string Name => adventurerName;
        public RoleType BaseRole => baseRole;
        public StatBlock BaseStats => baseStats;
        public IReadOnlyList<TraitData> DefaultTraits => defaultTraits;

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
