#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Models;
using IronFoundry.Container;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;
using Owin.WebSocket;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Controllers
{
    public interface IWebSocketEventSender
    {
        void SendEvent(string messageType, string message);
    }

    public class ContainerProcessHandler : WebSocketConnection, IWebSocketEventSender
    {
        private readonly IContainerService containerService;
        private readonly IRunService runService;

        public ContainerProcessHandler(IContainerService containerService, IRunService runService)
        {
            Console.WriteLine("Container Process Handler Constructor");
            this.containerService = containerService;
            this.runService = runService;
        }

        public void SendEvent(string messageType, string message)
        {
            Console.WriteLine("SendEvent: {0} :: {1}", messageType, message);
            var jsonString = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = messageType,
                Data = message
            }, Formatting.None);
            var data = new UTF8Encoding(true).GetBytes(jsonString);
            SendText(data, true);
        }

        public override void OnOpen()
        {
            var handle = Arguments["handle"];
            Console.WriteLine("onOpen: {0}", handle);

            runService.container = containerService.GetContainerByHandle(handle);
        }

        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            Console.WriteLine("OnClose: {0} :: {1}", closeStatus.ToString(), closeStatusDescription);
        }

        public override void OnReceiveError(Exception error)
        {
            Console.WriteLine("OnReceiveError: {0}", error.Message);
        }

        public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            var bytes = new UTF8Encoding(true).GetString(message.Array, 0, message.Count);
            var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(bytes);

            if (streamEvent.MessageType == "run" && streamEvent.ApiProcessSpec != null)
            {
                return runService.OnMessageReceived(this, streamEvent.ApiProcessSpec);
            }
            return null;
        }
    }
}