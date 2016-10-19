using System;
using System.Collections.Generic;
using Logger;
using NLog;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace Containerizer
{
    public sealed class LoggerTarget : TargetWithLayout
    {
        private ILogger Logger;
        public LoggerTarget(ILogger Logger)
        {
            this.Logger = Logger;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            string logMessage = Layout.Render(logEvent);
            if (logEvent.Level == LogLevel.Error || logEvent.Level == LogLevel.Fatal)
                Logger.Error(logMessage);
            else
                Logger.Info(logMessage);
        }
    }
}
