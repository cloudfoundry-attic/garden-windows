#region

using System;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class PropertiesControllerSpec : nspec
    {
        private void describe_()
        {
            before = () =>
            {

            };

            describe["Index"] = () =>
            {

            };

            describe["Show"] = () =>
            {
                IHttpActionResult result = null;
                string propertyValue = null;

                before = () =>
                {
                    var mockMetadataService = new Mock<IMetadataService>();
                    var propertiesController = new PropertiesController(mockMetadataService.Object)
                    {
                        Configuration = new HttpConfiguration(),
                        Request = new HttpRequestMessage()
                    };
                    string containerId = Guid.NewGuid().ToString();
                    propertyValue = "a lion, a hippo, the number 25";
                    mockMetadataService.Setup(x => x.GetMetadata(It.IsIn(new[] { containerId }), It.IsIn(new[] { "key" })))
                        .Returns(() =>
                        {
                            return propertyValue;
                        });

                    result = propertiesController
                        .Show(containerId, "key").GetAwaiter().GetResult();
                };


                it["returns a successful status code"] = () =>
                {
                    result.ExecuteAsync(new CancellationToken()).Result.IsSuccessStatusCode.should_be_true();
                };

                it["returns the correct property value"] = () =>
                {
                    result
                        .ExecuteAsync(new CancellationToken())
                        .Result
                        .Content
                        .ReadAsJson()["value"]
                        .ToString()
                        .should_be(propertyValue);
                };
            };

            describe["Update"] = () =>
            {

            };

            describe["Destroy"] = () =>
            {

            };

        }
    }
}