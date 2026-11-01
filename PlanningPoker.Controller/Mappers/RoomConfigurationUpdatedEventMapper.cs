using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomConfigurationUpdatedEventMapper(
    IRoomConfigurationResponseMapper configurationMapper,
    ICardResponseMapper cardMapper,
    IVoteStatusResponseMapper voteStatusMapper) : IRoomConfigurationUpdatedEventMapper
{
    public RoomConfigurationUpdatedEvent Map(RoomConfigurationChangeResult result)
    {
        return new RoomConfigurationUpdatedEvent
        {
            Configuration = configurationMapper.Map(result.Configuration),
            Cards = result.Cards.Select(cardMapper.Map).ToList(),
            UpdatedBy = result.UpdatedBy,
            VoteStatus = voteStatusMapper.Map(result.VoteStatus)
        };
    }
}
