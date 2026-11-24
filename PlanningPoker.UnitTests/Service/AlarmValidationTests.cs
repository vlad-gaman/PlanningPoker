using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Validations;

namespace PlanningPoker.UnitTests.Service;

public class AlarmValidationTests
{
    private static readonly DateTimeOffset EndsAt = new(2026, 1, 1, 12, 5, 0, TimeSpan.Zero);
    private readonly AlarmValidation _validation = new();

    private static RoomEntity Room(bool enabled = true, DateTimeOffset? endsAt = null, bool elapsed = false)
    {
        return new RoomEntity { Configuration = new RoomConfigurationEntity { AlarmEnabled = enabled }, AlarmEndsAt = endsAt, AlarmElapsed = elapsed };
    }

    private static string Code(PlanningPoker.Common.Models.ServiceResponse response) => Assert.Single(response.Errors).Code;

    [Fact]
    public void CanStart_OnlyWhenOnAndIdle()
    {
        Assert.True(_validation.CanStart(Room()).IsSuccess);
        Assert.Equal(ErrorCodes.AlarmDisabled, Code(_validation.CanStart(Room(enabled: false))));
        Assert.Equal(ErrorCodes.AlarmNotIdle, Code(_validation.CanStart(Room(endsAt: EndsAt))));
        Assert.Equal(ErrorCodes.AlarmNotIdle, Code(_validation.CanStart(Room(endsAt: EndsAt, elapsed: true))));
    }

    [Fact]
    public void CanCancel_OnlyWhileRunning()
    {
        Assert.True(_validation.CanCancel(Room(endsAt: EndsAt)).IsSuccess);
        Assert.Equal(ErrorCodes.AlarmNotRunning, Code(_validation.CanCancel(Room())));
        Assert.Equal(ErrorCodes.AlarmNotRunning, Code(_validation.CanCancel(Room(endsAt: EndsAt, elapsed: true))));
    }

    [Fact]
    public void CanReset_OnlyOnceElapsed()
    {
        Assert.True(_validation.CanReset(Room(endsAt: EndsAt, elapsed: true)).IsSuccess);
        Assert.Equal(ErrorCodes.AlarmNotElapsed, Code(_validation.CanReset(Room())));
        Assert.Equal(ErrorCodes.AlarmNotElapsed, Code(_validation.CanReset(Room(endsAt: EndsAt))));
    }

    [Fact]
    public void CanElapse_OnlyTheRunThatEndsAtTheGivenTime()
    {
        Assert.True(_validation.CanElapse(Room(endsAt: EndsAt), EndsAt).IsSuccess);
        Assert.Equal(ErrorCodes.AlarmNotRunning, Code(_validation.CanElapse(Room(endsAt: EndsAt), EndsAt.AddMinutes(1))));
        Assert.Equal(ErrorCodes.AlarmNotRunning, Code(_validation.CanElapse(Room(endsAt: EndsAt, elapsed: true), EndsAt)));
        Assert.Equal(ErrorCodes.AlarmNotRunning, Code(_validation.CanElapse(Room(), EndsAt)));
    }

    [Fact]
    public void DiffersOnlyInAlarmFrom_IgnoresTheAlarmFieldsAndNothingElse()
    {
        var stored = new RoomConfigurationEntity { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = true, MaxParticipants = 9, AlarmEnabled = true, AlarmMinutes = 5 };
        var alarmChanged = new RoomConfigurationEntity { CardSet = "linear", CountdownSeconds = 5, ShowFireworks = true, MaxParticipants = 9, AlarmEnabled = false, AlarmMinutes = 30 };
        var cardsChanged = new RoomConfigurationEntity { CardSet = "fibonacci", CountdownSeconds = 5, ShowFireworks = true, MaxParticipants = 9 };

        Assert.True(stored.DiffersOnlyInAlarmFrom(alarmChanged));
        Assert.False(stored.DiffersOnlyInAlarmFrom(cardsChanged));
    }
}
