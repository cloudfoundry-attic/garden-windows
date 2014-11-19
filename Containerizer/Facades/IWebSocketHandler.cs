using System;
using System.Threading.Tasks;
using System.Web.WebSockets;

namespace Containerizer.Facades
{
    public interface IWebSocketHandler
    {
        void OnOpen();
        void OnMessage(string message);
        void OnMessage(byte[] message);
        void OnError();
        void OnClose();
        void Send(string message);
        void Send(byte[] message);
        void Close();
        Task ProcessWebSocketRequestAsync(AspNetWebSocketContext webSocketContext);
        int MaxIncomingMessageSize { get; set; }
        AspNetWebSocketContext WebSocketContext { get; set; }
        Exception Error { get; set; }
    }
}