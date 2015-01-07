#region

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Http;
using Containerizer.Controllers;
using NSpec;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class PingControllerSpec : nspec
    {

        private void describe_()
        {
            PingController pingController = null;
            before = () =>
            {
                pingController = new PingController
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };

            };

            describe[Controller.Show] = () =>
            {
                IHttpActionResult result = null;
                before = () =>
                {
                    result = pingController.Show();
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };

                it["returns OK"] = () =>
                {
                    string jsonString = result.ExecuteAsync(new CancellationToken()).Result.Content.ReadAsString();
                    jsonString.should_be("\"OK\"");
                };
            };
        }
    }
}