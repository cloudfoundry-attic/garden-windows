using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSpec;
using Containerizer.Tests.Specs;

namespace Containerizer.Tests.Specs.Controllers
{
    internal class PingControllerSpec : nspec
    {
        private PingController pingController;

        private void before_each()
        {
            pingController = new PingController()
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
        }

        private
        void describe_ping()
        {
            HttpResponseMessage result = null;

                before = () =>
                {
                   result = pingController.Ping()
                       .ExecuteAsync(new CancellationToken())
                       .GetAwaiter()
                       .GetResult();
                };

                it["returns a successful status code"] = () =>
                {
                    result.IsSuccessStatusCode.should_be_true();
                };

                it["returns OK"] = () =>
                {
                    var jsonString = result.Content.ReadAsString(); // Json();
                    jsonString.should_be("\"OK\"");
                }; 
        }
    }
}