using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CoreCosmosSdk.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var endpoint = config["cosmosDb:endpoint"];
            var key = config["cosmosDb:key"];

            using (var client = new CosmosClient(endpoint, key))
            {
                //QueryForDocuments(client).Wait();

                //DatabasesDemo.Run(client).Wait();
                ContainersDemo.Run(client).Wait();
            }
        }

        private static async Task QueryForDocuments(CosmosClient client)
        {
            var query = "SELECT * FROM c WHERE ARRAY_LENGTH(c.children) > 1";

            var container = client.GetContainer("Families", "Families");

            // to perform a query, we first need to define an iterator
            // use "dynamic" when there is no document type defined (probably always should be?)
            var iterator = container.GetItemQueryIterator<dynamic>(query);

            // we then fetch a page of results
            var page = await iterator.ReadNextAsync();

            // now we can iterate through the paged result
            foreach (var doc in page)
            {
                Console.WriteLine($"Family {doc.id} has {doc.children.Count} children");
            }
        }
    }
}
