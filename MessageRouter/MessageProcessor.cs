using System;
using MessageModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace MessageRouter
{
    public static class MessageProcessor
    {
        [FunctionName("MessageProcessor")]
        public static void Run(
            [RabbitMQTrigger(
                "UnprocessedMessage", 
                ConnectionStringSetting = "RabbitMQConnectionString")]string queueMessage,
            [CosmosDB(
                databaseName: "OrderDatabase",
                collectionName: "OrderContainer",
                ConnectionStringSetting = "CosmosDBConnectionString")]out dynamic document,
            ILogger log)
        {
            var key = Guid.NewGuid();
            document = new { 
                id = key,
                MessageDetails = queueMessage,
                MessageReceivedTime = DateTime.UtcNow.ToString(),
                TrackingId = key};
            log.LogInformation($"C# Queue trigger function processed: {queueMessage}");
        }
    }
}
