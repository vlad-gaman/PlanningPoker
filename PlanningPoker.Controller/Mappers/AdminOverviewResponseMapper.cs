using PlanningPoker.Common.Models;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class AdminOverviewResponseMapper : IAdminOverviewResponseMapper
{
    public AdminOverviewResponse Map(AdminOverview overview)
    {
        var stats = overview.Stats;
        return new AdminOverviewResponse
        {
            Stats = new AdminStatsResponse
            {
                Rooms = new AdminRoomCountsResponse
                {
                    Total = stats.Rooms,
                    Voting = stats.VotingRooms,
                    Revealed = stats.RevealedRooms,
                    Restored = stats.RestoredRooms
                },
                People = new AdminPeopleCountsResponse { Online = stats.PeopleOnline, Offgrid = stats.PeopleOffgrid },
                UptimeSeconds = (long)stats.Uptime.TotalSeconds,
                RoomsRestoredAtStart = stats.RoomsRestoredAtStart,
                AbandonedRoomsSweptLast24Hours = stats.AbandonedRoomsSweptLast24Hours,
                LastStoreWrite = new AdminStoreWriteResponse
                {
                    At = stats.LastStoreWrite.At,
                    Succeeded = stats.LastStoreWrite.Succeeded,
                    FailedRooms = stats.LastStoreWrite.FailedRooms
                },
                FailedLoginsLast24Hours = stats.FailedLoginsLast24Hours
            },
            Rooms = [.. overview.Rooms.Select(room => new AdminRoomResponse
            {
                Id = room.Id,
                State = room.State.ToString().ToLowerInvariant(),
                CreatedAt = room.CreatedAt,
                LastActivityAt = room.LastActivityAt,
                People = [.. room.People.Select(person => new AdminPersonResponse
                {
                    Id = person.Id,
                    Name = person.Name,
                    PersonType = person.PersonType,
                    IsOwner = person.IsOwner,
                    Status = person.Status.ToString().ToLowerInvariant(),
                    LastSeenAt = person.LastSeenAt
                })]
            })],
            // The client address of a failed sign-in stays in the log; the page gets the message only.
            Issues = [.. overview.Issues.Select(issue => new AdminIssueResponse
            {
                Id = issue.Id,
                Time = issue.Time,
                Level = issue.Level.ToString().ToLowerInvariant(),
                Source = issue.Source,
                Message = issue.Message
            })]
        };
    }
}
