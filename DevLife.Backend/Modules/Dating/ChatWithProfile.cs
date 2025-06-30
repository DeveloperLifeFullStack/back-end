using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DevLife.Backend.Modules.Dating
{
    public static class ChatWithProfile
    {
        public static RouteGroupBuilder MapAiChatEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/chat", ChatWithAi);
            return group;
        }

        public record ChatRequest(string Message);

        private static async Task<IResult> ChatWithAi(ChatRequest request, IConfiguration config)
        {
            var apiKey = config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return Results.Problem("OpenAI API key not configured.");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = request.Message }
            },
                temperature = 0.7,
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode)
                return Results.Problem("Failed to get response from OpenAI.");

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var message = doc.RootElement
                             .GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString();

            return Results.Ok(new { Reply = message });
        }
    }
}