using System;
using System.Collections.Generic;
using Containerizer.Controllers;
using Newtonsoft.Json;

namespace Containerizer.Models
{
    public class ContainerSpecApiModel
    {
        public ContainerSpecApiModel ()
        {
            Limits = new Limits();
            BindMounts = new BindMount[]{};
        }

        public string Handle
        {
            get;
            set;
        }

       [JsonProperty("grace_time")]
        public long? GraceTime
        {
            get;
            set;
        }

        public Dictionary<string, string> Properties
        {
            get;
            set;
        }

        public List<String> Env { get; set; }

        public Limits Limits { get; set; }

       [JsonProperty("bind_mounts")]
        public BindMount[] BindMounts { get; set; }
    }

    public class BindMount
    {
       [JsonProperty("src_path")]
        public string SourcePath { get; set; }

       [JsonProperty("dst_path")]
        public string DestinationPath { get; set; }

        public int Mode { get; set; }

        public IronFrame.BindMount ToIronFrame()
        {
            return new IronFrame.BindMount()
            {
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath,
            };
        }

    }

    public class Limits
    {
        public Limits()
        {
            CpuLimits = new CpuLimits();
            MemoryLimits = new MemoryLimits();
            DiskLimits = new DiskLimits();
        }
        [JsonProperty("cpu_limits")]
        public CpuLimits CpuLimits { get; set; }
        [JsonProperty("memory_limits")]
        public MemoryLimits MemoryLimits { get; set; }
        [JsonProperty("disk_limits")]
        public DiskLimits DiskLimits { get; set; }
    }
}