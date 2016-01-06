#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Containerizer.Tests.Properties;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanStreamInAndStreamOut : nspec
    {
        private string handle;
        private Helpers.ContainerizerProcess process;
        private Stream tarStream;

        private static string TarArchiverPath(string filename)
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            return Path.Combine(Path.GetDirectoryName(uri.LocalPath), filename);
        }

        private void before_all()
        {
            File.WriteAllBytes(TarArchiverPath("tar.exe"), Resources.bsdtar);
            File.WriteAllBytes(TarArchiverPath("bzip2.dll"), Resources.bzip2);
            File.WriteAllBytes(TarArchiverPath("libarchive2.dll"), Resources.libarchive2);
            File.WriteAllBytes(TarArchiverPath("zlib1.dll"), Resources.zlib1);
        }

        private void before_each()
        {
            process = Helpers.CreateContainerizerProcess();
            tarStream = new MemoryStream(Resources.fixture1);
        }

        private void after_each()
        {
            process.Dispose();
        }

        private string WriteTarToDirectory(Stream tarStream)
        {
            var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tmpPath);
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = TarArchiverPath("tar.exe"),
                    Arguments = "xf - -C" + tmpPath,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            using (var stdin = process.StandardInput)
            {
                tarStream.CopyTo(stdin.BaseStream);
            }
            process.WaitForExit();
            return tmpPath;
        }

        private void describe_stream_in()
        {
            context["given that I'm a consumer of the containerizer api"] = () =>
            {
                HttpClient client = null;

                before = () => client = process.GetClient();

                context["there exists a container with a given id"] = () =>
                {
                    before = () => handle = Helpers.CreateContainer(client);
                    after = () => Helpers.DestroyContainer(client, handle);

                    context["when I stream in a file into the container"] = () =>
                    {
                        HttpResponseMessage responseMessage = null;

                        before = () =>
                        {
                            responseMessage = Helpers.StreamIn(handle: handle, tarStream: tarStream, client: client);
                        };

                        it["returns a tarred version of the file"] = () =>
                        {
                            responseMessage.IsSuccessStatusCode.should_be_true();

                            HttpResponseMessage getTask =
                                client.GetAsync("/api/containers/" + handle + "/files?source=/test/file.txt").Result;
                            getTask.IsSuccessStatusCode.should_be_true();

                            var resultStream = getTask.Content.ReadAsStreamAsync().Result;
                            var extracted = WriteTarToDirectory(resultStream);
                            var listing = Directory.GetFileSystemEntries(extracted).Select(Path.GetFileName);
                            listing.Count().should_be(1);
                            listing.should_contain("file.txt");
                        };

                        it["allows files that have filenames > 100 characters"] = () =>
                        {
                            var filename = 'l' + new String('o', 100) + "ngfile.txt";
                            var getTask =
                                client.GetAsync("/api/containers/" + handle + "/files?source=/test/" + filename).Result;
                            getTask.StatusCode.should_be(HttpStatusCode.OK);
                            var resultStream = getTask.Content.ReadAsStreamAsync().Result;
                            var extracted = WriteTarToDirectory(resultStream);
                            var listing = Directory.GetFileSystemEntries(extracted).Select(Path.GetFileName);
                            listing.should_contain(filename);
                        };
                    };
                };
            };
        }

    }
}
