using System;
using System.Collections.Generic;
using NSpec;
using System.Linq;
using System.Web.Http.Results;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;
using Containerizer.Services.Interfaces;
using Moq;

namespace Containerizer.Tests
{
    class ContainersControllerSpec : nspec
    {
        Containerizer.Controllers.ContainersController containersController;
        Mock<ICreateContainerService> mockCreateContainerService;

        void before_each()
        {
            mockCreateContainerService = new Mock<ICreateContainerService>();
            containersController = new Controllers.ContainersController(mockCreateContainerService.Object);
            containersController.Configuration = new System.Web.Http.HttpConfiguration();
            containersController.Request = new HttpRequestMessage();
        }

        void describe_post()
        {
            context["when the container is created successfully"] = () =>
            {
                string containerId = null;

                before = () =>
                {
                    containerId = Guid.NewGuid().ToString();
                    mockCreateContainerService.Setup(x => x.CreateContainer()).ReturnsAsync(containerId);
                };

                it["returns a successful status code"] = () =>
                {
                    var postTask = containersController.Post();
                    postTask.Wait();
                    var resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    resultTask.Result.IsSuccessStatusCode.should_be_true();
                };

                it["returns the container's id"] = () =>
                {
                    var postTask = containersController.Post();
                    postTask.Wait();
                    var resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    var readTask = resultTask.Result.Content.ReadAsStringAsync();
                    readTask.Wait();
                    var json = JObject.Parse(readTask.Result);
                    json["id"].ToString().should_be(containerId);
                };
            };
            context["when the container is not created successfully"] = () =>
            {
                before = () =>
                {
                    mockCreateContainerService.Setup(x => x.CreateContainer()).ThrowsAsync(new CouldNotCreateContainerException(null));
                };

                it["returns a error status code"] = () =>
                {
                    var postTask = containersController.Post();
                    postTask.Wait();
                    var resultTask = postTask.Result.ExecuteAsync(new CancellationToken());
                    resultTask.Wait();
                    resultTask.Result.IsSuccessStatusCode.should_be_false();
                };

            };
        }
    }
}


