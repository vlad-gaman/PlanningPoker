using PlanningPoker.Common.Constants;
using PlanningPoker.Repository.Models;
using PlanningPoker.Service.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Service.Mappers;

public class VoteStatusMapper(IVoteStatisticsMapper statisticsMapper, ICardSetService cardSets) : IVoteStatusMapper
{
    public VoteStatus Map(RoomEntity room, IReadOnlyCollection<Person> members)
    {
        var voters = members.Where(member => member.PersonType != PersonTypes.Observer).ToList();
        return new VoteStatus
        {
            Votes = room.VotingEnabled
                ? room.Votes.ToDictionary(vote => vote.Key, _ => RoomConstraints.HiddenVote)
                : voters.Where(voter => room.Votes.ContainsKey(voter.Guid)).ToDictionary(voter => voter.Guid, voter => room.Votes[voter.Guid]),
            HasEveryoneVoted = voters.Count > 0 && voters.All(voter => room.Votes.ContainsKey(voter.Guid)),
            VotingFinished = !room.VotingEnabled,
            Countdown = room.VotingEnabled && room.CountdownRunning ? room.CountdownRemaining : null,
            Statistics = room.VotingEnabled || room.Revealed is null ? null : statisticsMapper.Map(room.Revealed, CardOrder(room))
        };
    }

    private IReadOnlyList<string> CardOrder(RoomEntity room)
    {
        var cards = cardSets.GetCards(room.Configuration.CardSet);
        return cards.IsSuccess ? cards.Data!.Select(card => card.Value).ToList() : [];
    }
}
