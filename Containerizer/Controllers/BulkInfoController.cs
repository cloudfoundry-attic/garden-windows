using IronFoundry.Container;
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
    public struct BulkInfoResponse
    {
        public ContainerInfoApiModel Info;
        public string Err;
    }

    public class BulkInfoController : ApiController
    {
        private readonly IContainerInfoService containerInfoService;

        public BulkInfoController(IContainerInfoService containerInfoService)
        {
            this.containerInfoService = containerInfoService;
        }

        [Route("api/bulkcontainerinfo")]
        [HttpPost]
        public Dictionary<string, BulkInfoResponse> BulkInfo(string[] handles)
        {
            var response = new Dictionary<string, BulkInfoResponse>();
            foreach (var handle in handles) {
                var info = containerInfoService.GetInfoByHandle(handle);
                if (info != null)
                {
                    response[handle] = new BulkInfoResponse
                    {
                        Info = info,
                    };
                }
            }
            return response;
        }
    }
}