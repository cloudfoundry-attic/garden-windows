using System.IO;

namespace Containerizer.Services.Interfaces
{
    public interface IStreamOutService
    {
        Stream StreamOutFile(string id, string source);
    }
}