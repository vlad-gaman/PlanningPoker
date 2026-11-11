using PlanningPoker.Repository.Repositories;
using PlanningPoker.UnitTests.Builders;
using Microsoft.Extensions.Options;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;
using PlanningPoker.Service.Validations;
using PlanningPoker.UnitTests.Fakes;

namespace PlanningPoker.UnitTests.Service;

/// <summary>Builds a <see cref="RoomService"/> over real mappers and the given repositories.</summary>
public static class RoomServiceFactory
{
    public static RoomService Create(IRoomRepository rooms, IPersonRepository people, IRoomNameGenerator? generator = null, TimeProvider? time = null, IPlanningPokerMetrics? metrics = null, PresenceOptions? presence = null)
    {
        return new RoomService(
            rooms,
            people,
            generator ?? new StubRoomNameGenerator("unused"),
            new RoomEntityMapper(new RoomConfigurationEntityMapper(), time ?? TimeProvider.System),
            new RoomMapper(new RoomConfigurationMapper()),
            new PersonMapper(),
            VoteStatusMappers.Create(),
            new RevealedVotesEntityMapper(),
            new JoinRoomResultMapper(),
            new ExitRoomResultMapper(),
            new RoomIdentifierValidation(),
            new RoomExistsValidation(rooms),
            new VoteMarkValidation(new CardSetService()),
            new RoomConfigurationEntityMapper(),
            new RoomConfigurationMapper(),
            new CardSetService(),
            metrics ?? new FakePlanningPokerMetrics(),
            time ?? TimeProvider.System,
            Options.Create(presence ?? new PresenceOptions()));
    }
}
