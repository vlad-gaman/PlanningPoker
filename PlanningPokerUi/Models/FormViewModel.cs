using System;

namespace PlanningPokerUi.Models
{
    public class FormViewModel
    {
        public string Name { get; set; }
        public string RoomName { get; set; }
        public bool UseFunRoomName { get; set; }
        public string PersonType { get; set; } = "dev";
        public string CardSet { get; set; } = "modified-fibonacci";
        public int CountdownSeconds { get; set; } = 5;
        
        // Advanced room settings
        public bool ShowFireworks { get; set; } = true;
        public int MaxParticipants { get; set; } = 50;
    }
}
