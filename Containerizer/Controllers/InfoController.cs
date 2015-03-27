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
    public class InfoController : ApiController
    {
        private readonly IContainerInfoService containerInfoService;


        public InfoController(IContainerInfoService containerInfoService)
        {
            this.containerInfoService = containerInfoService;
        }

        [Route("api/containers/{handle}/info")]
        public IHttpActionResult GetInfo(string handle)
        {
            var info = containerInfoService.GetInfoByHandle(handle);
            if (info == null) {
                return NotFound();
            }
            return Json(info);
        }
    }
}
