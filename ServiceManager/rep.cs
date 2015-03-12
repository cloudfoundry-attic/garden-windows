using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceManager
{
    [RunInstaller(false)]
    public partial class Rep :  LocalInstaller
    {
        public Rep() : base("rep", "-etcdCluster=http://10.10.5.10:4001 -debugAddr=0.0.0.0:17008 -stack=windows2 -executorURL=http://127.0.0.1:1700 -listenAddr=0.0.0.0:1800 -cellID=cell_windows_z1-1 -zone=z1 -pollingInterval=30s -evacuationTimeout=180s")
        {
        }
    }
}
