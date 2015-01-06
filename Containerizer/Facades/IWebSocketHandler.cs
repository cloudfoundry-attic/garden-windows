#region

using System;
using System.Threading.Tasks;
using System.Web.WebSockets;

#endregion

namespace Containerizer.Facades
{
    public interface IWebSocketHandler
    {
        int MaxIncomingMessageSize { get; set; }
        AspNetWebSocketContext WebSocketContext { get; set; }
        Exception Error { get; set; }
        void OnOpen();
        void OnMessage(string message);
        void OnMessage(byte[] message);
        void OnError();
        void OnClose();
        void Send(string message);
        void Send(byte[] message);
        void Close();
        Task ProcessWebSocketRequestAsync(AspNetWebSocketContext webSocketContext);
    }
}