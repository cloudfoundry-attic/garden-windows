using Containerizer.Controllers;
using Containerizer.Facades;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Tests
{
    class ContainerProcessHandlerSpec : nspec
    {
        ContainerProcessHandler handler;
        Mock<IProcessFacade> mockProcess;
        private ProcessStartInfo startInfo;
 
        void before_each()
        {
            mockProcess = new Mock<IProcessFacade>();
            startInfo = new ProcessStartInfo();
            handler = new ContainerProcessHandler(mockProcess.Object);

            mockProcess.Setup(x => x.StartInfo).Returns(startInfo);
            mockProcess.Setup(x => x.Start());

            var bytes = Encoding.UTF8.GetBytes("some text");
            var stream = new StreamReader(new MemoryStream(bytes));
            mockProcess.Setup(x => x.StandardOutput).Returns(stream);
        }

        void describe_onmessage()
        {
            before = () =>
            {
                handler.OnMessage("{\"Path\":\"foo.exe\", \"Args\":[\"some\", \"args\"]}");
            };

            it["sets start info correctly"] = () =>
            {
                startInfo.FileName.should_be("foo.exe");
                startInfo.Arguments.should_be("some args");
            };

            it["runs something"] = () =>
            {
                mockProcess.Verify(x => x.Start());
            };
        }
    }
}
