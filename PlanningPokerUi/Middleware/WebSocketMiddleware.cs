using Microsoft.AspNetCore.Http;
using PlanningPokerUi.Services;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace PlanningPokerUi.Middleware
{
    public class WebSocketMiddleware : IMiddleware
    {
        private readonly WebSocketHandlerService _webSocketHandlerService;
        private readonly RoomsManagerService _roomsManagerService;

        public WebSocketMiddleware(WebSocketHandlerService webSocketHandlerService, RoomsManagerService roomsManagerService)
        {
            _webSocketHandlerService = webSocketHandlerService;
            _roomsManagerService = roomsManagerService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var guid = context.Request.Path.Value.Split("/")[1];
                var room = _roomsManagerService.GetRoom(guid);
                if (room != null)
                {
                    using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                    {
                        await _webSocketHandlerService.OnConnectedAsync(webSocket, context);
                        await Receive(webSocket, async (result, buffer) =>
                        {
                            switch (result.MessageType)
                            {
                                default:
                                    await _webSocketHandlerService.ReceiveAsync(context, webSocket, result, buffer);
                                    break;
                                case WebSocketMessageType.Close:
                                    await _webSocketHandlerService.OnDisconnectedAsync(webSocket, context);
                                    break;
                            }
                        });
                    }
                }
            }
            else
            {
                await next(context);
            }
        }

        private async Task Receive(WebSocket webSocket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 20];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                handleMessage(result, buffer);
                buffer = new byte[1024 * 20];
            }
        }

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }


            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
