using PlanningPoker.Common.Models;

namespace PlanningPoker.Common.Constants;

public static class CardSets
{
    public const string Fibonacci = "fibonacci";
    public const string ModifiedFibonacci = "modified-fibonacci";
    public const string Linear = "linear";
    public const string PowersOfTwo = "powers-of-2";
    public const string TShirtSizes = "t-shirt-sizes";
    public const string TrafficLights = "traffic-lights";
    public const string FistOfFive = "fist-of-five";

    public static readonly IReadOnlyList<CardSetOption> All =
    [
        new() { Value = Fibonacci, Display = "Fibonacci" },
        new() { Value = ModifiedFibonacci, Display = "Modified Fibonacci" },
        new() { Value = Linear, Display = "Linear (1-10)" },
        new() { Value = PowersOfTwo, Display = "Powers of 2" },
        new() { Value = TShirtSizes, Display = "T-Shirt Sizes" },
        new() { Value = TrafficLights, Display = "Traffic Lights" },
        new() { Value = FistOfFive, Display = "Fist of Five" }
    ];

    /// <summary>The cards of each set, in display order.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<CardOption>> Cards = new Dictionary<string, IReadOnlyList<CardOption>>
    {
        [Fibonacci] = [Card("0"), Card("0.5", "½"), Card("1"), Card("2"), Card("3"), Card("5"), Card("8"), Card("13"), Card("21"), Card("34"), Card("55"), Card("89"), Card("?"), Card("coffee", "☕")],
        [ModifiedFibonacci] = [Card("0"), Card("0.5", "½"), Card("1"), Card("2"), Card("3"), Card("5"), Card("8"), Card("13"), Card("20"), Card("40"), Card("100"), Card("?"), Card("coffee", "☕")],
        [Linear] = [Card("1"), Card("2"), Card("3"), Card("4"), Card("5"), Card("6"), Card("7"), Card("8"), Card("9"), Card("10"), Card("?"), Card("coffee", "☕")],
        [PowersOfTwo] = [Card("0"), Card("1"), Card("2"), Card("4"), Card("8"), Card("16"), Card("32"), Card("64"), Card("128"), Card("?"), Card("coffee", "☕")],
        [TShirtSizes] = [Card("XS"), Card("S"), Card("M"), Card("L"), Card("XL"), Card("XXL"), Card("?"), Card("coffee", "☕")],
        [TrafficLights] = [Card("green", "🟢"), Card("yellow", "🟡"), Card("red", "🔴")],
        [FistOfFive] =
        [
            Card("0", "zero-finger-512x512.png"),
            Card("1", "one-finger-512x512.png"),
            Card("2", "two-finger-512x512.png"),
            Card("3", "three-finger-512x512.png"),
            Card("4", "four-finger-512x512.png"),
            Card("5", "five-finger-512x512.png")
        ]
    };

    private static CardOption Card(string value, string? display = null)
    {
        return new CardOption { Value = value, Display = display };
    }

    public static bool IsValid(string? value)

    {
        return All.Any(option => option.Value == value);
    }
}
