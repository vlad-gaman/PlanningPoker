namespace PlanningPoker.Common.Constants;

public static class RoomConstraints
{
    public const int CountdownMinSeconds = 0;
    public const int CountdownMaxSeconds = 30;
    public const int MaxParticipantsMin = 1;
    public const int AlarmMinutesMin = 1;
    public const int AlarmMinutesMax = 60;
    public const int AlarmMinutesDefault = 5;
    public const int IdMaxLength = 100;
    public const int IdGenerationAttempts = 5;
    public const string HiddenVote = "hide";
    public const int VoteMarkMaxLength = 20;

    /// <summary>How many revealed rounds a room remembers in its history; the oldest go first.</summary>
    public const int RoundHistoryLimit = 50;
}
