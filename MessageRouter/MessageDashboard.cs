using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using MessageModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageRouter
{
    public class MessageDashboard
    {
        private readonly IConfiguration _configuration;

        public MessageDashboard(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("index")]
        public static IActionResult Index([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req, ExecutionContext context)
        {
            var path = Path.Combine(context.FunctionAppDirectory, "content", "index.html");
            return new ContentResult
            {
                Content = File.ReadAllText(path),
                ContentType = "text/html",
            };
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = "MessageHub")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName("broadcast")]
        public async Task Broadcast(
            [CosmosDBTrigger("OrderDatabase", "OrderContainer", ConnectionStringSetting = "CosmosDBConnectionString", LeaseCollectionName = "leases", CreateLeaseCollectionIfNotExists =true)]
            IEnumerable<object> updatedResults,
            [SignalR(HubName = "MessageHub")] IAsyncCollector<SignalRMessage> signalRMessages, 
            ILogger logger)
        {
            string baseAPI = _configuration["WebAPI"] ?? APIConfiguration.WebAPI;

            var updatedResult = updatedResults.FirstOrDefault();
            var updatedOrder = JsonSerializer.Deserialize<Order>(updatedResult.ToString(), APIConfiguration.JsonOptions);
            
            if (string.IsNullOrEmpty(updatedOrder.ReviewAssignedTo))
            {
                await MessageAssignment.AssignOrder(updatedOrder, baseAPI);
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, @$"{baseAPI}/Orders");
            request.Headers.UserAgent.ParseAdd("Serverless");
            var response = await APIConfiguration.HttpClient.SendAsync(request);
            var orderResult = JsonSerializer.Deserialize<Order[]>(await response.Content.ReadAsStringAsync(), APIConfiguration.JsonOptions);
            IEnumerable<Order> orderResultEnumerbale = orderResult.Where(order => !string.IsNullOrEmpty(order.ReviewAssignedTo));

            string generatedMessage = GenerateMessage(orderResultEnumerbale, updatedOrder);
            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newMessage",
                    Arguments = new[]
                    {
                        HttpUtility.HtmlEncode(generatedMessage).Replace(Environment.NewLine, "<br/>")
                    }
                });
        }

        private static string GenerateMessage(IEnumerable<Order> orderResults, Order updatedOrder)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("All Work items:");
            foreach (var order in orderResults)
            {
                sb.AppendLine($"Message with tracking id: {order.TrackingId} received at {order.MessageReceivedTime} is assigned to: {order.ReviewAssignedTo}: {order.MessageDetails}");
            }

            var mostRecentOrderUpdate = orderResults.FirstOrDefault(order => order.TrackingId == updatedOrder.TrackingId);
            if (mostRecentOrderUpdate != null)
            {
                sb.AppendLine();
                sb.AppendLine("Most recent update:");
                sb.AppendLine($"Message with tracking id: {mostRecentOrderUpdate.TrackingId} received at {mostRecentOrderUpdate.MessageReceivedTime} is assigned to: {mostRecentOrderUpdate.ReviewAssignedTo} : {mostRecentOrderUpdate.MessageDetails}");
            }

            return sb.ToString();
        }
    }
}