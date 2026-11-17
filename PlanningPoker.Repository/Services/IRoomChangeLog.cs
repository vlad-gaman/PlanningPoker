using System.Threading.Channels;

namespace PlanningPoker.Repository.Services;

/// <summary>The ids of the rooms that changed (or were removed) and still have to be written to the store.</summary>
public interface IRoomChangeLog
{
    void MarkChanged(string roomId);

    ChannelReader<string> Changes { get; }
}
