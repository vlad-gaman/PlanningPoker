using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace PlanningPokerUi.Services
{
    public class WebSocketManagerService
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _websockets;
        public WebSocketManagerService()
        {
            _websockets = new ConcurrentDictionary<Guid, WebSocket>();
        }

        public WebSocket GetSocketByGuid(Guid guid)
        {
            _websockets.TryGetValue(guid, out var webSocket);
            return webSocket;
        }

        public bool AddWebSocket(WebSocket webSocket, Guid guid)
        {
            return _websockets.TryAdd(guid, webSocket);
        }

        public async Task RemoveWebSocket(Guid guid)
        {
            if (_websockets.TryRemove(guid, out var webSocket))
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by manager", CancellationToken.None);
            }
        }
    }
}
