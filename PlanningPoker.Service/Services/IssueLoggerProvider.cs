using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanningPoker.Common.Models;

namespace PlanningPoker.Service.Services;

/// <summary>
/// Sends every warning and error the app logs (unhandled exceptions, failed persistence writes, sweep failures and so on) to the issues log.
/// The log itself and the Operator sign-in service are skipped: the first would loop, the second records its own entries.
/// </summary>
public sealed class IssueLoggerProvider(IServiceProvider services) : ILoggerProvider
{
    private static readonly string[] SkippedCategories =
    [
        typeof(IssueLog).FullName!,
        "PlanningPoker.Repository.Repositories.SqliteIssueStore",
        typeof(OperatorAuthService).FullName!
    ];

    [ThreadStatic]
    private static bool _writing;

    public ILogger CreateLogger(string categoryName)
    {
        return SkippedCategories.Contains(categoryName) ? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance : new IssueLogger(categoryName, this);
    }

    public void Dispose()
    {
    }

    private void Write(string category, LogLevel level, string message, Exception? exception)
    {
        if (_writing)
        {
            return;
        }

        _writing = true;
        try
        {
            // Resolved on first use: the log needs a logger, and the loggers are still being created when this provider is.
            services.GetService<IIssueLog>()?.Record(level >= LogLevel.Error ? IssueLevel.Error : IssueLevel.Warning, SourceOf(category), message, exception);
        }
        catch (Exception)
        {
            // Logging must never break the code that logs.
        }
        finally
        {
            _writing = false;
        }
    }

    private static string SourceOf(string category)
    {
        var dot = category.LastIndexOf('.');
        return dot < 0 ? category : category[(dot + 1)..];
    }

    private sealed class IssueLogger(string category, IssueLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Warning && logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(category, logLevel, formatter(state, exception), exception);
            }
        }
    }
}
