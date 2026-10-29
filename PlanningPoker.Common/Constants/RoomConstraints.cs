namespace PlanningPoker.Common.Constants;

public static class RoomConstraints
{
    public const int CountdownMinSeconds = 0;
    public const int CountdownMaxSeconds = 30;
    public const int MaxParticipantsMin = 1;
    public const int IdMaxLength = 100;
    public const int IdGenerationAttempts = 5;
    public const string HiddenVote = "hide";
    public const int VoteMarkMaxLength = 20;

    /// <summary>How often a reveal re-reads the member types when someone joined meanwhile, before it freezes with what it has.</summary>
    public const int TypeResolutionRounds = 3;
}
