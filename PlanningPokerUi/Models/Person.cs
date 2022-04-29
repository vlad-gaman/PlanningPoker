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
        public int Index { get; set; }
        [JsonIgnore]
        public WebSocket WebSocket { get; set; }

        public void CopyFrom(Person other)
        {
            Name = other.Name;
            IsObserver = other.IsObserver;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Person))
            {
                return false;
            }
            var other = obj as Person;

            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
