using System.IO;
using Containerizer.Services.Interfaces;
using SharpCompress.Common;
using SharpCompress.Reader;
using SharpCompress.Writer;

namespace Containerizer.Services.Implementations
{
    public class TarStreamService : ITarStreamService
    {
        public Stream WriteTarToStream(string filePath)
        {
            string gzPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gz");
            CreateFromDirectory(filePath, gzPath);
            Stream stream = File.OpenRead(gzPath);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int) stream.Length);
            var memStream = new MemoryStream(buffer);
            stream.Close();
            File.Delete(gzPath);
            return memStream;
        }


        public void WriteTarStreamToPath(Stream stream, string filePath)
        {
            IReader reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    reader.WriteEntryToDirectory(filePath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }
        }

        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            string tarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tar");
            try
            {
                using (Stream stream = File.OpenWrite(tarPath))
                {
                    using (
                        IWriter writer = WriterFactory.Open(stream, ArchiveType.Tar,
                            new CompressionInfo {Type = CompressionType.None}))
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
                            new CompressionInfo {Type = CompressionType.GZip}))
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