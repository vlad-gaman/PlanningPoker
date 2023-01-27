using Newtonsoft.Json;
using System;
using System.Net.WebSockets;

namespace PlanningPokerUi.Models
{
    public class Person
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public bool IsObserver { get; set; }
        public string PersonType { get; set; }
        [JsonIgnore]
        public WebSocket WebSocket { get; set; }
        [JsonIgnore]
        public bool IsConnected { get; set; } = true;

        public void CopyFrom(FormViewModel formViewModel)
        {
            Name = formViewModel.Name;
            IsObserver = formViewModel.IsObserver;
            PersonType = formViewModel.PersonType;
        }
    }
}
