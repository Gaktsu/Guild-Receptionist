#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuildReceptionist.GameDesign.Domain
{
    public sealed class QuestBoard
    {
        private readonly IQuestAssessmentService _questAssessmentService;
        private readonly List<QuestInstance> _activeQuests = new();

        public QuestBoard(IQuestAssessmentService questAssessmentService)
        {
            _questAssessmentService = questAssessmentService ?? throw new ArgumentNullException(nameof(questAssessmentService));
        }

        public void AddQuest(QuestInstance quest, WorldStateSnapshot world)
        {
            if (quest is null)
            {
                throw new ArgumentNullException(nameof(quest));
            }

            if (_activeQuests.Any(q => q.QuestId == quest.QuestId))
            {
                throw new InvalidOperationException($"Quest already exists on board: {quest.QuestId}");
            }

            _questAssessmentService.ApplyAssessment(quest, world);
            _activeQuests.Add(quest);
        }

        public void AddQuests(IEnumerable<QuestInstance> quests, WorldStateSnapshot world)
        {
            if (quests is null)
            {
                throw new ArgumentNullException(nameof(quests));
            }

            foreach (var quest in quests)
            {
                AddQuest(quest, world);
            }
        }

        public QuestInstance? FindById(string questId)
        {
            return _activeQuests.FirstOrDefault(q => q.QuestId == questId);
        }

        public IReadOnlyList<QuestInstance> GetOpenQuests()
        {
            return _activeQuests
                .Where(q => q.State is QuestState.Pending or QuestState.Assigned or QuestState.InProgress)
                .ToList();
        }

        public IReadOnlyList<QuestInstance> GetAllQuests()
        {
            return _activeQuests;
        }

        public bool RemoveQuest(string questId)
        {
            var quest = FindById(questId);
            if (quest is null)
            {
                return false;
            }

            _activeQuests.Remove(quest);
            return true;
        }

        public void ReassessOpenQuests(WorldStateSnapshot world)
        {
            foreach (var quest in GetOpenQuests())
            {
                _questAssessmentService.ApplyAssessment(quest, world);
            }
        }
    }
}
