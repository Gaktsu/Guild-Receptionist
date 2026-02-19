#nullable enable

namespace GuildReceptionist.GameDesign.Domain
{
    public readonly struct MissionResolvedEvent
    {
        public string QuestId { get; init; }
        public string PartyId { get; init; }
        public OutcomeGrade Grade { get; init; }
        public RewardPackage Rewards { get; init; }
        public InjuryPackage Injuries { get; init; }
    }

    public readonly struct QuestAssignedEvent
    {
        public QuestInstance Quest { get; init; }
        public Party Party { get; init; }
    }
}
