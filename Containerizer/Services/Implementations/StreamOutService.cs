using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Containerizer.Services.Interfaces;

namespace Containerizer.Services.Implementations
{
    public class StreamOutService : IStreamOutService
    {
        private readonly IContainerPathService containerPathService;
        private ITarStreamService tarStreamService;

        public StreamOutService(IContainerPathService containerPathService, ITarStreamService tarStreamService)
        {
            this.containerPathService = containerPathService;
            this.tarStreamService = tarStreamService;
        }

        public System.IO.Stream StreamFile(string id, string source)
        {
            var rootDir = containerPathService.GetContainerRoot(id);
            var path = Path.Combine(rootDir, source);
            var stream = tarStreamService.WriteTarToStream(path);
            return stream;
        }
    }
}