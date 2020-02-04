using Newtonsoft.Json;

namespace CoreCosmosSdk.Cli.Demos.Model
{
    public class Location
    {
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "county")]
        public string County { get; set; }
    }
}
