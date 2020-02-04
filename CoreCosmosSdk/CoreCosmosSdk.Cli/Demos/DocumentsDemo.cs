﻿using CoreCosmosSdk.Cli.Demos.Model;
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
        private static readonly string CustomerContainerId = "Customers";
        private static readonly string ProductContainerId = "Products";

        public static async Task Run(CosmosClient client)
        {
            await EnsureTemporaryDatabaseExists(client);
            await EnsureContainerExists(client, CustomerContainerId);
            await EnsureContainerExists(client, ProductContainerId);

            await PopulateProducts(client);

            await CreateDocuments(client);

            await QueryDocuments(client);

            await QueryWithStatefulPaging(client);
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

            var container = client.GetContainer(TemporaryDatabaseId, CustomerContainerId);

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

        private static async Task QueryDocuments(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Query Documents (SQL) <<<");
            Console.WriteLine();

            var container = client.GetContainer(TemporaryDatabaseId, CustomerContainerId);

            Console.WriteLine("Querying for new customer documents (SQL)");
            Console.WriteLine();

            int count;
            var query = "SELECT * FROM c WHERE STARTSWITH(c.name, 'New customer') = true"; // note: should avoid running cross-partition queries

            #region Query for dynamic objects

            var dynamicIterator = container.GetItemQueryIterator<dynamic>(query);

            var dynamicDocuments = await dynamicIterator.ReadNextAsync();

            count = 0;

            foreach (var document in dynamicDocuments)
            {
                count++;
                Console.WriteLine($"  #{count} Id: {document.id}; Name: {document.name};");

                // dynamic objects can also be converted to POCOs
                var customer = JsonConvert.DeserializeObject<Customer>(document.ToString());
                Console.WriteLine($"    City: {customer.Address?.Location?.City ?? "{{ Unknown }}"}");
            }

            Console.WriteLine();
            Console.WriteLine($"Retrieved {count} new documents as dynamic");
            Console.WriteLine();

            #endregion

            #region Query for defined types (POCOs)

            var typedIterator = container.GetItemQueryIterator<Customer>(query);

            var typedDocuments = await typedIterator.ReadNextAsync();

            count = 0;

            foreach (var customer in typedDocuments)
            {
                count++;
                Console.WriteLine($"  #{count} Id: {customer.Id}; Name: {customer.Name};");
                Console.WriteLine($"    City: {customer.Address?.Location?.City ?? "{{ Unknown }}"}");
            }

            Console.WriteLine();
            Console.WriteLine($"Retrieved {count} new documents as Customer (POCO)");
            Console.WriteLine();

            #endregion
        }

        private static async Task QueryWithStatefulPaging(CosmosClient client)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Query Documents (paged results, stateful) <<<");
            Console.WriteLine();

            var container = client.GetContainer(TemporaryDatabaseId, ProductContainerId);
            var query = "SELECT * FROM c";
            int itemCount;

            #region first page of large result set

            Console.WriteLine("Querying for all product documents (first page)");

            var iterator = container.GetItemQueryIterator<Product>(query, requestOptions: new QueryRequestOptions { MaxItemCount = 50 }); // default should be 100 is being ignored?

            var documents = await iterator.ReadNextAsync();

            itemCount = 0;

            foreach (var product in documents)
            {
                itemCount++;
                Console.WriteLine($"#{itemCount} Id: {product.Id}; Name: {product.Name};");
            }

            Console.WriteLine($"Retrieved {itemCount} documents in first page");
            Console.WriteLine();

            #endregion
            
            #region all pages of large result set (using iterator HasMoreResults)

            Console.WriteLine("Querying for all product documents (full reset set, stateful)");

            var pagedIterator = container.GetItemQueryIterator<Product>(query, requestOptions: new QueryRequestOptions { MaxItemCount = 50 }); // default should be 100 is being ignored?

            itemCount = 0;
            var pageCount = 0;

            while (pagedIterator.HasMoreResults)
            {
                pageCount++;

                Console.WriteLine($"Page index incremented to {pageCount}");

                var pagedDocuments = await pagedIterator.ReadNextAsync();

                foreach (var product in pagedDocuments)
                {
                    itemCount++;
                    Console.WriteLine($"#{itemCount} Id: {product.Id}; Name: {product.Name};");
                }
            }

            Console.WriteLine($"Retrieved {itemCount} documents across full result set");
            Console.WriteLine();

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

        private static async Task EnsureContainerExists(CosmosClient client, string containerId, int throughput = 400, string partitionKey = "/pk")
        {
            Console.WriteLine();
            Console.WriteLine($"Creating container {containerId} (if not exists)");
            
            var containerProperties = new ContainerProperties(containerId, partitionKey);
            var database = client.GetDatabase(TemporaryDatabaseId);

            var result = await database.CreateContainerIfNotExistsAsync(containerProperties, throughput);
            var container = result.Resource;

            Console.WriteLine($"Container Id: {container.Id}; Modified: {container.LastModified}");
        }

        private static async Task PopulateProducts(CosmosClient client)
        {
            var totalNew = 150;
            var container = client.GetContainer(TemporaryDatabaseId, ProductContainerId);

            for (var idx = 1; idx <= totalNew; idx++)
            {
                var productDocument = new Product
                {
                    Id = Guid.NewGuid().ToString(),
                    PartitionKey = "SN83PA",
                    Name = $"Product {idx}"
                };

                await container.CreateItemAsync(productDocument, new PartitionKey(productDocument.PartitionKey));
            }

            Console.WriteLine($"Added {totalNew} product documents");
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
