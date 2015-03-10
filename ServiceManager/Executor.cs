using System.ComponentModel;

namespace ServiceManager
{
    [RunInstaller(true)]
    public partial class Executor : LocalInstaller
    {
        public Executor() : base("executor",
            "-listenAddr=0.0.0.0:1700 -skipCertVerify=false -debugAddr=0.0.0.0:17004 -gardenNetwork=tcp -gardenAddr=localhost:9241 -memoryMB=auto -diskMB=auto -containerMaxCpuShares=1024 -tempDir=/c/tmp/executor/tmp -cachePath=/c/tmp/executor/cache -maxCacheSizeInBytes=10000000000 -allowPrivileged=false -exportNetworkEnvVars=false -drainTimeout=900s -healthyMonitoringInterval=30s -unhealthyMonitoringInterval=0.5s")
        {
        }
    }
}
