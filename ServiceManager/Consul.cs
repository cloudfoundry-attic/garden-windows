using System.ComponentModel;

namespace ServiceManager
{
    [RunInstaller(true)]
    public partial class Consul : LocalInstaller
    {
        public Consul()
        {
            serviceName = "consul";
            exeArguments = @"agent -pid-file=c:\consul\consul-agent.pid -config-dir=c:\consul\";
        }
    }
}
