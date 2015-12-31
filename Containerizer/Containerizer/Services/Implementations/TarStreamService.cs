#region

using System.Diagnostics;
using System.IO;
using Containerizer.Properties;
using Containerizer.Services.Interfaces;
using IronFrame;

#endregion

namespace Containerizer.Services.Implementations
{
    public class TarStreamService : ITarStreamService
    {

        private static string TarArchiverPath()
        {
            return Path.Combine(Path.GetTempPath(), "7za.exe");
        }

        public TarStreamService()
        {
            File.WriteAllBytes(TarArchiverPath(), Resources.SevenZip);
        }

        public Stream WriteTarToStream(string filePath)
        {
            string tarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gz");
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
            var process = new Process();
            var processStartInfo = process.StartInfo;
            processStartInfo.FileName = TarArchiverPath();
            processStartInfo.Arguments = "x -y -ttar -si -o" + filePath;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardInput = true;

            process.Start();

            using (var stdin = process.StandardInput)
            {
                stream.CopyTo(stdin.BaseStream);
            }

            process.WaitForExit();

            //container.ImpersonateContainerUser(() => reader.WriteAllToDirectory(filePath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite));
        }

        public void CreateTarFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            var pattern = "";
            if (File.GetAttributes(sourceDirectoryName).HasFlag(FileAttributes.Directory))
            {
                pattern = @"/*";
            }
            var process = new Process();
            var processStartInfo = process.StartInfo;
            processStartInfo.FileName = TarArchiverPath();
            processStartInfo.Arguments = "a -y -ttar " + destinationArchiveFileName + " " + sourceDirectoryName + pattern;
            processStartInfo.UseShellExecute = false;

            process.Start();
            process.WaitForExit();

        }
    }
}