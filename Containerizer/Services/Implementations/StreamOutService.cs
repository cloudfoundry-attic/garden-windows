#region

using System.IO;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;

#endregion

namespace Containerizer.Services.Implementations
{
    public class StreamOutService : IStreamOutService
    {
        private readonly IContainerService containerService;
        private readonly ITarStreamService tarStreamService;

        public StreamOutService(IContainerService containerService, ITarStreamService tarStreamService)
        {
            this.containerService = containerService;
            this.tarStreamService = tarStreamService;
        }

        public Stream StreamOutFile(string handle, string source)
        {
            IContainer container = containerService.GetContainerByHandle(handle);

            string path = container.Directory.MapUserPath(source);
            Stream stream = tarStreamService.WriteTarToStream(path);
            return stream;
        }
    }
}