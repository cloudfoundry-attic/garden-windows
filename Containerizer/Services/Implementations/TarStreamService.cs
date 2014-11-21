using Containerizer.Services.Interfaces;
using SharpCompress.Common;
using SharpCompress.Reader;
using SharpCompress.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer
{
    public class TarStreamService : ITarStreamService
    {
        public System.IO.Stream WriteTarToStream(string filePath)
        {
            var gzPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gz");
            CreateFromDirectory(filePath, gzPath);
            Stream stream = File.OpenRead(gzPath);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            var memStream = new MemoryStream(buffer);
            stream.Close();
            File.Delete(gzPath);
            return memStream;
        }

        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            var tarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tar");
            try
            {
                using (Stream stream = File.OpenWrite(tarPath))
                {
                    using (var writer = WriterFactory.Open(stream, ArchiveType.Tar, new CompressionInfo { Type = CompressionType.None }))
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
                    using (var writer = WriterFactory.Open(stream, ArchiveType.GZip, new CompressionInfo { Type = CompressionType.GZip }))
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


        public void WriteTarStreamToPath(Stream stream, string filePath)
        {
            var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    reader.WriteEntryToDirectory(filePath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }
        }
    }
}

