using System;
using System.Collections.Generic;
using System.Linq;
using Containerizer.Services.Interfaces;
using Microsoft.Web.Administration;

namespace Containerizer.Services.Implementations
{
    public class NetInService : INetInService
    {
        public int AddPort(int port, string id)
        {
            ServerManager serverManager = ServerManager.OpenRemote("localhost");
            Site existingSite = serverManager.Sites.First(x => x.Name == id);

            removeInvalidBindings(existingSite);

            if (port == 0)
            {
                var rand = new Random();
                port = rand.Next((40000 - 30000) + 1) + 30000;
            }
            existingSite.Bindings.Add("*:" + port + ":", "http");
            serverManager.CommitChanges();

            return port;
        }

        private static void removeInvalidBindings(Site existingSite)
        {
            List<Binding> bindings = existingSite.Bindings.Where(x => x.EndPoint.Port == 0).ToList();
            foreach (Binding binding in bindings)
            {
                existingSite.Bindings.Remove(binding);
            }
        }
    }
}