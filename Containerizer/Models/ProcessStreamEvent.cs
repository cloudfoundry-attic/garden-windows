using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Containerizer.Models
{
    public class ProcessStreamEvent
    {
        [JsonProperty(PropertyName = "type")]
        public string MessageType { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string Message { get; set; }
    }
}