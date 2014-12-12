using System.IO;
using System.Text;
using Containerizer.Services.Implementations;
using NSpec;
using SharpCompress.Reader;

namespace Containerizer.Tests.Specs.Services
{
    internal class TarStreamServiceSpec : nspec
    {
        private TarStreamService tarStreamService;
        private Stream tarStream;
        private string tmpDir;

        private void before_each()
        {
            tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tmpDir);
            tarStreamService = new TarStreamService();
        }

        private void after_each()
        {
            Directory.Delete(tmpDir, true);
        }

        private void describe_WriteTarStreamToPath()
        {
            string destinationArchiveFileName = null;

            before = () =>
            {
                destinationArchiveFileName = Path.GetRandomFileName();
                Directory.CreateDirectory(tmpDir);
                Directory.CreateDirectory(Path.Combine(tmpDir, "fooDir"));
                File.WriteAllText(Path.Combine(tmpDir, "content.txt"), "content");
                File.WriteAllText(Path.Combine(tmpDir, "fooDir", "content.txt"), "MOAR content");
                new TarStreamService().CreateTarFromDirectory(tmpDir, destinationArchiveFileName);
                tarStream = new FileStream(destinationArchiveFileName, FileMode.Open);
            };

            context["when the tar stream contains files and directories"] = () =>
            {
                it["writes the file to disk"] = () =>
                {
                    tarStreamService.WriteTarStreamToPath(tarStream, "output");
                    File.ReadAllLines(Path.Combine("output", "content.txt")).should_be("content");
                    File.ReadAllLines(Path.Combine("output", "fooDir", "content.txt")).should_be("MOAR content");
                };
            };

            after = () =>
            {
                tarStream.Close();
                File.Delete(destinationArchiveFileName);
            };
        }

        private void describe_WriteTarGzStreamToPath()
        {
            string destinationArchiveFileName = null;

            before = () =>
            {
                destinationArchiveFileName = Path.GetRandomFileName();
                Directory.CreateDirectory(tmpDir);
                Directory.CreateDirectory(Path.Combine(tmpDir, "fooDir"));
                File.WriteAllText(Path.Combine(tmpDir, "content.txt"), "content");
                File.WriteAllText(Path.Combine(tmpDir, "fooDir", "content.txt"), "MOAR content");
                new TarStreamService().CreateTarGzFromDirectory(tmpDir, destinationArchiveFileName);
                tarStream = new FileStream(destinationArchiveFileName, FileMode.Open);
            };

            context["when the tar stream contains files and directories"] = () =>
            {
                it["writes the file to disk"] = () =>
                {
                    tarStreamService.WriteTarStreamToPath(tarStream, "output");
                    File.ReadAllLines(Path.Combine("output", "content.txt")).should_be("content");
                    File.ReadAllLines(Path.Combine("output", "fooDir", "content.txt")).should_be("MOAR content");
                };
            };

            after = () =>
            {
                tarStream.Close();
                File.Delete(destinationArchiveFileName);
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
                before = () => { tarStream = tarStreamService.WriteTarToStream(Path.Combine(tmpDir, "a_file.txt")); };

                it["returns a steam with a single requested file"] = () =>
                {
                    using (IReader tgz = ReaderFactory.Open(tarStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_file.txt");

                        tgz.MoveToNextEntry().should_be_false();
                    }
                };
            };

            context["requesting a directory"] = () =>
            {
                before = () => { tarStream = tarStreamService.WriteTarToStream(tmpDir); };

                it["creates the tgz stream"] = () => { ReaderFactory.Open(tarStream).should_not_be_null(); };

                it["returns a stream with the files inside"] = () =>
                {
                    using (IReader tgz = ReaderFactory.Open(tarStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_file.txt");

                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_dir/another_file.txt");
                    }
                };

                it["has content in the files"] = () =>
                {
                    using (IReader tgz = ReaderFactory.Open(tarStream))
                    {
                        tgz.MoveToNextEntry().should_be_true();
                        tgz.Entry.Key.should_be("a_file.txt");
                        GetString(tgz.OpenEntryStream(), tgz.Entry.Size).should_be("Some exciting text");
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