#region

using System;
using Containerizer.Services.Interfaces;
using IronFrame;
using System.IO;

#endregion

namespace Containerizer.Services.Implementations
{
    public class StreamInService : IStreamInService
    {
        private readonly ITarStreamService tarStreamService;
        private readonly IContainerService containerService;

        public StreamInService(IContainerService containerService, ITarStreamService tarStreamService)
        {
            this.tarStreamService = tarStreamService;
            this.containerService = containerService;
        }

        public void StreamInFile(Stream stream, string handle, string destination)
        {
            IContainer container = containerService.GetContainerByHandle(handle);
            var path = container.Directory.MapUserPath(destination);
            tarStreamService.WriteTarStreamToPath(stream, container, path);
        }
    }
}