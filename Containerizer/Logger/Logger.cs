using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Logger
{
    public enum LogLevel
    {
        DEBUG = 0,
        INFO = 1,
        ERROR = 2,
        FATAL = 3
    }

    public class Logger : ILogger
    {
        private string component;

        public Logger(string component)
        {
            this.component = component;
        }

        public void Info(string msg, Dictionary<string, object> args = null)
        {
            WriteLine(new LogMessage()
            {
                Message = msg,
                LogLevel = LogLevel.INFO,
                Data = args,
            });
        }

        public void Error(string msg, Dictionary<string, object> args = null)
        {
            WriteLine(new LogMessage()
            {
                Message = msg,
                LogLevel = LogLevel.ERROR,
                Data = args,
            });
        }

        private void WriteLine(LogMessage message)
        {
            Console.WriteLine(message.ToString());
        }
    }

    public class LogMessage
    {
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Source = "Containerizer";
        public string Timestamp;
        private readonly JsonSerializerSettings _serializerSettings;

        public LogMessage()
        {
            _serializerSettings = new JsonSerializerSettings {ContractResolver = new UnderscoreMappingResolver()};
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Timestamp = string.Format("{0:F9}", (DateTime.UtcNow - epochStart).Ticks/10000000.0);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, _serializerSettings);
        }
    }

    public class UnderscoreMappingResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                propertyName, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToLower();
        }
    }
}
