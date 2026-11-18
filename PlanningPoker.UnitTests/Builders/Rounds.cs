using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Builders;

/// <summary>Builds the round and membership modules over the real mappers.</summary>
public static class Rounds
{
    public static RoomRound Create(TimeProvider? time = null)
    {
        return new RoomRound(new RevealedVotesEntityMapper(), new RoundResultEntityMapper(), time ?? TimeProvider.System);
    }

    public static RoomMembership Membership(TimeProvider? time = null)
    {
        return new RoomMembership(Create(time));
    }
}
