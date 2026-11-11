using System.Threading.Channels;

namespace PlanningPoker.Repository.Services;

public class RoomChangeLog : IRoomChangeLog
{
    private readonly Channel<string> _changes = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<string> Changes => _changes.Reader;

    public void MarkChanged(string roomId)
    {
        _changes.Writer.TryWrite(roomId);
    }
}
