using System.Collections.Generic;
using Containerizer.Services.Implementations;
using Microsoft.Owin.Hosting;
using System;
using System.Threading;

namespace Containerizer
{
    public class Program
    {
        private static readonly ManualResetEvent ExitLatch = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var logger = DependencyResolver.logger;

            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: {0} [ExternalIP] [Port]", "Containerizer.exe");
                return;
            }

            var externalIP = args[0];
            var port = args[1];

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                ExitLatch.Set();
            };

            // Start OWIN host
            DependencyResolver.ExternalIP = new ExternalIP(externalIP);
            int workerThreads, portFinishingThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out portFinishingThreads);
            ThreadPool.SetMinThreads(workerThreads, portFinishingThreads);
            using (WebApp.Start<Startup>("http://*:" + port + "/"))
            {
                logger.Info("containerizer.started", new Dictionary<string, object> {{"port", port }, {"externalIP", externalIP}});
                ExitLatch.WaitOne();
            }
        }
    }
}