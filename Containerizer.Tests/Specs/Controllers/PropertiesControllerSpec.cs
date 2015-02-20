#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;
using IronFoundry.Container;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class PropertiesControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerPropertyService> mockPropertyService = null;
            Mock<IContainerService> mockContainerService = null;
            Mock<IContainer> mockContainer = null;
            PropertiesController propertiesController = null;
            string containerHandle = null;
            string key = null;
            string value = null;

            before = () =>
            {
                mockPropertyService = new Mock<IContainerPropertyService>();
                mockContainerService = new Mock<IContainerService>();
                mockContainer = new Mock<IContainer>();
                propertiesController = new PropertiesController(mockPropertyService.Object, mockContainerService.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
                containerHandle = Guid.NewGuid().ToString();
                key = "some:key";
                value = "value";

                mockContainerService.Setup(x => x.GetContainerByHandle(containerHandle))
                        .Returns(() =>
                        {
                            return mockContainer.Object;
                        });
            };
                 
            describe[Controller.Show] = () =>
            {
                IHttpActionResult result = null;
                string propertyValue = null;

                before = () =>
                {
                    propertyValue = "a lion, a hippo, the number 25";
                    mockPropertyService.Setup(x => x.GetProperty(mockContainer.Object, key))
                        .Returns(() =>
                        {
                            return propertyValue;
                        });

                    result = propertiesController.Show(containerHandle, key).GetAwaiter().GetResult();
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
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

            describe[Controller.Update] = () =>
            {
                IHttpActionResult result = null;
                before = () =>
                {
                    propertiesController.Request.Content = new StringContent(value);
                    result = propertiesController.Update(containerHandle, key).Result;
                };

                it["calls the propertyService set method"] = () =>
                {
                    mockPropertyService.Verify(x => x.SetProperty(mockContainer.Object, key, value));
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };
            };

            describe[Controller.Destroy] = () =>
            {
                IHttpActionResult result = null;
                before = () =>
                {
                    result = propertiesController.Destroy(containerHandle, key).Result;
                };

                it["calls the propertyService destroy method"] = () =>
                {
                    mockPropertyService.Verify(x => x.RemoveProperty(mockContainer.Object, key));
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };
            };
        }
    }
}