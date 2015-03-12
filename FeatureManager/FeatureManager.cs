using ServiceManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureManager
{
    [RunInstaller(true)]
    public partial class FeatureManager : System.Configuration.Install.Installer
    {
        public FeatureManager()
        {
            InitializeComponent();
        }


        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            var dismPath = Environment.ExpandEnvironmentVariables(@"%WINDIR%\SysNative\dism.exe");

            // enable windows features that the containerizer depends on in order to run the container host
            var commands = new string[][] {
		        new string[]{dismPath, "/online /Enable-Feature /FeatureName:IIS-WebServer /All /NoRestart"},
				new string[]{dismPath, "/online /Enable-Feature /FeatureName:IIS-WebSockets /All /NoRestart"},
				new string[]{dismPath, "/online /Enable-Feature /FeatureName:Application-Server-WebServer-Support /FeatureName:AS-NET-Framework /All /NoRestart"},
				new string[]{dismPath, "/online /Enable-Feature /FeatureName:IIS-HostableWebCore /All /NoRestart"},
		    };

            LocalInstaller.RunCommands(LocalInstaller.CodeBaseDirectory(), commands);
        }
    }
}

