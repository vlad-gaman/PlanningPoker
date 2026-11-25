namespace PlanningPoker.Common.Constants;

/// <summary>The designs a person can choose for their face-down card. The client draws each one by this id.</summary>
public static class CardBacks
{
    public const string Classic = "classic";
    public const string Stripes = "stripes";
    public const string Dots = "dots";
    public const string Geometric = "geometric";
    public const string Waves = "waves";
    public const string Diamonds = "diamonds";
    public const string Rings = "rings";
    public const string Sunburst = "sunburst";
    public const string Scrollwork = "scrollwork";
    public const string Flourish = "flourish";
    public const string Nouveau = "nouveau";
    public const string Victorian = "victorian";

    /// <summary>What everyone starts with, and what a person stored before card backs existed has.</summary>
    public const string Default = Classic;

    public static bool IsValid(string? value)
    {
        return value is Classic or Stripes or Dots or Geometric or Waves or Diamonds or Rings or Sunburst or Scrollwork or Flourish or Nouveau or Victorian;
    }
}
