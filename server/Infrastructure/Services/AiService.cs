using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Application.Common.Interfaces;
using Application.Config;

namespace Infrastructure.Services;

public class AiService : IAiService
{
    private readonly HttpClient _http;
    private readonly AiServerSettings _settings;

    public AiService(HttpClient http, IOptions<AppSettings> options)
    {
        _http = http;
        _settings = options.Value.AiServer;
    }

    public async Task<string> ChatAsync(string userMessage, List<(string Role, string Content)> history)
    {
        var payload = new
        {
            message = userMessage,
            history = history.Select(h => new { role = h.Role, content = h.Content })
        };

        var response = await _http.PostAsJsonAsync(_settings.ChatEndpoint, payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("reply").GetString() ?? string.Empty;
    }

    public async Task<object> AnalyzeProjectRiskAsync(object projectData)
    {
        var response = await _http.PostAsJsonAsync(_settings.RiskEndpoint, projectData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<object>() ?? new { };
    }
}
