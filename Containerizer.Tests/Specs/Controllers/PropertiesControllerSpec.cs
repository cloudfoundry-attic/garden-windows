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
        private Mock<IMetadataService> mockMetadataService;
        private PropertiesController propertiesController;

        private void before_each()
        {
            mockMetadataService = new Mock<IMetadataService>();
            propertiesController = new PropertiesController(mockMetadataService.Object)
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
        }

        private void describe_get_property()
        {
            string containerId = null;
            IHttpActionResult result = null;
            string propertyValue = null;

            before = () =>
            {
                containerId = Guid.NewGuid().ToString();
                propertyValue = "a lion, a hippo, the number 25";
                mockMetadataService.Setup(x => x.GetMetadata(It.IsIn(new[] {containerId}), It.IsIn(new[] {"key"})))
                    .Returns(() =>
                    {
                        return propertyValue;
                    });

                result = propertiesController
                    .GetProperty(containerId, "key").GetAwaiter().GetResult();
            };


            it["returns a successful status code"] =
                () =>
                {
                    result.ExecuteAsync(new CancellationToken()).Result.IsSuccessStatusCode.should_be_true();
                };

            it["returns the correct property value"] = () =>
            {
                result.ExecuteAsync(new CancellationToken()).Result.Content.ReadAsJson()["value"].ToString().should_be(
                    propertyValue);
            };
        }
    }
}