using Microsoft.Extensions.Options;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

/// <summary>Builds a <see cref="PresenceService"/> over real mappers and the given repositories and room service.</summary>
public static class PresenceServiceFactory
{
    public static PresenceService Create(IRoomRepository rooms, IPersonRepository people, IRoomService roomService, TimeProvider time, PresenceOptions? options = null)
    {
        return new PresenceService(rooms, people, new PersonMapper(), new ExitRoomResultMapper(), roomService, time, Options.Create(options ?? new PresenceOptions()));
    }
}
