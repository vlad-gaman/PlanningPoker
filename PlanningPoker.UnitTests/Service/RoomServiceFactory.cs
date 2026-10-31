using PlanningPoker.Repository.Repositories;
using PlanningPoker.UnitTests.Builders;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.UnitTests.Service;

/// <summary>Builds a <see cref="RoomService"/> over real mappers and the given repositories.</summary>
public static class RoomServiceFactory
{
    public static RoomService Create(IRoomRepository rooms, IPersonRepository people, IRoomNameGenerator? generator = null)
    {
        return new RoomService(
            rooms,
            people,
            generator ?? new StubRoomNameGenerator("unused"),
            new RoomEntityMapper(new RoomConfigurationEntityMapper()),
            new RoomMapper(new RoomConfigurationMapper()),
            new PersonMapper(),
            VoteStatusMappers.Create(),
            new RevealedVotesEntityMapper(),
            new JoinRoomResultMapper(),
            new ExitRoomResultMapper(),
            new RoomIdentifierValidation(),
            new RoomExistsValidation(rooms),
            new VoteMarkValidation(new CardSetService()),
            new CardSetService());
    }
}
