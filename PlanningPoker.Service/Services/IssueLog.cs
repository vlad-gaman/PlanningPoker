using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanningPoker.Common.Constants;
using PlanningPoker.Common.Models;
using PlanningPoker.Repository.Models;
using PlanningPoker.Repository.Repositories;
using PlanningPoker.Service.Mappers;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Service.Services;

public class IssueLog(
    IIssueStore store,
    IIssueRedactor redactor,
    IIssueEntryMapper mapper,
    IOptions<IssueLogOptions> options,
    TimeProvider time,
    ILogger<IssueLog> logger) : IIssueLog
{
    public void Record(IssueLevel level, string source, string message, Exception? exception = null)
    {
        var text = exception is null ? message : $"{message} ({redactor.Describe(exception)})";
        Write(level, source, text, null);
    }

    public void RecordFailedLogin(string? ipAddress, bool locked)
    {
        var message = locked ? "Operator sign-in refused: locked out" : "Operator sign-in failed";
        Write(IssueLevel.Warning, IssueLogConstants.AdminSource, message, string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress);
    }

    public IReadOnlyList<IssueEntry> Recent(int limit)
    {
        try
        {
            return store.Recent(limit);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The issues log cannot be read");
            return [];
        }
    }

    public int CountFailedLogins(DateTimeOffset since)
    {
        try
        {
            return store.CountFailedLogins(since);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The issues log cannot be counted");
            return 0;
        }
    }

    public void Prune()
    {
        try
        {
            store.Prune(time.GetUtcNow() - options.Value.MaxAge, options.Value.MaxEntries);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The issues log cannot be pruned");
        }
    }

    private void Write(IssueLevel level, string source, string message, string? ipAddress)
    {
        try
        {
            store.Add(mapper.Map(time.GetUtcNow(), level, redactor.Redact(source), redactor.Redact(message), ipAddress));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "An issues log entry could not be saved");
        }
    }
}
