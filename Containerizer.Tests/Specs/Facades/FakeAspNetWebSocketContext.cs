using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.WebSockets;

namespace Containerizer.Tests.Specs.Facades
{
    internal class FakeWebSocket : WebSocket
    {
        public ArraySegment<byte> LastSentBuffer;

        public override WebSocketCloseStatus? CloseStatus
        {
            get { throw new NotImplementedException(); }
        }

        public override string CloseStatusDescription
        {
            get { throw new NotImplementedException(); }
        }

        public override string SubProtocol
        {
            get { throw new NotImplementedException(); }
        }

        public override WebSocketState State
        {
            get { throw new NotImplementedException(); }
        }

        public override void Abort()
        {
            throw new NotImplementedException();
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            LastSentBuffer = buffer;
            return null;
        }
    }

    internal class FakeAspNetWebSocketContext : AspNetWebSocketContext
    {
        private readonly FakeWebSocket fakeWebSocket;

        public FakeAspNetWebSocketContext()
        {
            fakeWebSocket = new FakeWebSocket();
        }

        public override WebSocket WebSocket
        {
            get { return fakeWebSocket; }
        }
    }
}