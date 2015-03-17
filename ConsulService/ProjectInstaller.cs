using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ConsulService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            this.AfterInstall += new InstallEventHandler(ProjectInstaller_AfterInstall);
        }
        void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            using (ServiceController pc = new ServiceController(this.serviceInstaller.ServiceName))
            {
                pc.Start();
            }
        }
    }
}
