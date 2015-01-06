#region

using System.IO;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface ITarStreamService
    {
        Stream WriteTarToStream(string filePath);
        void WriteTarStreamToPath(Stream steam, string filePath);
    }
}