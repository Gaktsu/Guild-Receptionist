#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildReceptionist.GameDesign.Data
{
    [CreateAssetMenu(menuName = "GuildReceptionist/GameDesign/RewardTable")]
    public sealed class RewardTable : ScriptableObject
    {
        [Serializable]
        public struct RewardEntry
        {
            [SerializeField] private int gold;
            [SerializeField] private int reputation;

            public RewardEntry(int gold, int reputation, List<string>? futureRewardKeys = null)
            {
                this.gold = Mathf.Max(0, gold);
                this.reputation = Mathf.Max(0, reputation);
                this.futureRewardKeys = futureRewardKeys ?? new List<string>();
            }

            // 확장 슬롯: 추후 아이템/토큰/조건부 보상 키를 붙일 수 있도록 유지
            [SerializeField] private List<string> futureRewardKeys;

            public int Gold => gold;
            public int Reputation => reputation;
            public IReadOnlyList<string> FutureRewardKeys => futureRewardKeys ?? Array.Empty<string>();
        }

        [Header("Default Reward")]
        [SerializeField] private int gold;
        [SerializeField] private int reputation;

        [Header("Optional Tier Entries")]
        [SerializeField] private List<RewardEntry> entries = new();

        public int Gold => gold;
        public int Reputation => reputation;
        public IReadOnlyList<RewardEntry> Entries => entries;

        public RewardEntry GetDefaultEntry()
        {
            return new RewardEntry(gold, reputation);
        }

        public void SetDefaultValues(int nextGold, int nextReputation)
        {
            gold = Mathf.Max(0, nextGold);
            reputation = Mathf.Max(0, nextReputation);
        }
    }
}
