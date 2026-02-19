#nullable enable
using UnityEngine;

namespace GuildReceptionist.GameDesign.Domain
{
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/TraitData")]
    public sealed class TraitData : ScriptableObject
    {
        [Header("Core Combat Bonuses")]
        [SerializeField] private int attackBonus;
        [SerializeField] private int defenseBonus;
        [SerializeField] private int magicBonus;
        [SerializeField] private int supportBonus;

        [Header("Exploration Bonuses")]
        [SerializeField] private int detectionBonus;
        [SerializeField] private int mobilityBonus;
        [SerializeField] private int survivalBonus;
        [SerializeField] private int moraleBonus;

        [Header("Sustain Bonuses")]
        [SerializeField] private int maxHpBonus;
        [SerializeField] private int currentHpBonus;
        [SerializeField] private int staminaBonus;
        [SerializeField] private int stressResistBonus;
        [SerializeField] private int injuryResistBonus;
        [SerializeField] private int carryCapacityBonus;

        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;
        public int MagicBonus => magicBonus;
        public int SupportBonus => supportBonus;

        public int DetectionBonus => detectionBonus;
        public int MobilityBonus => mobilityBonus;
        public int SurvivalBonus => survivalBonus;
        public int MoraleBonus => moraleBonus;

        public int MaxHpBonus => maxHpBonus;
        public int CurrentHpBonus => currentHpBonus;
        public int StaminaBonus => staminaBonus;
        public int StressResistBonus => stressResistBonus;
        public int InjuryResistBonus => injuryResistBonus;
        public int CarryCapacityBonus => carryCapacityBonus;
    }
}
