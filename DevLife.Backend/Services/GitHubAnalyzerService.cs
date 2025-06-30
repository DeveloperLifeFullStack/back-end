using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace DevLife.Backend.Services;

public class GitHubAnalyzerService
{
    private readonly HttpClient _http; // GitHub client
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubAnalyzerService(HttpClient http, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _http = http;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<string>> GetCommitMessagesAsync(string owner, string repo, string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("DevLifeAnalyzer");

        var url = $"https://api.github.com/repos/{owner}/{repo}/commits";
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new List<string>();

        var json = await response.Content.ReadAsStringAsync();
        var commits = JsonDocument.Parse(json).RootElement.EnumerateArray();

        return commits
            .Select(c => c.GetProperty("commit").GetProperty("message").GetString() ?? "")
            .ToList();
    }

    public async Task<object> AnalyzePersonalityAsync(List<string> commits)
    {
        var commitMessages = string.Join("\n", commits.Take(15).Select(c => $"- {c}"));

        var prompt = $$"""
        You are an expert developer personality profiler.

        Analyze the following Git commit messages and return the result as raw JSON only with this structure, without any explanation or markdown:
        {
          "type": "string - the developer personality type",
          "strengths": ["list of strengths"],
          "weaknesses": ["list of weaknesses"],
          "match": "string - similar famous developer",
          "image_url": "string - placeholder, will be replaced"
        }

        Commit messages:
        {{commitMessages}}
        """;

        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful developer personality analyzer." },
                new { role = "user", content = prompt }
            },
            temperature = 0.8
        };

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var openAiClient = _httpClientFactory.CreateClient();
        openAiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);
        openAiClient.DefaultRequestHeaders.UserAgent.ParseAdd("DevLifeAnalyzer");

        var response = await openAiClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            return new { error = $"❌ Personality analysis failed: {response.StatusCode}" };

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var contentStr = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        try
        {
            var match = Regex.Match(contentStr!, @"\{[\s\S]*\}");
            if (!match.Success)
                return new { error = "❌ Could not extract valid JSON from OpenAI response." };

            var resultJson = JsonSerializer.Deserialize<JsonElement>(match.Value);

            var type = resultJson.GetProperty("type").GetString();
            var strengths = resultJson.GetProperty("strengths").EnumerateArray().Select(x => x.GetString()).ToList();
            var weaknesses = resultJson.GetProperty("weaknesses").EnumerateArray().Select(x => x.GetString()).ToList();
            var matchName = resultJson.GetProperty("match").GetString();

            // Generate image using DALL·E based on personality type
            var imageUrl = await GenerateImageFromPersonalityAsync(type!);

            return new
            {
                type,
                strengths,
                weaknesses,
                match = matchName,
                image_url = imageUrl ?? "https://cdn.devlife.app/cards/default.png"
            };
        }
        catch (Exception ex)
        {
            return new { error = $"❌ Failed to parse OpenAI response: {ex.Message}" };
        }
    }

    private async Task<string?> GenerateImageFromPersonalityAsync(string personalityType)
    {
        var prompt = $"An illustrated trading card for a software developer personality called '{personalityType}', cartoon-style, futuristic design, high-quality, creative.";

        var request = new
        {
            model = "dall-e-3",
            prompt = prompt,
            n = 1,
            size = "1024x1024"
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DevLifeImageGenerator");

        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/images/generations", jsonContent);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data")[0].GetProperty("url").GetString();
    }
}
