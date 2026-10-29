using PlanningPoker.Service.Models;

namespace PlanningPoker.UnitTests.Builders;

/// <summary>Builds the service results the hub and publisher tests feed into them.</summary>
public static class RoomResults
{
    public static JoinRoomResult Join(
        Person person,
        IReadOnlyList<Person>? people = null,
        bool resetCountdown = true,
        int countdownSeconds = 5)
    {
        return new JoinRoomResult
        {
            Room = new Room
            {
                Id = "room",
                OwnerGuid = person.Guid,
                VotingEnabled = true,
                Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = countdownSeconds, ShowFireworks = false, MaxParticipants = 9 }
            },
            Person = person,
            People = people ?? [person],
            VoteStatus = new VoteStatus { Votes = new Dictionary<Guid, string>(), HasEveryoneVoted = false },
            ResetCountdown = resetCountdown
        };
    }

    public static ExitRoomResult Exit(Person person, bool personLeft = true, bool roomDisposed = false, Person? newOwner = null)
    {
        return new ExitRoomResult { RoomId = "room", Person = person, PersonLeft = personLeft, RoomDisposed = roomDisposed, NewOwner = newOwner };
    }
}
