using System.Collections.Generic;

namespace PlanningPokerUi.Models
{
    public static class CardSets
    {
        public static readonly Dictionary<string, List<(string value, string display)>> AvailableCardSets = new()
        {
            ["fibonacci"] = new List<(string value, string display)>
            {
                ("0", null), ("0.5", "½"), ("1", null), ("2", null), ("3", null), 
                ("5", null), ("8", null), ("13", null), ("21", null), ("34", null), 
                ("55", null), ("89", null), ("?", null), ("coffee", "☕")
            },
            
            ["modified-fibonacci"] = new List<(string value, string display)>
            {
                ("0", null), ("0.5", "½"), ("1", null), ("2", null), ("3", null), 
                ("5", null), ("8", null), ("13", null), ("20", null), ("40", null), 
                ("100", null), ("?", null), ("coffee", "☕")
            },
            
            ["linear"] = new List<(string value, string display)>
            {
                ("1", null), ("2", null), ("3", null), ("4", null), ("5", null), 
                ("6", null), ("7", null), ("8", null), ("9", null), ("10", null), 
                ("?", null), ("coffee", "☕")
            },
            
            ["powers-of-2"] = new List<(string value, string display)>
            {
                ("0", null), ("1", null), ("2", null), ("4", null), ("8", null), 
                ("16", null), ("32", null), ("64", null), ("128", null), 
                ("?", null), ("coffee", "☕")
            },
            
            ["t-shirt-sizes"] = new List<(string value, string display)>
            {
                ("XS", null), ("S", null), ("M", null), ("L", null), ("XL", null), 
                ("XXL", null), ("?", null), ("coffee", "☕")
            },
            
            ["traffic-lights"] = new List<(string value, string display)>
            {
                ("green", "🟢"), ("yellow", "🟡"), ("red", "🔴")
            },
            
            ["fist-of-five"] = new List<(string value, string display)>
            {
                ("0", "0"), ("1", "1"), ("2", "2"), ("3", "3"), ("4", "4"), ("5", "5")
            }
        };

        public static readonly Dictionary<string, string> CardSetNames = new()
        {
            ["fibonacci"] = "Fibonacci",
            ["modified-fibonacci"] = "Modified Fibonacci",
            ["linear"] = "Linear (1-10)",
            ["powers-of-2"] = "Powers of 2",
            ["t-shirt-sizes"] = "T-Shirt Sizes",
            ["traffic-lights"] = "Traffic Lights",
            ["fist-of-five"] = "Fist of Five"
        };

        public static List<(string value, string display)> GetCardSet(string cardSetKey)
        {
            return AvailableCardSets.TryGetValue(cardSetKey, out var cardSet) 
                ? cardSet 
                : AvailableCardSets["modified-fibonacci"]; // Default fallback
        }
    }
}