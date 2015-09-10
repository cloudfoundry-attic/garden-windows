using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Logger.Tests
{
    [TestClass]
    public class LogMessageTests
    {
        [TestMethod]
        public void ItConvertsFieldsToUnderscorized()
        {
                var logMessage = new LogMessage
                {
                    Message = "Hello, world!",
                    LogLevel = LogLevel.FATAL
                };
                var jsonMessage = logMessage.ToString();
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonMessage);
                Assert.AreEqual("Hello, world!", dictionary["message"]);
                Assert.AreEqual(LogLevel.FATAL.ToString("D"), dictionary["log_level"]);
        }

        [TestMethod]
        public void ItFormatsTimestampsAsExpected()
        {
                var logMessage = new LogMessage
                {
                    Message = "Hello, world!",
                };
                var jsonMessage = logMessage.ToString();
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonMessage);
                var regex = new Regex(@"\d+\.\d{9}");
                StringAssert.Matches(dictionary["timestamp"], regex);
        }
    }
}