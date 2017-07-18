using CommandLine;
using CommandLine.Text;
using Containerizer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Containerizer.Services.Implementations
{
    public class Options : IOptions
    {
        private string containerDirectory;
        private int activeProcessLimit;

        [Option(
            Required = true,
            HelpText = "IP address to listen on.")]
        public string MachineIp { get; set; }

        [Option(
            Required = true,
            HelpText = "TCP port number to listen on.")]
        public int Port { get; set; }

        [Option(
            Required = false,
            HelpText = "Directory path where all the container data is stored.")]
        public string ContainerDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(this.containerDirectory))
                {
                    return  Containerizer.Factories.ContainerServiceFactory.GetContainerDefaultRoot();
                }
                return this.containerDirectory;
            }
            set
            {
                this.containerDirectory = value;
            }
        }

        [Option("activeProcessLimit",
            Required = false,
            HelpText = "Max number of processes in container")]
        public int ActiveProcessLimit
        {
            get
            {
                if (this.activeProcessLimit < 10)
                {
                    return 10;
                }
                return this.activeProcessLimit;
            }
            set
            {
                this.activeProcessLimit = value;
            }
        }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}