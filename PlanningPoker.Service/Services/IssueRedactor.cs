using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public partial class IssueRedactor(IOptions<IssueLogOptions> options) : IIssueRedactor
{
    private const string Redacted = "[redacted]";
    private const string Ellipsis = "...";

    // "?name=value&other=1" up to the next space.
    [GeneratedRegex(@"\?[\w%.\-]+=\S*")]
    private static partial Regex QueryString();

    // "vote: 8", "votes=XL", "card = 13": a short value after a vote or card label. Longer words ("Vote: person not found") are left alone.
    [GeneratedRegex(@"\b(votes?|card)(\s*[:=]\s*)\S{1,4}(?=[\s,;.)]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex VoteValue();

    public string Redact(string text)
    {
        var line = text.Split('\n', 2)[0].TrimEnd('\r');
        line = QueryString().Replace(line, string.Empty);
        line = VoteValue().Replace(line, $"$1$2{Redacted}");
        var max = options.Value.MaxMessageLength;
        return line.Length <= max ? line : line[..(max - Ellipsis.Length)] + Ellipsis;
    }

    public string Describe(Exception exception)
    {
        return Redact($"{exception.GetType().Name}: {exception.Message}");
    }
}
