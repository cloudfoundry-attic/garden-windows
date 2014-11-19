using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using Microsoft.Web.WebSockets;

namespace Containerizer.Facades
{
    public class WebSocketHandlerFacade : IWebSocketHandler
    {
        private readonly WebSocketHandler handler;

        WebSocketHandlerFacade()
        {
            this.handler = new WebSocketHandler();
        }

        public void OnOpen()
        {
            handler.OnOpen();
        }

        public void OnMessage(string message)
        {
            handler.OnMessage(message);
        }

        public void OnMessage(byte[] message)
        {
            handler.OnMessage(message);
        }

        public void OnError()
        {
            handler.OnError();
        }

        public void OnClose()
        {
            handler.OnClose();
        }

        public void Send(string message)
        {
            handler.Send(message);
        }

        public void Send(byte[] message)
        {
            handler.Send(message);
        }

        public void Close()
        {
            handler.Close();
        }

        public Task ProcessWebSocketRequestAsync(AspNetWebSocketContext webSocketContext)
        {
            return handler.ProcessWebSocketRequestAsync(webSocketContext);
        }

        public int MaxIncomingMessageSize
        {
            get { return handler.MaxIncomingMessageSize; }
            set { handler.MaxIncomingMessageSize = value; }
        }

        public AspNetWebSocketContext WebSocketContext
        {
            get { return handler.WebSocketContext; }
            set { handler.WebSocketContext = value; }
        }

        public Exception Error
        {
            get { return handler.Error; }
            set { handler.Error = value; }
        }
    }
}