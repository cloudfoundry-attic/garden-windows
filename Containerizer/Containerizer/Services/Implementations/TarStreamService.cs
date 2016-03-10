#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Containerizer.Properties;
using Containerizer.Services.Interfaces;
using IronFrame;

#endregion

namespace Containerizer.Services.Implementations
{
    public class TarStreamService : ITarStreamService
    {

        private static string TarArchiverPath(string filename)
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            return Path.Combine(Path.GetDirectoryName(uri.LocalPath), filename);
        }

        public Stream WriteTarToStream(string filePath)
        {
            string tarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CreateTarFromDirectory(filePath, tarPath);
            Stream stream = File.OpenRead(tarPath);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            var memStream = new MemoryStream(buffer);
            stream.Close();
            File.Delete(tarPath);
            return memStream;
        }

        public void WriteTarStreamToPath(Stream stream, IContainer container, string filePath)
        {
            var tmpFilePath = container.Directory.MapBinPath(Path.GetRandomFileName());
            Directory.CreateDirectory(filePath);
            using (var tmpFile = File.Create(tmpFilePath))
            {
                stream.CopyTo(tmpFile);
            }
            var pSpec = new ProcessSpec()
            {
                ExecutablePath = TarArchiverPath("tar.exe"),
                Arguments = new []{"xf", tmpFilePath, "-C", filePath},
            };
            var process = container.Run(pSpec, null);
            var exitCode = process.WaitForExit();
            if (exitCode != 0)
                throw new Exception("Failed to extract stream");
            File.Delete(tmpFilePath);
        }

        public void CreateTarFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            string file;
            string directory;
            if (File.GetAttributes(sourceDirectoryName).HasFlag(FileAttributes.Directory))
            {
                file = ".";
                directory = sourceDirectoryName;
            }
            else
            {
                file = Path.GetFileName(sourceDirectoryName);
                directory = Path.GetDirectoryName(sourceDirectoryName);
            }
            var process = new Process();
            var processStartInfo = process.StartInfo;
            processStartInfo.FileName = TarArchiverPath("tar.exe");
            processStartInfo.Arguments = "cf " + destinationArchiveFileName + " -C " + directory + " " + file;
            processStartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();
            var exitCode = process.ExitCode;
            if (exitCode != 0)
            {
                throw new Exception("Failed to create archive");
            }
        }
    }
}
