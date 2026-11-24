namespace PlanningPoker.Service.Models;

public class AlarmChangeResult
{
    public string RoomId { get; init; } = default!;

    public AlarmState Alarm { get; init; } = default!;
}
