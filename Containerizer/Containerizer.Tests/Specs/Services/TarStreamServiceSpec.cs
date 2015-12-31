#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Containerizer.Services.Implementations;
using IronFrame;
using Moq;
using NSpec;
using SharpCompress.Archive;
using SharpCompress.Reader;

#endregion

namespace Containerizer.Tests.Specs.Services
{
    internal class TarStreamServiceSpec : nspec
    {
        private Stream tarStream;
        private TarStreamService tarStreamService;
        private string tmpDir;
        private string inputDir;
        private string tarFile;
        private string outputDir;

        private void before_each()
        {
            tmpDir = Path.Combine(@"C:\", Path.GetRandomFileName());
            inputDir = Path.Combine(tmpDir, "input");
            outputDir = Path.Combine(tmpDir, "output");
            tarFile = Path.Combine(tmpDir, "output.tgz");

            Directory.CreateDirectory(tmpDir);
            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(outputDir);

            var proc = Process.Start("icacls", tmpDir + " /grant \"everyone\":(OI)(CI)M");
            proc.WaitForExit();
            proc.ExitCode.should_be(0);
            tarStreamService = new TarStreamService();
        }

        private void after_each()
        {
            Directory.Delete(tmpDir, true);
        }

        private void describe_WriteTarStreamToPath()
        {
            string destinationArchiveFileName = null;
            Mock<IContainer> containerMock = null;
            LocalPrincipalManager userManager = null;
            string username = null;

            before = () =>
            {
                Helpers.AssertAdministratorPrivileges();

                userManager = new LocalPrincipalManager();
                var guid = System.Guid.NewGuid().ToString("N");
                username = "if" + guid.Substring(0, 6);
                var credentials = userManager.CreateUser(username);
                containerMock = new Mock<IContainer>();
                containerMock.Setup(x => x.ImpersonateContainerUser(It.IsAny<Action>())).Callback((Action x) => x());

                Directory.CreateDirectory(Path.Combine(inputDir, "fooDir"));
                File.WriteAllText(Path.Combine(inputDir, "content.txt"), "content");
                File.WriteAllText(Path.Combine(inputDir, "fooDir", "content.txt"), "MOAR content");
                new TarStreamService().CreateTarFromDirectory(inputDir, tarFile);
                tarStream = new FileStream(tarFile, FileMode.Open);
            };

            context["when the tar stream contains files and directories"] = () =>
            {
                act = () => tarStreamService.WriteTarStreamToPath(tarStream, containerMock.Object, outputDir);

                it["writes the file to disk"] = () =>
                {
                    File.ReadAllLines(Path.Combine(outputDir, "content.txt")).should_be("content");
                    File.ReadAllLines(Path.Combine(outputDir, "fooDir", "content.txt")).should_be("MOAR content");
                };
            };

            after = () =>
            {
                tarStream.Close();
                File.Delete(tarFile);
                userManager.DeleteUser(username);
            };
        }

        private void describe_CreateFromDirectory()
        {
            before = () =>
            {
                File.WriteAllText(Path.Combine(tmpDir, "a_file.txt"), "Some exciting text");
                Directory.CreateDirectory(Path.Combine(tmpDir, "a_dir"));
                File.WriteAllText(Path.Combine(tmpDir, "a_dir", "another_file.txt"), "Some different text");
            };

            context["requesting a single file"] = () =>
            {
                before = () =>
                {
                    tarStream = tarStreamService.WriteTarToStream(Path.Combine(tmpDir, "a_file.txt"));
                };

                it["returns a stream with a single requested file"] = () =>
                {
                    using (IReader tar = ReaderFactory.Open(tarStream))
                    {
                        tar.MoveToNextEntry().should_be_true();
                        tar.Entry.Key.should_be("a_file.txt");

                        tar.MoveToNextEntry().should_be_false();
                    }
                };
            };

            context["requesting a directory"] = () =>
            {
                before = () =>
                {
                    tarStream = tarStreamService.WriteTarToStream(tmpDir);
                };

                it["creates the tgz stream"] = () =>
                {
                    ReaderFactory.Open(tarStream).should_not_be_null();
                };

                it["returns a stream with the files inside"] = () =>
                {
                    using (var tar = ArchiveFactory.Open(tarStream))
                    {
                        var entries = tar.Entries.Select(x => x.Key).ToList();
                        entries.should_contain("a_file.txt");
                        entries.should_contain("a_dir/another_file.txt");
                    }
                };

                it["has content in the files"] = () =>
                {
                    using (var tar = ArchiveFactory.Open(tarStream))
                    {
                        var entries = tar.Entries.ToList();
                        var aFile = entries.First(x => x.Key == "a_file.txt");
                        GetString(aFile.OpenEntryStream(), aFile.Size).should_be("Some exciting text");
                    }
                };
            };
        }

        private static string GetString(Stream stream, long size)
        {
            var bytes = new byte[size];
            stream.Read(bytes, 0, bytes.Length);
            return Encoding.Default.GetString(bytes);
        }
    }
}