using Containerizer.Controllers;
using Containerizer.Models;
using IronFoundry.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IRunService
    {
        IContainer container { get; set; }

        Task OnMessageReceived(IWebSocketEventSender WebSocketSendText, ApiProcessSpec processSpec);
    }
}
