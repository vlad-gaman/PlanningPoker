using System.ComponentModel.DataAnnotations;

namespace PlanningPokerUi.Models
{
    public class RoomConfiguration
    {
        public string CardSet { get; set; } = "modified-fibonacci";
        
        [Range(1, 30)]
        public int CountdownSeconds { get; set; } = 5;
        
        
        public bool ShowFireworks { get; set; } = true;
        
        
        public int MaxParticipants { get; set; } = 50;
    }
}