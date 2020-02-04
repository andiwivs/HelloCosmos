using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace CoreCosmosSdk.Cli.Demos
{
    public static class ContainersDemo
    {
        private static readonly string TemporaryDatabaseId = "MyTempDb";
        private static readonly string TemporaryContainerId1 = "MyContainer1";
        private static readonly string TemporaryContainerId2 = "MyContainer2";

        public static async Task Run(CosmosClient client)
        {
            await EnsureTemporaryDatabaseExists(client);

            await ViewContainers(client);

            await CreateContainer(client, TemporaryContainerId1);
            await CreateContainer(client, TemporaryContainerId2, 1000, "/state");
            await ViewContainers(client);

            await DeleteContainer(client, TemporaryContainerId1);
            await DeleteContainer(client, TemporaryContainerId2);
            await ViewContainers(client);

            await DeleteTemporaryDatabase(client);
        }

        private static async Task ViewContainers(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> View Containers in {TemporaryDatabaseId} <<<");

            var database = client.GetDatabase(TemporaryDatabaseId);
            var iterator = database.GetContainerQueryIterator<ContainerProperties>();

            var containers = await iterator.ReadNextAsync();

            var count = 0;

            foreach (var container in containers)
            {
                count++;
                Console.WriteLine();
                Console.WriteLine($"Container #{count}");
                await ViewContainer(client, container);
            }

            Console.WriteLine();
            Console.WriteLine($"Total containers in {TemporaryDatabaseId} database: {count}");
        }

        private static async Task ViewContainer(CosmosClient client, ContainerProperties containerProperties)
        {
            // to get the container throughput, we need to perform an extra query...
            var container = client.GetContainer(TemporaryDatabaseId, containerProperties.Id);
            var throughput = await container.ReadThroughputAsync();

            Console.WriteLine($"    Container Id: {containerProperties.Id};");
            Console.WriteLine($"        Modified: {containerProperties.LastModified}");
            Console.WriteLine($"   Partition Key: {containerProperties.PartitionKeyPath}");
            Console.WriteLine($"      Throughput: {throughput}");
        }

        private static async Task CreateContainer(CosmosClient client, string containerId, int throughput = 400, string partitionKey = "/pk")
        {
            Console.WriteLine();
            Console.WriteLine($">>> Create Container {containerId} in {TemporaryDatabaseId} <<<");
            Console.WriteLine();
            Console.WriteLine($"        Throughput: {throughput} RU/sec");
            Console.WriteLine($"     Partition key: {partitionKey}");
            Console.WriteLine();

            var containerProperties = new ContainerProperties(containerId, partitionKey);
            var database = client.GetDatabase(TemporaryDatabaseId);

            await database.CreateContainerIfNotExistsAsync(containerProperties, throughput);

            Console.WriteLine($"Created new container {containerId}");
        }

        private static async Task DeleteContainer(CosmosClient client, string containerId)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Delete Container {containerId} in {TemporaryDatabaseId} <<<");

            var container = client.GetContainer(TemporaryDatabaseId, containerId);

            await container.DeleteContainerAsync();

            Console.WriteLine($"Deleted Container {containerId} from {TemporaryDatabaseId}");
        }

        #region setup and teardown helpers

        private static async Task EnsureTemporaryDatabaseExists(CosmosClient client)
        {
            Console.WriteLine($"Creating database {TemporaryDatabaseId} (if not exists)");

            var result = await client.CreateDatabaseIfNotExistsAsync(TemporaryDatabaseId);
            var database = result.Resource;

            Console.WriteLine($"Database Id: {database.Id}; Modified: {database.LastModified}");
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
