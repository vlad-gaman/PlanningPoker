using System.Collections.Generic;

namespace PlanningPokerUi.Models
{
    public class VoteResultInfo
    {
        public List<Vote> Votes { get; set; }
        public Statistics Statistics { get; set; }
        public int Countdown { get; set; } = -1;
        public bool VotingFinished { get; set; }
        public bool HasEveryoneVoted { get; set; }
    }
}
