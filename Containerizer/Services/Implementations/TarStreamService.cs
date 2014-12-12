using System.IO;
using Containerizer.Services.Interfaces;
using SharpCompress.Common;
using SharpCompress.Reader;
using SharpCompress.Reader.Tar;
using SharpCompress.Writer;

namespace Containerizer.Services.Implementations
{
    public class TarStreamService : ITarStreamService
    {
        public Stream WriteTarToStream(string filePath)
        {
            string gzPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gz");
            CreateTarGzFromDirectory(filePath, gzPath);
            Stream stream = File.OpenRead(gzPath);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            var memStream = new MemoryStream(buffer);
            stream.Close();
            File.Delete(gzPath);
            return memStream;
        }


        public void WriteTarStreamToPath(Stream stream, string filePath)
        {
            IReader reader = ReaderFactory.Open(new BufferedStream(stream));
            reader.WriteAllToDirectory(filePath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
        }

        public void CreateTarFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            using (Stream stream = File.OpenWrite(destinationArchiveFileName))
            {
                using (
                    IWriter writer = WriterFactory.Open(stream, ArchiveType.Tar,
                        new CompressionInfo { Type = CompressionType.None }))
                {
                    if (File.Exists(sourceDirectoryName))
                    {
                        var info = new FileInfo(sourceDirectoryName);
                        writer.Write(info.Name, info);
                    }
                    else
                    {
                        writer.WriteAll(sourceDirectoryName, "*", SearchOption.AllDirectories);
                    }
                }
            }
        }

        public void CreateTarGzFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            string tarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tar");
            try
            {
                using (Stream stream = File.OpenWrite(tarPath))
                {
                    using (
                        IWriter writer = WriterFactory.Open(stream, ArchiveType.Tar,
                            new CompressionInfo { Type = CompressionType.None }))
                    {
                        if (File.Exists(sourceDirectoryName))
                        {
                            var info = new FileInfo(sourceDirectoryName);
                            writer.Write(info.Name, info);
                        }
                        else
                        {
                            writer.WriteAll(sourceDirectoryName, "*", SearchOption.AllDirectories);
                        }
                    }
                }
                using (Stream stream = File.OpenWrite(destinationArchiveFileName))
                {
                    using (
                        IWriter writer = WriterFactory.Open(stream, ArchiveType.GZip,
                            new CompressionInfo { Type = CompressionType.GZip }))
                    {
                        writer.Write("Tar.tar", tarPath);
                    }
                }
            }
            finally
            {
                if (File.Exists(tarPath)) File.Delete(tarPath);
            }
        }
    }
}