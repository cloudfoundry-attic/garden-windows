using Containerizer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Containerizer.Services.Implementations
{
    public class ExternalIP : IExternalIP
    {
        private string externalIP;

        public ExternalIP(string externalIP)
        {
            this.externalIP = externalIP;
        }

        string IExternalIP.ExternalIP()
        {
            return externalIP;
        }
    }
}