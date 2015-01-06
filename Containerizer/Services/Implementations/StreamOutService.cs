#region

using System.IO;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Services.Implementations
{
    public class StreamOutService : IStreamOutService
    {
        private readonly IContainerPathService containerPathService;
        private readonly ITarStreamService tarStreamService;

        public StreamOutService(IContainerPathService containerPathService, ITarStreamService tarStreamService)
        {
            this.containerPathService = containerPathService;
            this.tarStreamService = tarStreamService;
        }

        public Stream StreamOutFile(string id, string source)
        {
            string rootDir = containerPathService.GetContainerRoot(id);
            string path = rootDir + source;
            Stream stream = tarStreamService.WriteTarToStream(path);
            return stream;
        }
    }
}