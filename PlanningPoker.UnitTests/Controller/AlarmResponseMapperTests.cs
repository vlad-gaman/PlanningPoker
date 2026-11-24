using PlanningPoker.Common.Models;
using Microsoft.Extensions.Time.Testing;
using PlanningPoker.Controller.Constants;
using PlanningPoker.Controller.Mappers;
using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.UnitTests.Builders;

namespace PlanningPoker.UnitTests.Controller;

public class AlarmResponseMapperTests
{
    private static readonly DateTimeOffset EndsAt = new(2026, 1, 1, 12, 5, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(AlarmStatus.Idle, "idle")]
    [InlineData(AlarmStatus.Running, "running")]
    [InlineData(AlarmStatus.Elapsed, "elapsed")]
    public void Map_NamesTheStatusInLowerCase_AndCarriesTheEndTime(AlarmStatus status, string name)
    {
        var response = new AlarmResponseMapper().Map(new AlarmState { Status = status, EndsAt = status == AlarmStatus.Idle ? null : EndsAt });

        Assert.Equal(name, response.Status);
        Assert.Equal(status == AlarmStatus.Idle ? null : EndsAt, response.EndsAt);
    }

    [Fact]
    public void Map_SaysHowLongARunningAlarmHasLeft_SoNoClientNeedsToShareTheServersClock()
    {
        var time = new FakeTimeProvider(EndsAt.AddSeconds(-90));
        var mapper = new AlarmResponseMapper(time);

        Assert.Equal(90_000, mapper.Map(new AlarmState { Status = AlarmStatus.Running, EndsAt = EndsAt }).RemainingMilliseconds);

        time.Advance(TimeSpan.FromSeconds(100));
        Assert.Equal(0, mapper.Map(new AlarmState { Status = AlarmStatus.Running, EndsAt = EndsAt }).RemainingMilliseconds);
    }

    [Theory]
    [InlineData(AlarmStatus.Idle)]
    [InlineData(AlarmStatus.Elapsed)]
    public void Map_HasNoTimeLeftToReportOutsideARun(AlarmStatus status)
    {
        var response = new AlarmResponseMapper().Map(new AlarmState { Status = status, EndsAt = status == AlarmStatus.Idle ? null : EndsAt });

        Assert.Null(response.RemainingMilliseconds);
    }

    [Fact]
    public void JoinRoomResponseMapper_CarriesTheAlarmAState()
    {
        var mapper = new JoinRoomResponseMapper(new PersonResponseMapper(), ResponseMappers.VoteStatus(), new RoomConfigurationResponseMapper(), new CardResponseMapper(), new RoundResultResponseMapper(), new AlarmResponseMapper());
        var ann = new Person { Guid = Guid.NewGuid(), Name = "Ann", PersonType = "dev" };

        var response = mapper.Map(RoomResults.Join(ann, alarm: new AlarmState { Status = AlarmStatus.Running, EndsAt = EndsAt }));

        Assert.Equal("running", response.Alarm.Status);
        Assert.Equal(EndsAt, response.Alarm.EndsAt);
    }

    [Fact]
    public void RoomConfigurationUpdatedEventMapper_CarriesTheAlarmAfterTheSave()
    {
        var result = new RoomConfigurationChangeResult
        {
            RoomId = "room",
            Configuration = new RoomConfiguration { CardSet = "linear", CountdownSeconds = 0, ShowFireworks = false, MaxParticipants = 3, AlarmMinutes = 9 },
            Cards = [new CardOption { Value = "1" }],
            UpdatedBy = "Ann",
            VoteStatus = new VoteStatus { Votes = new Dictionary<Guid, string>() },
            Alarm = new AlarmState { Status = AlarmStatus.Elapsed, EndsAt = EndsAt }
        };

        var mapped = new RoomConfigurationUpdatedEventMapper(new RoomConfigurationResponseMapper(), new CardResponseMapper(), ResponseMappers.VoteStatus(), new AlarmResponseMapper()).Map(result);

        Assert.Equal("elapsed", mapped.Alarm.Status);
        Assert.Equal(9, mapped.Configuration.AlarmMinutes);
    }
}
