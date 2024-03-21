using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;

namespace PlanningPokerUi.Models
{
    public class Person
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string PersonType { get; set; }
        [JsonIgnore]
        public WebSocket WebSocket { get; set; }
        [JsonIgnore]
        public bool IsConnected { get; set; } = true;

        public void CopyFrom(FormViewModel formViewModel)
        {
            var tempName = formViewModel.Name.Substring(0, formViewModel.Name.Length > 20 ? 20 : formViewModel.Name.Length);

            Name = EncodeNonAsciiCharacters(tempName);
            PersonType = formViewModel.PersonType;
        }

        static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "%u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
