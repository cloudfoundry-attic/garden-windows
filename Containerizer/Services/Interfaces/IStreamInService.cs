using System.IO;

namespace Containerizer.Services.Interfaces
{
    public interface IStreamInService
    {
        void StreamInFile(Stream steam, string id, string destination);
    }
}