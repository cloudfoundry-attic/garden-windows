using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Http;
using Containerizer.Factories;
using Newtonsoft.Json;

namespace Containerizer.Controllers
{
    public class Capacity
    {
        [JsonProperty("disk_in_bytes")] public ulong DiskInBytes;
        [JsonProperty("max_containers")] public ulong MaxContainers;
        [JsonProperty("memory_in_bytes")] public ulong MemoryInBytes;
    }

    public class CapacityController : ApiController
    {
        private const ulong MaxContainers = 256;

        [Route("api/capacity")]
        [HttpGet]
        public Capacity Index()
        {
            var memStatus = new MEMORYSTATUSEX();
            if (!GlobalMemoryStatusEx(memStatus))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var drive = new DriveInfo(ContainerServiceFactory.GetContainerRoot());

            return new Capacity
            {
                MemoryInBytes = memStatus.ullTotalPhys,
                DiskInBytes = (ulong) drive.TotalSize,
                MaxContainers = MaxContainers,
            };
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint) Marshal.SizeOf(typeof (MEMORYSTATUSEX));
            }
        }
    }
}