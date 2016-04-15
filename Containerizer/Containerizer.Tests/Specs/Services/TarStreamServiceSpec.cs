#region

using System;
using System.Collections.Generic;
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
        private string outputDir;

        private void before_each()
        {
            tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            outputDir = Path.Combine(tmpDir, "output");

            Directory.CreateDirectory(tmpDir);
            Directory.CreateDirectory(outputDir);

            tarStreamService = new TarStreamService();
        }

        private void after_each()
        {
            Directory.Delete(tmpDir, true);
        }

        private void describe_WriteTarStreamToPath()
        {
            Mock<IContainer> containerMock = null;
            string mappedPath = null;
            string tmpPath = null;

            before = () =>
            {
                containerMock = new Mock<IContainer>();

                mappedPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                var binPath = Path.Combine(mappedPath, "bin");
                Directory.CreateDirectory(binPath);

                containerMock.Setup(x => x.Directory.MapBinPath(It.IsIn(new List<string>(){"tar.exe"}.AsEnumerable())))
                    .Returns(Path.Combine(binPath,"tar.exe"));
                containerMock.Setup(x => x.Directory.MapBinPath(It.IsIn(new List<string>(){"zlib1.dll"}.AsEnumerable())))
                    .Returns(Path.Combine(binPath,"zlib1.dll"));
                containerMock.Setup(x => x.Directory.MapBinPath(It.IsNotIn(new List<string>() {"tar.exe","zlib1.dll"}.AsEnumerable())))
                    .Returns(Path.Combine(binPath, Path.GetRandomFileName()));

                tmpPath = Path.GetTempFileName();
                tarStream = new FileStream(tmpPath, FileMode.Open);
            };

            after = () =>
            {
                tarStream.Close();
                File.Delete(tmpPath);
            };

            context["when copying tar.exe to localPath"] = () =>
            {
                before = () =>
                {
                    var processMock = new Mock<IContainerProcess>();
                    processMock.Setup(x => x.WaitForExit()).Returns(0);

                    containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Returns(processMock.Object);
                };

                act = () => tarStreamService.WriteTarStreamToPath(tarStream, containerMock.Object, outputDir);

                it["expects the tar.exe to exists in container bin directory"] = () =>
                {
                    var tarPath = containerMock.Object.Directory.MapBinPath("tar.exe");
                    File.Exists(tarPath).should_be_true();
                    var zlibPath = containerMock.Object.Directory.MapBinPath("zlib1.dll");
                    File.Exists(zlibPath).should_be_true();
                };
            };

            context["when the tar process returns a zero exit code"] = () =>
            {
                before = () =>
                {
                    var processMock = new Mock<IContainerProcess>();
                    processMock.Setup(x => x.WaitForExit()).Returns(0);

                    containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Returns(processMock.Object);
                };

                act = () => tarStreamService.WriteTarStreamToPath(tarStream, containerMock.Object, outputDir);

                it["extracts the tar to the container"] = () =>
                {
                    var arguments = new[] {"xf", "-C", outputDir};
                    // Verify the processes arguments are a subset of the expected arguments.
                    // We cannot check the temp file argument as it is random.
                    containerMock.Verify(x => x.Run(It.Is<ProcessSpec>(p => !arguments.Except(p.Arguments).Any()), null));
                };
            };

            context["when the tar process returns a non-zero exit code"] = () =>
            {
                before = () =>
                {
                    var processMock = new Mock<IContainerProcess>();
                    processMock.Setup(x => x.WaitForExit()).Returns(1);

                    containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Returns(processMock.Object);
                };

                it["throws an exception"] = () =>
                {
                    var passed = false;
                    try
                    {
                        tarStreamService.WriteTarStreamToPath(tarStream, containerMock.Object, outputDir);
                    } catch(Exception)
                    {
                        passed = true;
                    }

                    passed.should_be_true();
                };
            };
        }

        private void describe_CreateFromDirectory()
        {
            before = () =>
            {
                File.WriteAllText(Path.Combine(tmpDir, "a_file.txt"), "Some exciting text");
                Directory.CreateDirectory(Path.Combine(tmpDir, "a dir"));
                File.WriteAllText(Path.Combine(tmpDir, "a dir", "another_file.txt"), "Some spacey text");
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
                        entries.should_contain("./a_file.txt");
                        entries.should_contain("./a dir/another_file.txt");
                    }
                };

                it["has content in the files"] = () =>
                {
                    using (var tar = ArchiveFactory.Open(tarStream))
                    {
                        var entries = tar.Entries.ToList();
                        entries.Select(x => x.Key).should_contain("./a_file.txt");
                        var aFile = entries.First(x => x.Key == "./a_file.txt");
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