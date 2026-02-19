#nullable enable
using System;
using System.Linq;
using GuildReceptionist.GameDesign.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace GuildReceptionist.GameDesign
{
    public sealed class DebugUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameLauncher gameLauncher = null!;
        [SerializeField] private Text resultText = null!;

        [Header("Debug Settings")]
        [SerializeField] private string debugPartyId = "debug-ui-party";
        [SerializeField] private int debugSeed = 2026;

        public void ExecuteTestMission()
        {
            if (!TryBuildSelection(out var quest, out var party, out var failReason))
            {
                WriteResult($"[DebugUI] Selection failed: {failReason}");
                return;
            }

            var planner = gameLauncher.AssignmentPlanner;
            var resolver = gameLauncher.MissionResolver;
            if (planner is null || resolver is null)
            {
                WriteResult("[DebugUI] Planner/Resolver is not initialized.");
                return;
            }

            if (!planner.CanAssign(quest, party))
            {
                WriteResult($"[DebugUI] Cannot assign. Quest={quest.QuestId}, PartyMembers={party.Members.Count}");
                return;
            }

            if (quest.State == QuestState.Pending)
            {
                quest.AssignToParty(party.PartyId);
                quest.MarkInProgress();
            }

            var resolveResult = resolver.Resolve(new ResolveRequest
            {
                Quest = quest,
                Party = party,
                World = gameLauncher.WorldSnapshot,
                DayIndex = gameLauncher.WorldSnapshot.DayIndex,
                Seed = debugSeed,
                Options = new ResolveOptions(
                    enableTraitEffects: true,
                    enableInjurySimulation: true,
                    globalDifficultyMultiplier: 1f,
                    criticalSuccessBonus: 0f)
            });

            quest.Resolve(resolveResult.Outcome);

            var summary =
                $"Mission Result\n" +
                $"Quest: {resolveResult.Outcome.QuestId}\n" +
                $"Party: {resolveResult.Outcome.PartyId}\n" +
                $"Grade: {resolveResult.Outcome.Grade}\n" +
                $"SuccessChance: {resolveResult.FinalSuccessChance:P1}\n" +
                $"Gold: {resolveResult.Rewards.Gold}, Rep: {resolveResult.Rewards.Reputation}\n" +
                $"Injuries: {resolveResult.Injuries.Injuries.Count}";

            WriteResult(summary);
        }

        private bool TryBuildSelection(out QuestInstance quest, out Party party, out string failReason)
        {
            quest = null!;
            party = null!;
            failReason = string.Empty;

            if (gameLauncher is null)
            {
                failReason = "GameLauncher reference is missing.";
                return false;
            }

            var board = gameLauncher.QuestBoard;
            var roster = gameLauncher.AdventurerRoster;
            if (board is null || roster is null)
            {
                failReason = "QuestBoard/AdventurerRoster is not initialized.";
                return false;
            }

            quest = board.GetOpenQuests().FirstOrDefault();
            if (quest is null)
            {
                failReason = "No open quest found.";
                return false;
            }

            var idleMembers = roster.GetIdleAdventurers();
            if (idleMembers.Count == 0)
            {
                failReason = "No idle adventurers available.";
                return false;
            }

            party = new Party(debugPartyId, idleMembers);
            return true;
        }

        private void WriteResult(string message)
        {
            Debug.Log(message);
            if (resultText != null)
            {
                resultText.text = message;
            }
        }
    }
}
