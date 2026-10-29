namespace PlanningPoker.Controller.Constants;

public static class HubEvents
{
    public const string PersonJoined = "PersonJoined";
    public const string PersonExited = "PersonExited";
    public const string OwnershipTransferred = "OwnershipTransferred";
    public const string RoomDisposed = "RoomDisposed";
    public const string CountdownReset = "CountdownReset";
    public const string CountdownUpdate = "CountdownUpdate";
    public const string VoteCast = "VoteCast";
    public const string VoteStatusUpdate = "VoteStatusUpdate";
    public const string VotesShown = "VotesShown";
    public const string VotesCleared = "VotesCleared";
}
