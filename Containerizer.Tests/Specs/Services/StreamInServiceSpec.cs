using System;
using System.IO;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;

namespace Containerizer.Tests.Specs.Services
{
    internal class StreamInServiceSpec : nspec
    {
        private string id;
        private Mock<IContainerPathService> mockIContainerPathService;
        private Mock<ITarStreamService> mockITarStreamService;
        private StreamInService streamInService;

        private void before_each()
        {
            mockIContainerPathService = new Mock<IContainerPathService>();
            mockITarStreamService = new Mock<ITarStreamService>();
            streamInService = new StreamInService(mockIContainerPathService.Object, mockITarStreamService.Object);
            id = Guid.NewGuid().ToString();
        }

        private void describe_stream_in()
        {
            Stream stream = null;

            before = () =>
            {
                mockIContainerPathService.Setup(x => x.GetSubdirectory(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(() => @"C:\a\path\file.txt");
                stream = new MemoryStream();
                streamInService.StreamInFile(stream, id, "file.txt");
            };

            it["passes through its stream and combined path to tarstreamer"] = () =>
            {
                Func<Stream, bool> verifyStream = x => { return stream.Equals(x); };

                Func<String, bool> verifyPath = x => { return x.Equals(Path.Combine(@"C:\a\path", "file.txt")); };

                mockITarStreamService.Verify(x => x.WriteTarStreamToPath(
                    It.Is((Stream y) => verifyStream(y)),
                    It.Is((String y) => verifyPath(y))
                    ));
            };
        }
    }
}