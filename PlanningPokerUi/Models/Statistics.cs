using System.Collections.Generic;

namespace PlanningPokerUi.Models
{
    public class Statistics
    {
        public List<MarkPercentage> Marks { get; set; }
        public decimal? AverageMark { get; set; }
        public string HighestMark { get; set; }
    }
}
