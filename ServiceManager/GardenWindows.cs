using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceManager
{
    [RunInstaller(true)]
    public partial class GardenWindows : LocalInstaller
    {
        public GardenWindows() : base("garden-windows", "--listenNetwork=tcp -listenAddr=0.0.0.0:9241 -containerGraceTime=1h -containerizerURL=http://10.10.5.4:80")
        {
        }
    }
}
