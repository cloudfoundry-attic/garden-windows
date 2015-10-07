#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Controllers;
using Moq;
using NSpec;
using IronFrame;
using System.Web.Http.Results;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class PropertiesControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            Mock<IContainer> mockContainer = null;
            PropertiesController propertiesController = null;
            string containerHandle = null;
            string key = null;
            string value = null;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                mockContainer = new Mock<IContainer>();
                propertiesController = new PropertiesController(mockContainerService.Object)
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
                            return mockContainer != null ? mockContainer.Object : null;
                        });
            };

            describe[Controller.Index] = () =>
            {
                IHttpActionResult result = null;
                Dictionary<string,string> properties = null;

                before = () =>
                {
                    properties = new Dictionary<string,string> { 
                        { "wardrobe", "a lion" },
                        { "a hippo", "the number 25" }
                    };
                    mockContainer.Setup(x => x.GetProperties()).Returns(() => properties);
                };

                act = () => result = propertiesController.Index(containerHandle);

                it["returns the correct property value"] = () =>
                {
                    var jsonResult = result.should_cast_to<JsonResult<Dictionary<string,string>>>();
                    jsonResult.Content.should_be(properties);
                };

                context["the container doesn't exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainer = null;
                    };

                    it["returns a 404"] = () =>
                    {
                        var message = result.should_cast_to<ResponseMessageResult>();
                        message.Response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };
            };

            describe[Controller.Show] = () =>
            {
                IHttpActionResult result = null;
                string propertyValue = null;

                before = () =>
                {
                    propertyValue = "a lion, a hippo, the number 25";
                    mockContainer.Setup(x => x.GetProperty(key)).Returns(() => propertyValue);
                };

                act = () => result = propertiesController.Show(containerHandle, key);

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };

                it["returns the correct property value"] = () =>
                {
                    var actualValue = result.should_cast_to<JsonResult<string>>();
                    actualValue.Content.should_be(propertyValue);
                };

                context["when the property does not exist"] = () =>
                {
                    before = () => propertyValue = null;
                    it["returns a 404"] = () =>
                    {
                        var message = result.should_cast_to<ResponseMessageResult>();
                        message.Response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => mockContainer = null;
                    it["returns a 404"] = () =>
                    {
                        var message = result.should_cast_to<ResponseMessageResult>();
                        message.Response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
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
                    mockContainer.Verify(x => x.SetProperty(key, value));
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };
            };

            describe[Controller.Destroy] = () =>
            {
                IHttpActionResult result = null;
                act = () =>
                {
                    result = propertiesController.Destroy(containerHandle, key);
                };

                it["calls the propertyService destroy method"] = () =>
                {
                    mockContainer.Verify(x => x.RemoveProperty(key));
                };

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };

                context["the container doesn't exist"] = () =>
                {
                    before = () =>
                    {
                        mockContainer = null;
                    };

                    it["returns a 404"] = () =>
                    {
                        var message = result.should_cast_to<ResponseMessageResult>();
                        message.Response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };
            };
        }
    }
}