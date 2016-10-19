using System.Collections.Generic;
using Containerizer.Services.Implementations;
using Logger;
using Microsoft.Owin.Hosting;
using System;
using System.Threading;
using NLog;
using NLog.Config;
using LogLevel = NLog.LogLevel;

namespace Containerizer
{
    public class Program
    {
        private static readonly ManualResetEvent ExitLatch = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var logger = DependencyResolver.logger;
            ConfigNLog(logger);

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
                using (WebApp.Start<Startup>("http://127.0.0.1:" + startOptions.Port + "/"))
                {
                    logger.Info("containerizer.started", new Dictionary<string, object> { { "port", startOptions.Port }, { "machineIp", startOptions.MachineIp } });
                    ExitLatch.WaitOne();
                }
            }
        }

        static void ConfigNLog(ILogger logger)
        {
            var config = new LoggingConfiguration();

            var loggerTarget = new LoggerTarget(logger);
            config.AddTarget("logger", loggerTarget);
            loggerTarget.Layout = "${message}";
            var rule = new LoggingRule("*", LogLevel.Trace, loggerTarget);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
        }
    }
}