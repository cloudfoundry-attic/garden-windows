using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;
using Microsoft.Web.Administration;
using Moq;
using NSpec;

namespace Containerizer.Tests.Specs.Services
{
    class PropertyServiceSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            Mock<IContainer> mockContainer = null;
            Mock<IContainerDirectory> mockIContainerDirectory = null;
            PropertyService propService = null;
            string handle = null;
            string containerDirectory = null;
            before = () =>
            {
                handle = Guid.NewGuid().ToString();
                mockContainerService = new Mock<IContainerService>();
                mockContainer = new Mock<IContainer>();
                mockIContainerDirectory = new Mock<IContainerDirectory>();

                containerDirectory = Path.Combine(Path.GetTempPath(), handle);
                Directory.CreateDirectory(containerDirectory);

                mockContainerService.Setup(x => x.GetContainerByHandle(It.IsAny<string>())).Returns(mockContainer.Object);
                mockContainer.Setup(x => x.Directory).Returns(mockIContainerDirectory.Object);
                mockIContainerDirectory.Setup(x => x.MapUserPath("")).Returns(containerDirectory);

                propService = new PropertyService(mockContainerService.Object);
            };

            after = () =>
            {
                Directory.Delete(containerDirectory, true);
            };

            describe["#BulkSet"] = () =>
            {
                it["stores the dictionary to disk"] = () =>
                {
                    propService.BulkSet(handle, new Dictionary<string, string>
                    {
                        { "mysecret", "dontread" },
                    });

                    File.ReadAllText(Path.Combine(containerDirectory, "properties.json")).should_be(
                        "{\"mysecret\":\"dontread\"}"
                        );
                };

                context["passed in properties are null"] = () =>
                {
                    it["sets an empty hash"] = () =>
                    {
                        propService.BulkSet(handle, null);

                        File.ReadAllText(Path.Combine(containerDirectory, "properties.json")).should_be(
                            "{}"
                        );
                    };
                };
            };

            describe["#BulkSetWithContainerPath"] = () =>
            {
                it["stores the dictionary to disk"] = () =>
                {
                    propService.BulkSetWithContainerPath(containerDirectory, new Dictionary<string, string>
                    {
                        { "mysecret", "dontread" },
                    });

                    File.ReadAllText(Path.Combine(containerDirectory, "properties.json")).should_be(
                        "{\"mysecret\":\"dontread\"}"
                        );
                };
            };

            describe["#Set"] = () =>
            {
                it["stores the dictionary to disk"] = () =>
                {
                    propService.Set(handle, "mysecret", "dontread");
                    File.ReadAllText(Path.Combine(containerDirectory, "properties.json")).should_be(
                        "{\"mysecret\":\"dontread\"}"
                        );
                };

                it["adds to existing properties dictionary"] = () =>
                {
                    File.WriteAllText(Path.Combine(containerDirectory, "properties.json"), "{\"mysecret\":\"dontread\"}");
                    propService.Set(handle, "anothersecret", "durst");
                    File.ReadAllText(Path.Combine(containerDirectory, "properties.json")).should_be(
                        "{\"mysecret\":\"dontread\",\"anothersecret\":\"durst\"}"
                        );
                };
            };

            describe["#Get"] = () =>
            {
                context["file does not exist"] = () =>
                {
                    it["raises an error"] = () => expect<FileNotFoundException>(() => propService.Get(handle, "key"))();
                };

                context["file exists but key does not"] = () =>
                {
                    before = () => File.WriteAllText(Path.Combine(containerDirectory, "properties.json"), "{\"mysecret\":\"dontread\"}");
                    it["raises an error"] = () => expect<KeyNotFoundException>(() => propService.Get(handle, "key"))();
                };

                context["file and key exist"] = () =>
                {
                    before = () => File.WriteAllText(Path.Combine(containerDirectory, "properties.json"), "{\"mysecret\":\"dontread\"}");
                    it["returns the associated value"] = () => propService.Get(handle, "mysecret").should_be("dontread");
                };
            };

            describe["#Destroy"] = () =>
            {
                context["file does not exist"] = () =>
                {
                    it["raises an error"] = () => expect<FileNotFoundException>(() => propService.Destroy(handle, "key"))();
                };

                context["file exists but key does not"] = () =>
                {
                    before = () => File.WriteAllText(Path.Combine(containerDirectory, "properties.json"), "{\"mysecret\":\"dontread\"}");
                    it["raises an error"] = () => expect<KeyNotFoundException>(() => propService.Destroy(handle, "key"))();
                };

                context["file and key exist"] = () =>
                {
                    before = () => File.WriteAllText(Path.Combine(containerDirectory, "properties.json"), "{\"mysecret\":\"dontread\",\"another\":\"text\"}");
                    it["removes the key-value pair"] = () =>
                    {
                        propService.Destroy(handle, "mysecret");
                        File.ReadAllText(Path.Combine(containerDirectory, "properties.json")).should_be("{\"another\":\"text\"}");
                    };
                };
            };

            describe["#GetAll"] = () =>
            {
                context["file does not exist"] = () =>
                {
                    it["raises an error"] = () => expect<FileNotFoundException>(() => propService.GetAll(handle))();
                };

                context["file does exist"] = () =>
                {
                    before = () => File.WriteAllText(Path.Combine(containerDirectory, "properties.json"), "{\"mysecret\":\"dontread\",\"another\":\"text\"}");
                    it["returns the properties"] = () =>
                    {
                        propService.GetAll(handle).should_be(new Dictionary<string, string>
                        {
                            {"mysecret", "dontread"},
                            {"another", "text"},
                        });
                    };
                };
            };

        }
    }
}
