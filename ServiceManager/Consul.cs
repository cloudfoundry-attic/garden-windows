using System.ComponentModel;

namespace ServiceManager
{
    [RunInstaller(false)]
    public partial class Consul : LocalInstaller
    {
        public Consul() : base("consul", @"agent -pid-file=c:\consul\consul-agent.pid -config-dir=c:\consul\")
        {
        }
    }
}
