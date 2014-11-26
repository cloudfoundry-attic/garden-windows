using System;
using System.Linq;
using Containerizer.Services.Interfaces;
using Microsoft.Web.Administration;

namespace Containerizer.Services.Implementations
{
    public class NetInService : INetInService
    {
        public int AddPort(int port, string id)
        {

            var serverManager = ServerManager.OpenRemote("localhost");
            var existingSite = serverManager.Sites.First(x => x.Name == id);

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
            var bindings = existingSite.Bindings.Where(x => x.EndPoint.Port == 0).ToList();
            foreach (var binding in bindings)
            {
                existingSite.Bindings.Remove(binding);
            }
        }
    }
}