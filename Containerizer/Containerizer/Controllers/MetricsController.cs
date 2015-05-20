using IronFrame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Models;
using Containerizer.Services.Interfaces;
using System.IO;
using Newtonsoft.Json;

namespace Containerizer.Controllers
{
    public class MetricsController : ApiController
    {
        private readonly IContainerService containerService;


        public MetricsController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/metrics")]
        public IHttpActionResult Get(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
            {
                return NotFound();
            }

            var info = container.GetInfo();
            var metrics = new Metrics
            {
                CPUStat = new Metrics.CCPUStat
                {
                    Usage = Convert.ToUInt64(info.CpuStat.TotalProcessorTime.TotalMilliseconds)
                },
                MemoryStat = new Metrics.CMemoryStat
                {
                    TotalBytesUsed = Convert.ToUInt64(info.MemoryStat.PrivateBytes)
                },
                DiskStat = new Metrics.CDiskStat()
                {
                    BytesUsed = 0 // FIXME
                }
            };

            return Json(metrics);
        }
    }


    public class Metrics
    {
        public CMemoryStat MemoryStat;
        public CDiskStat DiskStat;
        public CCPUStat CPUStat;

        public class CMemoryStat
        {
            [JsonProperty("TotalRss")]
            public ulong TotalBytesUsed;
        }
        public class CDiskStat
        {
            public ulong BytesUsed;
        }
        public class CCPUStat
        {
            public ulong Usage;
        }
    }
}
