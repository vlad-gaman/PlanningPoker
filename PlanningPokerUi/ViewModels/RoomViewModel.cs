using PlanningPokerUi.Models;
using System.Collections.Generic;

namespace PlanningPokerUi.ViewModels
{
    public class RoomViewModel
    {
        public string Guid { get; set; }
        public Person Person { get; set; }
        public List<(string value, string display)> CardSets { get; set; }
        public RoomConfiguration Configuration { get; set; }
        public Dictionary<string, string> AvailableCardSets { get; set; }
        public Person Owner { get; set; }
        public bool IsCurrentUserOwner => Person?.Guid == Owner?.Guid;
        
        public RoomViewModel()
        {
            CardSets = Models.CardSets.GetCardSet("modified-fibonacci");
            Configuration = new RoomConfiguration();
            AvailableCardSets = Models.CardSets.CardSetNames;
        }
        
        public RoomViewModel(Room room, Person person)
        {
            Guid = room.Guid;
            Person = person;
            Owner = room.Owner;
            CardSets = Models.CardSets.GetCardSet(room.CardSet);
            Configuration = room.Configuration;
            AvailableCardSets = Models.CardSets.CardSetNames;
        }
    }
}
