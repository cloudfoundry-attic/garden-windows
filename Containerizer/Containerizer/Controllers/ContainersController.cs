﻿#region

using Containerizer.Models;
using IronFrame;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Controllers
{
    public class CreateResponse
    {
        [JsonProperty("handle")]
        public string Handle { get; set; }
    }

    public class ContainersController : ApiController
    {
        private const int MaxRetrys = 5;
        private const int DelayInMilliseconds = 1000;
        private readonly uint ContainerActiveProcessLimit;
        private readonly IContainerService containerService;
        private readonly ILogger logger;

        internal const int CONTAINER_DEFAULT_CPU_WEIGHT = 5;
        private static readonly object containerDeletionLock = new Object();

        public ContainersController(IContainerService containerService, ILogger logger, IOptions options)
        {
            this.containerService = containerService;
            this.logger = logger;
            this.ContainerActiveProcessLimit = (uint)options.ActiveProcessLimit;
        }

        [Route("api/containers")]
        [HttpGet]
        public IReadOnlyList<string> Index(string q = null)
        {
            IEnumerable<IContainer> containers = containerService.GetContainers();
            if (q != null)
            {
                var desiredProps = JsonConvert.DeserializeObject<Dictionary<string, string>>(q);

                containers = containers.Where((x) =>
                {
                    try
                    {
                        var properties = x.GetProperties();
                        return desiredProps.All(p =>
                        {
                            string propValue;
                            var exists = properties.TryGetValue(p.Key, out propValue);
                            return exists && propValue == p.Value;
                        });
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return true;
                    }
                });

            }
            return containers.Select(x => x.Handle).ToList();
        }

        [Route("api/containers")]
        [HttpPost]
        public CreateResponse Create(ContainerSpecApiModel spec)
        {
            if (spec.Env == null)
            {
                spec.Env = new List<string>();
            }

            var containerSpec = new ContainerSpec
            {
                Handle = spec.Handle,
                Properties = spec.Properties,
                Environment = ContainerService.EnvsFromList(spec.Env),
                BindMounts = spec.BindMounts.Select(b => b.ToIronFrame()).ToArray()
            };

            try
            {
                var container = containerService.CreateContainer(containerSpec);
                container.SetActiveProcessLimit(ContainerActiveProcessLimit);
                container.SetPriorityClass(ProcessPriorityClass.BelowNormal);
                container.LimitCpu(CONTAINER_DEFAULT_CPU_WEIGHT);
                if (spec.Limits.MemoryLimits.LimitInBytes != null)
                    container.LimitMemory(spec.Limits.MemoryLimits.LimitInBytes.Value);
                if (spec.Limits.DiskLimits.ByteHard != null)
                    container.LimitDisk(spec.Limits.DiskLimits.ByteHard.Value);
                if (spec.GraceTime.HasValue)
                    container.SetProperty("GraceTime", spec.GraceTime.Value.ToString());

                return new CreateResponse
                {
                    Handle = container.Handle
                };
            }
            catch (PrincipalExistsException)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict,
                    string.Format("handle already exists: {0}", spec.Handle)));
            }
        }

        [Route("api/containers/{handle}/stop")]
        [HttpPost]
        public IHttpActionResult Stop(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container != null)
            {
                container.Stop(true);
                return Ok();
            }

            return NotFound();
        }

        [Route("api/containers/{handle}")]
        [HttpDelete]
        public IHttpActionResult Destroy(string handle)
        {
            var container = containerService.GetContainerByHandleIncludingDestroyed(handle);
            if (container != null)
            {
                try
                {
                    containerService.DestroyContainer(handle);
                }
                catch (Exception e)
                {
                    logger.Error("Cannot delete container", new Dictionary<string, object>
                    {
                        {"handle", handle},
                        {"exception", e},
                    });
                    throw;
                }
                return Ok();
            }

            return NotFound();
        }
    }
}
