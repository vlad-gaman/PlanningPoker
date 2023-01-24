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
        [JsonIgnore]
        public WebSocket WebSocket { get; set; }

        public void CopyFrom(FormViewModel formViewModel)
        {
            Name = formViewModel.Name;
            IsObserver = formViewModel.IsObserver;
        }
    }
}
