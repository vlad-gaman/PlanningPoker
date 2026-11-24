using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class AlarmResponseMapper(TimeProvider? time = null) : IAlarmResponseMapper
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public AlarmResponse Map(AlarmState alarm)
    {
        return new AlarmResponse
        {
            Status = alarm.Status.ToString().ToLowerInvariant(),
            EndsAt = alarm.EndsAt,
            RemainingMilliseconds = alarm.Status == AlarmStatus.Running ? RemainingMilliseconds(alarm.EndsAt!.Value) : null
        };
    }

    private long RemainingMilliseconds(DateTimeOffset endsAt)
    {
        return Math.Max(0, (long)(endsAt - _time.GetUtcNow()).TotalMilliseconds);
    }
}
