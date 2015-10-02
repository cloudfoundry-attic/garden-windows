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
using ContainerInfo = Containerizer.Models.ContainerInfo;

namespace Containerizer.Controllers
{
    public struct ContainerInfoEntry
    {
        public ContainerInfo Info;
        public Error Err;
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
        public Dictionary<string, ContainerInfoEntry> BulkInfo(string[] handles)
        {
            var response = new Dictionary<string, ContainerInfoEntry>();
            foreach (var handle in handles)
            {
                ContainerInfoEntry infoEntry = new ContainerInfoEntry();
                try
                {
                    var info = containerInfoService.GetInfoByHandle(handle);
                    if (info == null)
                    {
                        throw new Exception("container " + handle + " does not exist");
                    }
                    infoEntry.Info = info;
                }
                catch (Exception e)
                {
                    infoEntry.Err = new Error()
                    {
                        Message = "cannot get info for container " + handle + ". Error: " + e.Message,
                    };
                }
                response[handle] = infoEntry;
            }
            return response;
        }
    }
}