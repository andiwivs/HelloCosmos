﻿using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

namespace CoreCosmosSdk.Cli
{
    public static class DatabasesDemo
    {
        private static readonly string TemporaryDatabaseName = "MyTempDb";

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
            Console.WriteLine($">>> Create Database {TemporaryDatabaseName} <<<");

            var result = await client.CreateDatabaseIfNotExistsAsync(TemporaryDatabaseName);
            var database = result.Resource;

            Console.WriteLine($"Database Id: {database.Id}; Modified: {database.LastModified}");
        }

        private static async Task DeleteDatabase(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Delete Database {TemporaryDatabaseName} <<<");

            await client.GetDatabase(TemporaryDatabaseName).DeleteAsync();
        }
    }
}
