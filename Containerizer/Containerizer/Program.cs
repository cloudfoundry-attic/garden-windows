using System.Collections.Generic;
using Containerizer.Services.Implementations;
using Microsoft.Owin.Hosting;
using System;
using System.Threading;
using CommandLine;
using CommandLine.Text;

namespace Containerizer
{
    public class Program
    {
        private static readonly ManualResetEvent ExitLatch = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var logger = DependencyResolver.logger;

            var startOptions = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, startOptions))
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    ExitLatch.Set();
                };

                // Start OWIN host
                DependencyResolver.StartOptions = startOptions;

                int workerThreads, portFinishingThreads;
                ThreadPool.GetMaxThreads(out workerThreads, out portFinishingThreads);
                ThreadPool.SetMinThreads(workerThreads, portFinishingThreads);
                using (WebApp.Start<Startup>("http://*:" + startOptions.Port + "/"))
                {
                    logger.Info("containerizer.started", new Dictionary<string, object> { { "port", startOptions.Port }, { "externalIP", startOptions.ExternalIp } });
                    ExitLatch.WaitOne();
                }
            }
        }
    }
}