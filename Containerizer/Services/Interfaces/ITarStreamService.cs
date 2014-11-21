using System.IO;

namespace Containerizer.Services.Interfaces
{
    public interface ITarStreamService
    {
        Stream WriteTarToStream(string filePath);
        void WriteTarStreamToPath(Stream steam, string filePath);
    }
}