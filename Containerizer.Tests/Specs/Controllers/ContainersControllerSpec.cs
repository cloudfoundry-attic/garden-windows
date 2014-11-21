using System;
using System.Collections.Generic;
using System.IO;
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
        Mock<IStreamOutService> mockStreamOutService;
        Mock<IStreamInService> mockStreamInService;

        void before_each()
        {
            mockCreateContainerService = new Mock<ICreateContainerService>();
            mockStreamOutService = new Mock<IStreamOutService>();
            mockStreamInService = new Mock<IStreamInService>();
            containersController = new Controllers.ContainersController(mockCreateContainerService.Object, mockStreamInService.Object, mockStreamOutService.Object);
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
                    mockCreateContainerService.Setup(x => x.CreateContainer()).ThrowsAsync(new CouldNotCreateContainerException(String.Empty, null));
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

        void describe_get_files()
        {
            context["when the file exists"] = () =>
            {
                HttpResponseMessage result = null;
                before = () =>
                {
                    mockStreamOutService.Setup(x => x.StreamOutFile(It.IsAny<string>(), It.IsAny<string>() )).Returns(() =>
                    {

                        var stream = new MemoryStream();
                        var writer = new StreamWriter(stream);
                        writer.Write("hello");
                        writer.Flush();
                        stream.Position = 0;
                        return stream;
                    });

                    result = containersController
                        .StreamOut("guid", "file.txt").GetAwaiter().GetResult();
                };


                it["returns a successful status code"] = () =>
                {
                    result.IsSuccessStatusCode.should_be_true();
                };
            };
        }

        void describe_put_files()
        {
            context["when it receives a new file"] = () =>
            {
                string id = null;
                const string fileName = "file.txt";
                HttpResponseMessage result = null;
                string content = null;

                before = () =>
                {
                    id = Guid.NewGuid().ToString();
                    Stream stream = new MemoryStream();
                    var sr = new StreamWriter(stream);
                    content = Guid.NewGuid().ToString();
                    sr.Write(content);
                    sr.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    containersController.Request.Content = new StreamContent(stream);
                    result = containersController.StreamIn(id, fileName).GetAwaiter().GetResult();
                };

                it["calls the stream in service with the correct stream, passed in id, and file name query parameter"] = () =>
                {
                    mockStreamInService.Verify(x => x.StreamInFile(
                        It.Is((Stream y) => new StreamReader(y).ReadToEnd() == content),
                        id,
                        fileName));
                };


                it["returns a successful status code"] = () =>
                {
                    result.IsSuccessStatusCode.should_be_true();
                };
            };
        }
    }
}


