using System;
using System.Collections.Generic;
using NSpec;
using System.Linq;
using System.Web.Http.Results;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;
using Containerizer.Services.Interfaces;
using Containerizer.Services.Implementations;
using Moq;
using Microsoft.Web.Administration;
using System.IO;

namespace Containerizer.Tests
{
    class StreamOutServiceSpec : nspec
    {
        StreamOutService streamOutService;
        private string id;
        private string actualPath;
        private Mock<IContainerPathService> mockIContainerPathService;
        private Mock<ITarStreamService> mockITarStreamService;
        private System.IO.Stream expectedStream;

        void before_each()
        {
            mockIContainerPathService = new Mock<IContainerPathService>();
            mockITarStreamService = new Mock<ITarStreamService>();
            streamOutService = new StreamOutService(mockIContainerPathService.Object, mockITarStreamService.Object);
            id = Guid.NewGuid().ToString();
        }

        void describe_stream_out()
        {
            System.IO.Stream stream = null;
            
            before = () =>
            {
                mockIContainerPathService.Setup(x => x.GetContainerRoot(It.IsAny<string>()))
                    .Returns(() =>  @"C:\a\path" );
                mockITarStreamService.Setup(x => x.WriteTarToStream(It.IsAny<string>()))
                    .Returns((string path) =>
                {
                    expectedStream = new MemoryStream();
                    actualPath = path;
                    return expectedStream;
                });
                stream = streamOutService.StreamFile(id, "file.txt");
            };

            it["returns a stream from the tarstreamer"] = () =>
            {
                stream.should_be_same(expectedStream);
            };

            it["passes the path combined with the id to tarstreamer"] = () =>
            {
                actualPath.should_be(Path.Combine(@"C:\a\path", "file.txt"));
            };
        }
    }
}


