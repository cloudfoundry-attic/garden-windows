using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using Microsoft.Web.Administration;
using Moq;
using NSpec;

namespace Containerizer.Tests.Specs.Services
{
    class PropertyServiceSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerPathService> mockPathService = null;
            PropertyService propService = null;
            string handle = null;
            string baseFileName = null;
            before = () =>
            {
                handle = Guid.NewGuid().ToString();
                mockPathService = new Mock<IContainerPathService>();
                baseFileName = Path.GetTempFileName();
                mockPathService.Setup(x => x.GetContainerRoot(handle)).Returns(baseFileName);
                propService = new PropertyService(mockPathService.Object);
            };

            after = () =>
            {
                if (File.Exists(baseFileName + ".json")) File.Delete(baseFileName + ".json");
            };

            it["delete me"] = () =>
            {
                if (File.Exists("C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\this.json")) File.Delete("C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\this.json");
                if (File.Exists("C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\that.json")) File.Delete("C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\that.json");

                mockPathService.Setup(x => x.GetContainerRoot("this")).Returns("C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\this");
                mockPathService.Setup(x => x.GetContainerRoot("that")).Returns("C:\\Users\\Administrator\\AppData\\Local\\Temp\\2\\that");

                var properties = new Dictionary<string, string>
                {
                    { "mysecret", "dontread" },
                    { "hello", "goodbye" },
                };

                propService.BulkSet("this", properties);
                propService.GetAll("this").should_be(properties);
                expect<FileNotFoundException>(() => propService.GetAll("that"))();

                propService.Set("that", "mysecret", "fred");
                propService.Get("that", "mysecret").should_be("fred");
                propService.Get("this", "mysecret").should_be("dontread");

                propService.Set("this", "mysecret", "jane");
                propService.Get("that", "mysecret").should_be("fred");
                propService.Get("this", "mysecret").should_be("jane");

                propService.Destroy("that", "mysecret");
                propService.Get("this", "mysecret").should_be("jane");
                expect<KeyNotFoundException>(() => propService.Get("that", "mysecret"))();
            };


            describe["#BulkSet"] = () =>
            {
                it["stores the dictionary to disk"] = () =>
                {
                    propService.BulkSet(handle, new Dictionary<string, string>
                    {
                        { "mysecret", "dontread" },
                    });

                    File.ReadAllText(baseFileName + ".json").should_be(
                        "{\"mysecret\":\"dontread\"}"
                        );
                };
            };

            describe["#Set"] = () =>
            {
                it["stores the dictionary to disk"] = () =>
                {
                    propService.Set(handle, "mysecret", "dontread");
                    File.ReadAllText(baseFileName + ".json").should_be(
                        "{\"mysecret\":\"dontread\"}"
                        );
                };

                it["adds to existing properties dictionary"] = () =>
                {
                    File.WriteAllText(baseFileName + ".json", "{\"mysecret\":\"dontread\"}");
                    propService.Set(handle, "anothersecret", "durst");
                    File.ReadAllText(baseFileName + ".json").should_be(
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
                    before = () => File.WriteAllText(baseFileName + ".json", "{\"mysecret\":\"dontread\"}");
                    it["raises an error"] = () => expect<KeyNotFoundException>(() => propService.Get(handle, "key"))();
                };

                context["file and key exist"] = () =>
                {
                    before = () => File.WriteAllText(baseFileName + ".json", "{\"mysecret\":\"dontread\"}");
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
                    before = () => File.WriteAllText(baseFileName + ".json", "{\"mysecret\":\"dontread\"}");
                    it["raises an error"] = () => expect<KeyNotFoundException>(() => propService.Destroy(handle, "key"))();
                };

                context["file and key exist"] = () =>
                {
                    before = () => File.WriteAllText(baseFileName + ".json", "{\"mysecret\":\"dontread\",\"another\":\"text\"}");
                    it["removes the key-value pair"] = () =>
                    {
                        propService.Destroy(handle, "mysecret");
                        File.ReadAllText(baseFileName + ".json").should_be("{\"another\":\"text\"}");
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
                    before = () => File.WriteAllText(baseFileName + ".json", "{\"mysecret\":\"dontread\",\"another\":\"text\"}");
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
