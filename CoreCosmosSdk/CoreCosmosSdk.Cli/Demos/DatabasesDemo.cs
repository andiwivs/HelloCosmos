using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace CoreCosmosSdk.Cli.Demos
{
    public static class DatabasesDemo
    {
        private static readonly string TemporaryDatabaseId = "MyTempDb";

        public static async Task Run(CosmosClient client)
        {
            await ViewDatabases(client);

            await CreateDatabase(client);
            await ViewDatabases(client);

            await DeleteDatabase(client);
            await ViewDatabases(client);
        }

        private static async Task ViewDatabases(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>> View Databases <<<");

            var iterator = client.GetDatabaseQueryIterator<DatabaseProperties>();

            var databases = await iterator.ReadNextAsync();

            var count = 0;

            foreach (var database in databases)
            {
                Console.WriteLine($"Database Id: {database.Id}; Modified: {database.LastModified}");
                count++;
            }

            Console.WriteLine();
            Console.WriteLine($"Total databases: {count}");
        }

        private static async Task CreateDatabase(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Create Database {TemporaryDatabaseId} <<<");

            var result = await client.CreateDatabaseIfNotExistsAsync(TemporaryDatabaseId);
            var database = result.Resource;

            Console.WriteLine($"Database Id: {database.Id}; Modified: {database.LastModified}");
        }

        private static async Task DeleteDatabase(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Delete Database {TemporaryDatabaseId} <<<");

            await client.GetDatabase(TemporaryDatabaseId).DeleteAsync();
        }
    }
}
