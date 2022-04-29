using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlanningPokerUi.Services
{
    public abstract class WebSocketHandlerService
    {
        protected readonly WebSocketManagerService _webSocketManagerService;

        protected WebSocketHandlerService(WebSocketManagerService webSocketManagerService)
        {
            _webSocketManagerService = webSocketManagerService;
        }

        protected Guid GetGuid(HttpContext httpContext)
        {
            return Guid.Parse(httpContext.Session.GetString("Guid"));
        }

        public virtual async Task OnConnectedAsync(WebSocket webSocket, HttpContext httpContext)
        {
            var guid = GetGuid(httpContext);
            if (!_webSocketManagerService.AddWebSocket(webSocket, guid))
            {
                await _webSocketManagerService.RemoveWebSocket(guid);
                _webSocketManagerService.AddWebSocket(webSocket, guid);
            }
        }

        public virtual async Task OnDisconnectedAsync(WebSocket webSocket, HttpContext httpContext)
        {
            var guid = GetGuid(httpContext);
            await _webSocketManagerService.RemoveWebSocket(guid);
        }

        public async Task SendMessageAsync(WebSocket webSocket, string message)
        {
            if (webSocket?.State != WebSocketState.Open)
            {
                return;
            }

            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message), 0, message.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public Task SendMessageAsync(Guid guid, string message)
        {
            return SendMessageAsync(_webSocketManagerService.GetSocketByGuid(guid), message);
        }

        public abstract Task ReceiveAsync(HttpContext httpContext, WebSocket webSocket, WebSocketReceiveResult result, byte[] buffer);
    }
}
