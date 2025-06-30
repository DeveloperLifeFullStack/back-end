using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace DevLife.Backend.Services;

public class AiRoaster
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public AiRoaster(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<(string review, int rating)> EvaluateCodeAsync(string code)
    {
        var prompt = GeneratePrompt(code);

        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You're a sarcastic but helpful AI code reviewer." },
                new { role = "user", content = prompt }
            },
            temperature = 0.9
        };

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

        var response = await _http.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            return ($"Oops! Something went wrong: {response.StatusCode}", 0);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var contentStr = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var resultJson = JsonSerializer.Deserialize<JsonElement>(contentStr!);
        var review = resultJson.GetProperty("review").GetString();
        var rating = resultJson.GetProperty("rating").GetInt32();

        return (review!, rating);
    }

    public async Task<object> GenerateTaskAsync(string language, string difficulty)
    {
        var prompt = $$"""
        You are a programming challenge generator. Generate a {{difficulty}}-level task in {{language}}.
        Respond in JSON format like:
        {
            "title": "Task title here",
            "description": "Detailed task description here.",
            "language": "{{language}}",
            "difficulty": "{{difficulty}}"
        }
        """;

        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful programming challenge generator." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

        var response = await _http.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            return new { error = $"Task generation failed: {response.StatusCode}" };

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var contentStr = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return JsonSerializer.Deserialize<object>(contentStr!);
    }

    private string GeneratePrompt(string code) =>
        $$"""
    Please review the following code and roast it if it's bad, or praise it in a funny way if it's good.

    Then, rate the code from 1 to 10 based on quality, style, and logic.

    Respond in the following JSON format:
    {
        "review": "Your funny/sarcastic comment here.",
        "rating": 1-10
    }

    Code:
    ```
    {{code}}
    ```
    """;
}