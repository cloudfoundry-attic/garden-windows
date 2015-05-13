#region

using System.IO;
using Containerizer.Services.Implementations;
using IronFrame;

#endregion

namespace Containerizer.Services.Interfaces
{
    public class TarOwner
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public interface ITarStreamService
    {
        Stream WriteTarToStream(string filePath);
        void WriteTarStreamToPath(Stream steam, IContainer container, string filePath);
    }
}