#region

using System;
using System.IO;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;
using Moq;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Services
{
    internal class StreamInServiceSpec : nspec
    {
        private string id;
        private Mock<ITarStreamService> mockITarStreamService;
        private Mock<IContainerService> mockIContainerService;
        private Mock<IContainer> mockIContainer;
        private Mock<IContainerDirectory> mockIContainerDirectory;
        private StreamInService streamInService;

        private void before_each()
        {
            mockITarStreamService = new Mock<ITarStreamService>();
            mockIContainerService = new Mock<IContainerService>();
            mockIContainer = new Mock<IContainer>();
            mockIContainerDirectory = new Mock<IContainerDirectory>();

            streamInService = new StreamInService(mockIContainerService.Object, mockITarStreamService.Object);
            id = Guid.NewGuid().ToString();
        }

        private void describe_stream_in()
        {
            Stream stream = null;

            before = () =>
            {
                mockIContainerService.Setup(x => x.GetContainerByHandle(It.IsAny<string>())).Returns(mockIContainer.Object);
                mockIContainer.Setup(x => x.Directory).Returns(mockIContainerDirectory.Object);
                mockIContainerDirectory.Setup(x => x.MapUserPath("file.txt")).Returns(@"C:\a\path\file.txt");

                stream = new MemoryStream();
                streamInService.StreamInFile(stream, id, "file.txt");
            };

            it["passes through its stream and combined path to tarstreamer"] = () =>
            {
                Func<Stream, bool> verifyStream = x =>
                {
                    return stream.Equals(x);
                };

                Func<String, bool> verifyPath = x =>
                {
                    return x.Equals(Path.Combine(@"C:\a\path", "file.txt"));
                };

                mockITarStreamService.Verify(x => x.WriteTarStreamToPath(
                    It.Is((Stream y) => verifyStream(y)),
                    It.Is((String y) => verifyPath(y))
                    ));
            };
        }
    }
}