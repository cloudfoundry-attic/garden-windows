#region

using System.IO;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface IStreamOutService
    {
        Stream StreamOutFile(string handle, string source);
    }
}