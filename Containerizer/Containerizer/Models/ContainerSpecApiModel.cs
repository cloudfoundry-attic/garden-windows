using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Containerizer.Models
{
    public class ContainerSpecApiModel
    {
        public string Handle
        {
            get;
            set;
        }
        public Dictionary<string, string> Properties
        {
            get;
            set;
        }

        public List<String> Env { get; set; } 
    }
}