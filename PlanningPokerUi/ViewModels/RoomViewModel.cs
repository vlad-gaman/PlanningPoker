using PlanningPokerUi.Models;
using System.Collections.Generic;

namespace PlanningPokerUi.ViewModels
{
    public class RoomViewModel
    {
        public string Guid { get; set; }
        public Person Person { get; set; }
        public List<(string value, string display)> CardSets { get; set; }
        
        public RoomViewModel()
        {
            CardSets = Models.CardSets.GetCardSet("modified-fibonacci");
        }
        
        public RoomViewModel(Room room, Person person)
        {
            Guid = room.Guid;
            Person = person;
            CardSets = Models.CardSets.GetCardSet(room.CardSet);
        }
    }
}
