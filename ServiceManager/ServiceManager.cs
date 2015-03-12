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
    public partial class ServiceManager : System.Configuration.Install.Installer
    {
        private readonly LocalInstaller[] installers = new LocalInstaller[]{
            new Consul(),
            new Containerizer(),
            new Rep(),
            new Executor(),
            new GardenWindows()
        };

        public ServiceManager()
        {
            InitializeComponent();
        }


        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            foreach (var installer in installers)
	        {
               installer.Install(stateSaver);
	        }
        }


        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            foreach (LocalInstaller installer in installers)
	        {
               installer.Uninstall(savedState);
	        }
        }

    }
}
