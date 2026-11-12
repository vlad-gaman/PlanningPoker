using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Mappers;

public class AdminRoomMapper : IAdminRoomMapper
{
    public AdminRoom Map(RoomEntity room, IPersonRepository people)
    {
        return new AdminRoom
        {
            Id = room.Id,
            State = StateOf(room),
            CreatedAt = room.CreatedAt,
            LastActivityAt = room.LastActivityAt,
            People = [.. room.MemberGuids.Select(guid => MapPerson(room, guid, people.Get(guid))).OfType<AdminPerson>()]
        };
    }

    private static AdminRoomState StateOf(RoomEntity room)
    {
        if (room.AwaitingReturn)
        {
            return AdminRoomState.Restored;
        }

        return room.VotingEnabled ? AdminRoomState.Voting : AdminRoomState.Revealed;
    }

    private static AdminPerson? MapPerson(RoomEntity room, Guid guid, PersonEntity? person)
    {
        if (person is null)
        {
            return null;
        }

        return new AdminPerson
        {
            Id = guid,
            Name = person.Name,
            PersonType = person.PersonType,
            IsOwner = room.OwnerGuid == guid,
            Status = room.OffgridGuids.Contains(guid) ? AdminPersonStatus.Offgrid : AdminPersonStatus.Online,
            LastSeenAt = room.DroppedAt.TryGetValue(guid, out var droppedAt) ? droppedAt : null
        };
    }
}
