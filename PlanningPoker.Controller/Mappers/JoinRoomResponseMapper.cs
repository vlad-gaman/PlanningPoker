using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class JoinRoomResponseMapper(
    IPersonResponseMapper personMapper,
    IVoteStatusResponseMapper voteStatusMapper,
    IRoomConfigurationResponseMapper configurationMapper,
    ICardResponseMapper cardMapper,
    IRoundResultResponseMapper roundResultMapper) : IJoinRoomResponseMapper
{
    public JoinRoomResponse Map(JoinRoomResult result)
    {
        return new JoinRoomResponse
        {
            People = result.People.Select(personMapper.Map).ToList(),
            VoteStatus = voteStatusMapper.Map(result.VoteStatus),
            Cards = result.Cards.Select(cardMapper.Map).ToList(),
            MyMark = result.MyMark,
            OwnerGuid = result.Room.OwnerGuid,
            Configuration = configurationMapper.Map(result.Room.Configuration),
            RoundHistory = result.Room.RoundHistory.Select(roundResultMapper.Map).ToList()
        };
    }
}
