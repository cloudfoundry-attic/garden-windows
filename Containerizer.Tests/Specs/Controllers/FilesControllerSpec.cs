#region

using System;
using System.IO;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class FilesControllerSpec : nspec
    {
        private FilesController filesController;
        private Mock<IStreamInService> mockStreamInService;
        private Mock<IStreamOutService> mockStreamOutService;

        private void before_each()
        {
            mockStreamOutService = new Mock<IStreamOutService>();
            mockStreamInService = new Mock<IStreamInService>();
            filesController = new FilesController(mockStreamInService.Object, mockStreamOutService.Object)
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
        }

        private void describe_get_files()
        {
            context["when the file exists"] = () =>
            {
                HttpResponseMessage result = null;
                before = () =>
                {
                    mockStreamOutService.Setup(x => x.StreamOutFile(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns(() =>
                        {
                            var stream = new MemoryStream();
                            var writer = new StreamWriter(stream);
                            writer.Write("hello");
                            writer.Flush();
                            stream.Position = 0;
                            return stream;
                        });

                    result = filesController
                        .Show("guid", "file.txt").GetAwaiter().GetResult();
                };


                it["returns a successful status code"] = () =>
                {
                    result.IsSuccessStatusCode.should_be_true();
                };
            };
        }

        private void describe_put_files()
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
                    filesController.Request.Content = new StreamContent(stream);
                    result = filesController.Create(id, fileName).GetAwaiter().GetResult();
                };

                it["calls the stream in service with the correct stream, passed in id, and file name query parameter"] =
                    () =>
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