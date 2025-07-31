using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanningPokerUi.Services
{
    public static class RoomNameGenerator
    {
        public static List<string> OpinionAdjectives { get; set; } = new List<string>();
        public static List<string> SizeAdjectives { get; set; } = new List<string>();
        public static List<string> ShapeAdjectives { get; set; } = new List<string>();
        public static List<string> AgeAdjectives { get; set; } = new List<string>();
        public static List<string> ColourAdjectives { get; set; } = new List<string>();
        public static List<string> OriginAdjectives { get; set; } = new List<string>();
        public static List<string> MaterialAdjectives { get; set; } = new List<string>();
        public static List<string> Nouns { get; set; } = new List<string>();

        public static string Generate(int addNoOfAdjectives = 0)
        {
            // Check if lists are populated, fallback to GUID if empty
            if (Nouns.Count == 0 || OpinionAdjectives.Count == 0 || SizeAdjectives.Count == 0)
            {
                return Guid.NewGuid().ToString();
            }

            var noOfAdjectives = Math.Min(2 + addNoOfAdjectives, 7); // Ensure we don't exceed available lists
            var stringBuilder = new StringBuilder();
            var rand = new Random();

            var set = new HashSet<int>() { 0, 1, 2, 3, 4, 5, 6 };
            var selectedAdjectivesList = new List<int>();
            for (int i = 0; i < noOfAdjectives; i++)
            {
                if (set.Count == 0) break; // Safety check
                var element = set.ElementAt(rand.Next(set.Count));
                selectedAdjectivesList.Add(element);
                set.Remove(element);
            }

            stringBuilder.Append($"{rand.Next(100)}_");

            foreach (var number in selectedAdjectivesList.OrderBy(x => x))
            {
                AppendAnAdjectiveFromListNo(stringBuilder, rand, number);
            }

            AppendRandomElementFromList(stringBuilder, rand, Nouns);

            // Remove trailing underscore
            if (stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] == '_')
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        private static void AppendAnAdjectiveFromListNo(StringBuilder stringBuilder, Random rand, int number)
        {
            switch (number)
            {
                case 0:
                    AppendRandomElementFromList(stringBuilder, rand, OpinionAdjectives);
                    break;
                case 1:
                    AppendRandomElementFromList(stringBuilder, rand, SizeAdjectives);
                    break;
                case 2:
                    AppendRandomElementFromList(stringBuilder, rand, ShapeAdjectives);
                    break;
                case 3:
                    AppendRandomElementFromList(stringBuilder, rand, AgeAdjectives);
                    break;
                case 4:
                    AppendRandomElementFromList(stringBuilder, rand, ColourAdjectives);
                    break;
                case 5:
                    AppendRandomElementFromList(stringBuilder, rand, OriginAdjectives);
                    break;
                case 6:
                    AppendRandomElementFromList(stringBuilder, rand, MaterialAdjectives);
                    break;
            }
        }

        private static void AppendRandomElementFromList(StringBuilder stringBuilder, Random rand, List<string> list)
        {
            if (list.Count == 0)
            {
                stringBuilder.Append("unknown_");
                return;
            }
            stringBuilder.Append($"{list.ElementAt(rand.Next(list.Count))}_");
        }
    }
}
