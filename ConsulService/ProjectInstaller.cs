using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

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

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);
            var consulIps = Context.Parameters["CONSUL_IPS"].Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            var consulConfig = new
            {
                datacenter = "dc1",
                data_dir = "/tmp",
                node_name = Context.Parameters["MACHINE_NAME"],
                server = false,
                ports = new { dns = 53 },
                bind_addr = Context.Parameters["EXTERNAL_IP"],
                rejoin_after_leave = true,
                disable_remote_exec = true,
                disable_update_check = true,
                protocol = 2,
                start_join = consulIps,
                retry_join = consulIps
            };

            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonString = javaScriptSerializer.Serialize(consulConfig);
            var configDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Context.Parameters["assemblypath"], "..", "consul"));
            System.IO.Directory.CreateDirectory(configDir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(configDir, "config.json"), jsonString);
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
