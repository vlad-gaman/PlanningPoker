namespace PlanningPoker.Service.Services;

/// <summary>Makes text safe for the issues log: one line, no query strings, no vote values, capped in length.</summary>
public interface IIssueRedactor
{
    string Redact(string text);

    /// <summary>The exception type and the first line of its message; never the stack trace.</summary>
    string Describe(Exception exception);
}
