#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Models;
using IronFrame;
using Newtonsoft.Json;
using Owin.WebSocket;
using Containerizer.Services.Interfaces;
using Logger;

#endregion

namespace Containerizer.Controllers
{
    public interface IWebSocketEventSender
    {
        void SendEvent(string messageType, string message);

        void Flush();

        Task Close(WebSocketCloseStatus status, string reason);
    }

    public class ContainerProcessHandler : WebSocketConnection, IWebSocketEventSender
    {
        private readonly IContainerService containerService;
        private readonly IRunService runService;
        private readonly ILogger logger;

        public ContainerProcessHandler(IContainerService containerService, IRunService runService, ILogger logger)
        {
            this.containerService = containerService;
            this.runService = runService;
            this.logger = logger;
        }

        public void SendEvent(string messageType, string message)
        {
            // logger.Info("SendEvent: {0} :: {1}", messageType, message);
            var jsonString = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = messageType,
                Data = message
            }, Formatting.None);
            var data = new UTF8Encoding(true).GetBytes(jsonString);
            SendText(data, true);
        }

        public void Flush()
        {
            QueueSend.Drain();
        }

        public override void OnOpen()
        {
            var handle = Arguments["handle"];
            logger.Info("ContainerProcessHandler.OnOpen", new Dictionary<string, object> {{"handle", handle}});

            runService.container = containerService.GetContainerByHandle(handle);
        }


        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            logger.Info("ContainerProcessHandler.OnClose", new Dictionary<string, object> {{"closeStatus", closeStatus.ToString()}, {"closeStatusDescription", closeStatusDescription}});
        }

        public override void OnReceiveError(Exception error)
        {
            logger.Error("ContainerProcessHandler.OnReceiveError", new Dictionary<string, object> {{"message", error.Message}, {"stackTrace", error.StackTrace}});
        }

        public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            if (message.Count > 0)
            {
                var bytes = new UTF8Encoding(true).GetString(message.Array, 0, message.Count);
                var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(bytes);

                if (streamEvent.MessageType == "run" && streamEvent.ApiProcessSpec != null)
                {
                    runService.Run(this, streamEvent.ApiProcessSpec);
                }
            }

            return Task.Delay(0);
        }
    }
}