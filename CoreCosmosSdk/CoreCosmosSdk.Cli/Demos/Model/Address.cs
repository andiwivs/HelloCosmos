using Newtonsoft.Json;

namespace CoreCosmosSdk.Cli.Demos.Model
{
    public class Address
    {
        [JsonProperty(PropertyName = "addressType")]
        public string AddressType { get; set; }

        [JsonProperty(PropertyName = "addressLine1")]
        public string AddressLine1 { get; set; }

        [JsonProperty(PropertyName = "location")]
        public Location Location { get; set; }

        [JsonProperty(PropertyName = "postcode")]
        public string Postcode { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}
