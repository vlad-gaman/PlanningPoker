using System.Collections.Generic;

namespace PlanningPokerUi.Models
{
    public class Statistics
    {
        public List<MarkPercentage> Marks { get; set; }
        public decimal? AverageMark { get; set; }
        public string HighestMark { get; set; }
        public List<MarkPercentage> MarksDev { get; set; }
        public string HighestMarkDev { get; set; }
        public decimal? AverageMarkDev { get; set; }
        public List<MarkPercentage> MarksTest { get; set; }
        public string HighestMarkTest { get; set; }
        public decimal? AverageMarkTest { get; set; }
    }
}
