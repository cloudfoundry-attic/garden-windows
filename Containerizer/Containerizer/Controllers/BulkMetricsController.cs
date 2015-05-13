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

namespace Containerizer.Controllers
{
    public struct BulkMetricsResponse
    {
        public ContainerMetricsApiModel Metrics;
    }

    public class BulkMetricsController : ApiController
    {
        private readonly IContainerInfoService containerInfoService;

        public BulkMetricsController(IContainerInfoService containerInfoService)
        {
            this.containerInfoService = containerInfoService;
        }

        [Route("api/bulkcontainermetrics")]
        [HttpPost]
        public Dictionary<string, BulkMetricsResponse> BulkMetrics(string[] handles)
        {
            var response = new Dictionary<string, BulkMetricsResponse>();
            foreach (var handle in handles) {
                var metrics = containerInfoService.GetMetricsByHandle(handle);
                if (metrics != null)
                {
                    response[handle] = new BulkMetricsResponse
                    {
                        Metrics = metrics,
                    };
                }
            }
            return response;
        }
    }
}