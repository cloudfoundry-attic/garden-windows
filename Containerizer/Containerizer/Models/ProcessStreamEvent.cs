#region

using Newtonsoft.Json;

#endregion

namespace Containerizer.Models
{
    public class ProcessStreamEvent
    {
        [JsonProperty(PropertyName = "type")]
        public string MessageType { get; set; }

        [JsonProperty(PropertyName = "pspec", NullValueHandling = NullValueHandling.Ignore)]
        public ApiProcessSpec ApiProcessSpec { get; set; }

        [JsonProperty(PropertyName = "data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data { get; set; }
    }
}