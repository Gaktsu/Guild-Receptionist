#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using GuildReceptionist.GameDesign.Data;
using GuildReceptionist.GameDesign.Domain;
using UnityEngine;

namespace GuildReceptionist.GameDesign
{
    public sealed class GameLauncher : MonoBehaviour
    {
        [Header("Source Data")]
        [SerializeField] private List<QuestData> questDataList = new();
        [SerializeField] private List<AdventurerData> adventurerDataList = new();

        [Header("World Bootstrap")]
        [SerializeField] private int startDayIndex;
        [SerializeField] [Range(0f, 1f)] private float weatherSeverity;
        [SerializeField] [Range(0f, 1f)] private float globalRiskLevel;

        private QuestBoard? _questBoard;
        private AdventurerRoster? _adventurerRoster;
        private IMissionResolver? _missionResolver;
        private AssignmentPlanner? _assignmentPlanner;
        private IQuestAssessmentService? _questAssessmentService;

        public QuestBoard? QuestBoard => _questBoard;
        public AdventurerRoster? AdventurerRoster => _adventurerRoster;
        public IMissionResolver? MissionResolver => _missionResolver;
        public AssignmentPlanner? AssignmentPlanner => _assignmentPlanner;
        public WorldStateSnapshot WorldSnapshot => CreateWorldSnapshot();

        private void Start()
        {
            var world = CreateWorldSnapshot();

            InitializeSystems();
            RegisterInitialAdventurers();
            RegisterInitialQuests(world);

            SimpleEventBus.Subscribe<MissionResolvedEvent>(OnMissionResolved);

            // 시스템 연결 확인용: 가능한 경우 첫 퀘스트를 자동 배정/해결하여 이벤트를 발행한다.
            TryRunInitialAssignmentAndResolution(world);
        }

        private void OnDestroy()
        {
            SimpleEventBus.Unsubscribe<MissionResolvedEvent>(OnMissionResolved);
        }

        private void InitializeSystems()
        {
            _questAssessmentService = new QuestAssessmentService();
            _questBoard = new QuestBoard(_questAssessmentService);
            _adventurerRoster = new AdventurerRoster();
            _missionResolver = new MissionResolver();
            _assignmentPlanner = new AssignmentPlanner(_missionResolver, minimumPartySize: 1, defaultDayIndex: startDayIndex);
        }

        private void RegisterInitialQuests(WorldStateSnapshot world)
        {
            if (_questBoard is null)
            {
                throw new InvalidOperationException("QuestBoard is not initialized.");
            }

            foreach (var data in questDataList)
            {
                if (data is null)
                {
                    continue;
                }

                data.Validate();
                var quest = BuildQuestInstance(data, world.DayIndex);
                _questBoard.AddQuest(quest, world);
            }
        }

        private void RegisterInitialAdventurers()
        {
            if (_adventurerRoster is null)
            {
                throw new InvalidOperationException("AdventurerRoster is not initialized.");
            }

            foreach (var data in adventurerDataList)
            {
                if (data is null)
                {
                    continue;
                }

                data.Validate();
                var adventurer = BuildAdventurerState(data);
                _adventurerRoster.Add(adventurer);
            }
        }

        private void TryRunInitialAssignmentAndResolution(WorldStateSnapshot world)
        {
            if (_questBoard is null || _adventurerRoster is null || _assignmentPlanner is null || _missionResolver is null)
            {
                return;
            }

            var quest = _questBoard.GetOpenQuests().FirstOrDefault();
            if (quest is null)
            {
                return;
            }

            var idleMembers = _adventurerRoster.GetIdleAdventurers();
            if (idleMembers.Count == 0)
            {
                return;
            }

            var party = new Party("party-bootstrap", idleMembers.ToList());
            if (!_assignmentPlanner.CanAssign(quest, party))
            {
                Debug.Log("[GameLauncher] Initial assignment skipped: constraints not satisfied.");
                return;
            }

            quest.AssignToParty(party.PartyId);
            quest.MarkInProgress();

            SimpleEventBus.Publish(new QuestAssignedEvent
            {
                Quest = quest,
                Party = party
            });

            var resolveResult = _missionResolver.Resolve(new ResolveRequest
            {
                Quest = quest,
                Party = party,
                World = world,
                DayIndex = world.DayIndex,
                Seed = Environment.TickCount,
                Options = new ResolveOptions(
                    enableTraitEffects: true,
                    enableInjurySimulation: true,
                    globalDifficultyMultiplier: 1f,
                    criticalSuccessBonus: 0f)
            });

            quest.Resolve(resolveResult.Outcome);

            SimpleEventBus.Publish(new MissionResolvedEvent
            {
                QuestId = resolveResult.Outcome.QuestId,
                PartyId = resolveResult.Outcome.PartyId,
                Grade = resolveResult.Outcome.Grade,
                Rewards = resolveResult.Outcome.Rewards,
                Injuries = resolveResult.Outcome.Injuries
            });
        }

        private void OnMissionResolved(MissionResolvedEvent evt)
        {
            var isSuccess = evt.Grade != OutcomeGrade.Fail;
            Debug.Log($"[MissionResolved] Quest={evt.QuestId}, Party={evt.PartyId}, Success={isSuccess}, Grade={evt.Grade}, Gold={evt.Rewards.Gold}, Rep={evt.Rewards.Reputation}, InjuryCount={evt.Injuries.Injuries.Count}");
        }

        private WorldStateSnapshot CreateWorldSnapshot()
        {
            return new WorldStateSnapshot(
                dayIndex: Math.Max(0, startDayIndex),
                weatherSeverity: weatherSeverity,
                globalRiskLevel: globalRiskLevel,
                locationRiskById: new Dictionary<string, float>(),
                activeWorldTags: Array.Empty<string>());
        }

        private static AdventurerState BuildAdventurerState(AdventurerData data)
        {
            var traits = data.DefaultTraits
                .Where(t => t != null)
                .Select(t => new TraitRuntime(t.name, 0f))
                .ToList();

            return new AdventurerState(
                adventurerId: string.IsNullOrWhiteSpace(data.AdventurerId) ? Guid.NewGuid().ToString("N") : data.AdventurerId,
                name: data.Name,
                role: data.BaseRole,
                level: 1,
                experience: 0,
                stats: data.BaseStats,
                traits: traits);
        }

        private static QuestInstance BuildQuestInstance(QuestData data, int dayIndex)
        {
            var questId = string.IsNullOrWhiteSpace(data.QuestId) ? Guid.NewGuid().ToString("N") : data.QuestId;
            var locationId = data.LocationProfile == null ? "unknown" : data.LocationProfile.name;
            var baseDifficulty = Math.Max(1f, data.RecommendedPower);
            var timeLimitDays = Math.Max(1, data.TimeLimitDays);
            var baseReward = new RewardPackage(
                gold: Math.Max(10, data.RecommendedPower * 12),
                reputation: Math.Max(1, (int)data.BaseRank + 1));

            return new QuestInstance(
                questId: questId,
                templateId: data.name,
                title: data.DisplayName,
                category: data.Category,
                baseDifficulty: baseDifficulty,
                issuedDay: dayIndex,
                expireDay: dayIndex + timeLimitDays,
                timeLimitDays: timeLimitDays,
                locationId: locationId,
                environmentTags: Array.Empty<string>(),
                baseReward: baseReward);
        }
    }
}
