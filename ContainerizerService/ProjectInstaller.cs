using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ContainerizerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            this.AfterInstall += new InstallEventHandler(ProjectInstaller_AfterInstall);
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.User;
            this.serviceProcessInstaller.Username = Context.Parameters["CONTAINERIZER_USERNAME"];
            this.serviceProcessInstaller.Password = Context.Parameters["CONTAINERIZER_PASSWORD"];

            base.OnBeforeInstall(savedState);
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);


            /*
            var externalIP = this.Context.Parameters["EXTERNAL_IP"];
            if (externalIP == null)
            {
                throw new Exception("Must supply property EXTERNAL_IP");
            }
            */

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
