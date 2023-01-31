using PlanningPokerUi.Models;
using System.Collections.Generic;

namespace PlanningPokerUi.ViewModels
{
    public class RoomViewModel
    {
        public string Guid { get; set; }
        public Person Person { get; set; }
        public List<(string value, string display)> CardSets { get; set; }
            = new List<(string value, string display)> { ("0", null), ("0.5", "\u00BD"), ("1", null), ("2", null), ("3", null), ("5", null), ("8", null), ("13", null), ("20", null), ("40", null), ("100", null), ("?", null), ("coffee", "\u2615") };
    }
}
