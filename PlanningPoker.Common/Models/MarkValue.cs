using System.Globalization;

namespace PlanningPoker.Common.Models;

/// <summary>The number behind a card mark, when it has one: "5" and "0.5" do, "?", coffee, "XL" and traffic lights do not.</summary>
public static class MarkValue
{
    private const NumberStyles NumericMark = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static decimal? TryParse(string mark)
    {
        return decimal.TryParse(mark, NumericMark, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
