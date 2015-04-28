﻿using Containerizer.Services.Implementations;
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
                logger.Error("Usage: {0} [ExternalIP] [Port]", "Containerizer.exe");
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
            using (WebApp.Start<Startup>("http://*:" + port + "/"))
            {
                logger.Info("SUCCESS: started on {0} with externalIP {1}", port, externalIP);
                logger.Info("Control-C to quit.");
                ExitLatch.WaitOne();
            }
        }
    }
}