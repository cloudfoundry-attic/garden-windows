using System;
using System.Net.Mime;
using NSpec;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Containerizer;
using SharpCompress.Archive;
using SharpCompress.Reader;
using SharpCompress.Common.Tar;

namespace Containerizer.Tests
{
    class TarStreamServiceSpec : nspec
    {
        string tmpDir;
        Stream tgzStream;
        TarStreamService tarStreamService;

        void before_each()
        {
            tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tmpDir);
            tarStreamService = new TarStreamService();
        }

        void after_each()
        {
            Directory.Delete(tmpDir, true);
        }

        void describe_WriteTarStreamToPath()
        {
            string destinationArchiveFileName = null;

            before = () =>
            {
                destinationArchiveFileName = Path.GetRandomFileName();
                Directory.CreateDirectory(tmpDir);
                Directory.CreateDirectory(Path.Combine(tmpDir, "fooDir"));
                File.WriteAllText(Path.Combine(tmpDir, "content.txt"), "content");
                File.WriteAllText(Path.Combine(tmpDir, "fooDir", "content.txt"), "MOAR content");
                new TarStreamService().CreateFromDirectory(tmpDir, destinationArchiveFileName);
                tgzStream = new FileStream(destinationArchiveFileName, FileMode.Open);
            };

            context["when the tar stream contains files and directories"] = () =>
            {
                it["writes the file to disk"] = () =>
                {
                    tarStreamService.WriteTarStreamToPath(tgzStream, "output");
                    File.ReadAllLines(Path.Combine("output", "content.txt")).should_be("content");
                    File.ReadAllLines(Path.Combine("output", "fooDir", "content.txt")).should_be("MOAR content");
                };
            };

            after = () =>
            {
                tgzStream.Close();
                File.Delete(destinationArchiveFileName);
            };
        }
        void describe_CreateFromDirectory()
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
                    tgzStream = tarStreamService.WriteTarToStream(Path.Combine(tmpDir, "a_file.txt"));
                };

                it["returns a steam with a single requested file"] = () =>
                {
                    using (var tgz = ReaderFactory.Open(tgzStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_file.txt");

                        tgz.MoveToNextEntry().should_be_false();
                    }
                };

            };

            context["requesting a directory"] = () =>
            {
                before = () =>
                {
                    tgzStream = tarStreamService.WriteTarToStream(tmpDir);
                };

                it["creates the tgz stream"] = () =>
                {
                    ReaderFactory.Open(tgzStream).should_not_be_null();
                };

                it["returns a stream with the files inside"] = () =>
                {
                    using (var tgz = ReaderFactory.Open(tgzStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_file.txt");

                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_dir/another_file.txt");
                    }
                };

                it["has content in the files"] = () =>
                {
                    using (var tgz = ReaderFactory.Open(tgzStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_file.txt");
                        GetString(tgz.OpenEntryStream(), tgz.Entry.Size).should_be("Some exciting text");
                    }
                };
            };
        }

        static string GetString(Stream stream, long size)
        {
            byte[] bytes = new byte[size];
            stream.Read(bytes, 0, bytes.Length);
            return System.Text.Encoding.Default.GetString(bytes);
        }
    }
}