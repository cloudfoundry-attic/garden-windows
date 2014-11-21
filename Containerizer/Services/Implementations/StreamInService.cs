using System.IO;
using Containerizer.Services.Interfaces;

namespace Containerizer.Services.Implementations
{
    public class StreamInService : IStreamInService
    {
        private readonly IContainerPathService containerPathService;
        private readonly ITarStreamService tarStreamService;

        public StreamInService(IContainerPathService containerPathService, ITarStreamService tarStreamService)
        {
            this.containerPathService = containerPathService;
            this.tarStreamService = tarStreamService;
        }

        public void StreamInFile(Stream stream, string id, string destination)
        {
            string path = containerPathService.GetSubdirectory(id, destination);
            tarStreamService.WriteTarStreamToPath(stream, path);
        }
    }
}