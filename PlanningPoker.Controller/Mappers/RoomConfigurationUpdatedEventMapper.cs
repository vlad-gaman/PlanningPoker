using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomConfigurationUpdatedEventMapper(
    IRoomConfigurationResponseMapper configurationMapper,
    ICardResponseMapper cardMapper,
    IVoteStatusResponseMapper voteStatusMapper,
    IAlarmResponseMapper alarmMapper) : IRoomConfigurationUpdatedEventMapper
{
    public RoomConfigurationUpdatedEvent Map(RoomConfigurationChangeResult result)
    {
        return new RoomConfigurationUpdatedEvent
        {
            Configuration = configurationMapper.Map(result.Configuration),
            Cards = result.Cards.Select(cardMapper.Map).ToList(),
            UpdatedBy = result.UpdatedBy,
            Alarm = alarmMapper.Map(result.Alarm),
            VoteStatus = voteStatusMapper.Map(result.VoteStatus)
        };
    }
}
