using Newtonsoft.Json;

namespace CoreCosmosSdk.Cli.Demos.Model
{
    public class Product
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "pk")]
        public string PartitionKey { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "stockLevel")]
        public int StockLevel { get; set; }
    }
}
