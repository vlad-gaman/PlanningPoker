namespace PlanningPoker.Common.Constants;

/// <summary>The designs a person can choose for their face-down card. The client draws each one by this id.</summary>
public static class CardBacks
{
    public const string Classic = "classic";
    public const string Stripes = "stripes";
    public const string Dots = "dots";
    public const string Geometric = "geometric";
    public const string Waves = "waves";
    public const string Checker = "checker";
    public const string Diamonds = "diamonds";
    public const string Zigzag = "zigzag";
    public const string Grid = "grid";
    public const string Rings = "rings";
    public const string Sunburst = "sunburst";
    public const string Plaid = "plaid";

    /// <summary>What everyone starts with, and what a person stored before card backs existed has.</summary>
    public const string Default = Classic;

    public static bool IsValid(string? value)
    {
        return value is Classic or Stripes or Dots or Geometric or Waves or Checker or Diamonds or Zigzag or Grid or Rings or Sunburst or Plaid;
    }
}
