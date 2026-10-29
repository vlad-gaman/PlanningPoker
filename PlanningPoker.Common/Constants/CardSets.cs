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

    public static bool IsValid(string? value)
    {
        return All.Any(option => option.Value == value);
    }
}
