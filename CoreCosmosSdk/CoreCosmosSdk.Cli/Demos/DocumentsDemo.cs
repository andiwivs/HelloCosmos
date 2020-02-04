using CoreCosmosSdk.Cli.Demos.Model;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace CoreCosmosSdk.Cli.Demos
{
    public static class DocumentsDemo
    {
        private static readonly string TemporaryDatabaseId = "MyTempDb";
        private static readonly string TemporaryContainerId = "MyContainer1";

        public static async Task Run(CosmosClient client)
        {
            await EnsureTemporaryDatabaseExists(client);
            await EnsureTemporaryContainerExists(client);

            await CreateDocuments(client);

            //await QueryDocuments(client);

            //await QueryWithStatefulPaging(client);
            //await QueryWithStatelessPaging(client);

            //await QueryWithStatefulPagingStreamed(client);
            //await QueryWithStatelessPagingStreamed(client);

            //QueryWithLinq();

            //await ReplaceDocuments();

            //await DeleteDocuments();

            await DeleteTemporaryDatabase(client);
        }

        private static async Task CreateDocuments(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Create Documents <<<");
            Console.WriteLine();

            var container = client.GetContainer(TemporaryDatabaseId, TemporaryContainerId);

            #region create document using dynamic type

            dynamic dynamicDocument = new
            {
                id = Guid.NewGuid(),
                pk = "OX117GA", // note: partition key path /pk was defined in the container
                name = "New customer 1",
                address = new
                {
                    addressType = "Main Office",
                    addressLine1 = "123 Main Street",
                    location = new
                    {
                        city = "Oxford",
                        county = "Oxfordshire"
                    },
                    postcode = "OX117GA",
                    country = "United Kingdom"
                }
            };

            await container.CreateItemAsync(dynamicDocument, new PartitionKey(dynamicDocument.pk));
            Console.WriteLine($"Created new document {dynamicDocument.id} from dynamic object");

            #endregion

            #region create document using raw json

            var jsonDocumentRaw = $@"
                {{
                    ""id"": ""{Guid.NewGuid()}"",
                    ""pk"": ""RG179XA"",
                    ""name"": ""New customer 2""
                }}";

            var jsonDocumentObject = JsonConvert.DeserializeObject<JObject>(jsonDocumentRaw);
            
            await container.CreateItemAsync(jsonDocumentObject, new PartitionKey(jsonDocumentObject["pk"].Value<string>()));
            Console.WriteLine($"Created new document {jsonDocumentObject["id"].Value<string>()} from JSON string");

            #endregion

            #region create using POCOs

            var pocoDocument = new Customer
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKey = "SN83PA",
                Name = "New customer 3",
                Address = new Address
                {
                    AddressType = "Residential",
                    AddressLine1 = "99 Some other road",
                    Location = new Location
                    {
                        City = "Swindon",
                        County = "Wiltshire"
                    },
                    Postcode = "SN83PA",
                    Country = "United Kingdom"
                }
            };

            await container.CreateItemAsync(pocoDocument, new PartitionKey(pocoDocument.PartitionKey));
            Console.WriteLine($"Created new document {pocoDocument.Id} from typed POCO");

            #endregion
        }

        #region setup and teardown helpers

        private static async Task EnsureTemporaryDatabaseExists(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($"Creating database {TemporaryDatabaseId} (if not exists)");

            var result = await client.CreateDatabaseIfNotExistsAsync(TemporaryDatabaseId);
            var database = result.Resource;

            Console.WriteLine($"Database Id: {database.Id}; Modified: {database.LastModified}");
        }

        private static async Task EnsureTemporaryContainerExists(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($"Creating container {TemporaryContainerId} (if not exists)");

            var partitionKey = "/pk";
            var throughput = 400;

            var containerProperties = new ContainerProperties(TemporaryContainerId, partitionKey);
            var database = client.GetDatabase(TemporaryDatabaseId);

            var result = await database.CreateContainerIfNotExistsAsync(containerProperties, throughput);
            var container = result.Resource;

            Console.WriteLine($"Container Id: {container.Id}; Modified: {container.LastModified}");
        }

        private static async Task DeleteTemporaryDatabase(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($"Deleting database {TemporaryDatabaseId}...");

            await client.GetDatabase(TemporaryDatabaseId).DeleteAsync();

            Console.WriteLine($"Database {TemporaryDatabaseId} has been deleted");
        }

        #endregion
    }
}
