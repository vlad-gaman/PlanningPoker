using System.ComponentModel.DataAnnotations;

namespace PlanningPokerUi.Models
{
    public class RoomConfiguration
    {
        public string CardSet { get; set; } = "modified-fibonacci";
        
        [Range(1, 30)]
        public int CountdownSeconds { get; set; } = 5;
        
        [Range(500, 5000)]
        public int CountdownInterval { get; set; } = 1000; // milliseconds
        
        [Range(1000, 30000)]
        public int HealthCheckInterval { get; set; } = 5000; // milliseconds
        
        public bool AutoShowVotes { get; set; } = true;
        
        public bool AllowObserverVoting { get; set; } = false;
        
        public bool ShowStatistics { get; set; } = true;
        
        public bool ShowFireworks { get; set; } = true;
        
        // Room management settings
        public string RoomName { get; set; } = "";
        
        public bool IsPublic { get; set; } = true;
        
        public int MaxParticipants { get; set; } = 50;
    }
}