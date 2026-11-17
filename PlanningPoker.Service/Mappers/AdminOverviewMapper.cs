using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class AdminOverviewMapper : IAdminOverviewMapper
{
    public AdminOverview Map(
        IReadOnlyList<AdminRoom> rooms,
        IReadOnlyList<IssueEntry> issues,
        TimeSpan uptime,
        int roomsRestoredAtStart,
        int roomsSweptLast24Hours,
        PersistenceStatusSnapshot storeWrite,
        int failedLoginsLast24Hours)
    {
        var people = rooms.SelectMany(room => room.People).ToList();
        return new AdminOverview
        {
            Rooms = rooms,
            Issues = issues,
            Stats = new AdminStats
            {
                Rooms = rooms.Count,
                VotingRooms = rooms.Count(room => room.State == AdminRoomState.Voting),
                RevealedRooms = rooms.Count(room => room.State == AdminRoomState.Revealed),
                RestoredRooms = rooms.Count(room => room.State == AdminRoomState.Restored),
                PeopleOnline = people.Count(person => person.Status == AdminPersonStatus.Online),
                PeopleOffgrid = people.Count(person => person.Status == AdminPersonStatus.Offgrid),
                Uptime = uptime,
                RoomsRestoredAtStart = roomsRestoredAtStart,
                AbandonedRoomsSweptLast24Hours = roomsSweptLast24Hours,
                LastStoreWrite = new AdminStoreWrite { At = storeWrite.LastWriteAt, Succeeded = storeWrite.LastWriteSucceeded, FailedRooms = storeWrite.FailedRooms },
                FailedLoginsLast24Hours = failedLoginsLast24Hours
            }
        };
    }
}
