using System.Net.Http;
using System.Text.Json;

namespace MessageRouter
{
    public static class APIConfiguration
    {
        public static HttpClient HttpClient = new();
        public static string WebAPI => "https://localhost:7272/api";
        public static JsonSerializerOptions JsonOptions => new(JsonSerializerDefaults.Web);
    }
}
