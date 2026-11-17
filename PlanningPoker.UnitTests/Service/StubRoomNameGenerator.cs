using PlanningPoker.Service.Services;

namespace PlanningPoker.UnitTests.Service;

/// <summary>Hands out the given ids in order, then repeats the last one.</summary>
public class StubRoomNameGenerator(params string[] ids) : IRoomNameGenerator
{
    public int Calls { get; private set; }

    public string Generate()
    {
        var id = ids[Math.Min(Calls, ids.Length - 1)];
        Calls++;
        return id;
    }
}
