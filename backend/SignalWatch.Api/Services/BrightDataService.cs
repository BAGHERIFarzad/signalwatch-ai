using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SignalWatch.Api.Models;

namespace SignalWatch.Api.Services;

public class BrightDataService
{
    private readonly HttpClient _httpClient;
    private readonly BrightDataOptions _options;
    private readonly ILogger<BrightDataService> _logger;

    public BrightDataService(
        HttpClient httpClient,
        IOptions<BrightDataOptions> options,
        ILogger<BrightDataService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<BrightDataSearchResult>> SearchCompanySignalsAsync(
        IntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var queries = BuildQueries(request);
        var allResults = new List<BrightDataSearchResult>();

        foreach (var query in queries)
        {
            try
            {
                var results = await SearchAsync(query, cancellationToken);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bright Data SERP search failed for query: {Query}", query);
            }
        }

        return allResults
            .Where(r =>
                !string.IsNullOrWhiteSpace(r.Title) &&
                !string.IsNullOrWhiteSpace(r.Url) &&
                r.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Where(r => !IsLowValueSearchResult(r))
            .GroupBy(r => r.Url)
            .Select(g => g.First())
            .Take(12)
            .ToList();
    }

    private List<string> BuildQueries(IntelligenceRequest request)
    {
        var company = request.CompanyName.Trim();

        var queries = new List<string>
        {
            $"{company} press release product launch",
            $"{company} latest product updates enterprise AI",
            $"{company} pricing page enterprise plan",
            $"{company} hiring data engineering AI site:linkedin.com/jobs OR site:greenhouse.io OR site:lever.co",
            $"{company} security compliance incident risk",
            $"{company} competitor positioning observability monitoring"
        };

        foreach (var competitor in request.Competitors.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var competitorName = competitor.Trim();

            queries.Add($"{company} vs {competitorName} comparison");
            queries.Add($"{competitorName} product launch pricing enterprise");
            queries.Add($"{competitorName} observability monitoring AI press release");
        }

        return queries;
    }

    private async Task<List<BrightDataSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Bright Data API key is missing.");
        }

        var payload = new
        {
            zone = _options.SerpZone,
            url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}",
            format = "json"
        };

        using var requestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            _options.SerpEndpoint);

        requestMessage.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var json = JsonSerializer.Serialize(payload);

        requestMessage.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation(
            "Bright Data raw response preview for query {Query}: {Preview}",
            query,
            responseContent.Length > 1500
                ? responseContent.Substring(0, 1500)
                : responseContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Bright Data returned {StatusCode}: {Body}",
                response.StatusCode,
                responseContent);

            throw new HttpRequestException(
                $"Bright Data request failed: {response.StatusCode}");
        }

        return ParseSerpResults(responseContent, query);
    }

    private List<BrightDataSearchResult> ParseSerpResults(string rawJson, string sourceQuery)
    {
        var results = new List<BrightDataSearchResult>();

        using var outerDocument = JsonDocument.Parse(rawJson);
        var outerRoot = outerDocument.RootElement;

        // Bright Data wraps the real SERP JSON inside "body" as a JSON string.
        if (outerRoot.TryGetProperty("body", out var bodyElement))
        {
            if (bodyElement.ValueKind == JsonValueKind.String)
            {
                var bodyJson = bodyElement.GetString();

                if (!string.IsNullOrWhiteSpace(bodyJson))
                {
                    using var innerDocument = JsonDocument.Parse(bodyJson);
                    ParseJsonTree(innerDocument.RootElement, sourceQuery, results);
                }
            }
            else if (bodyElement.ValueKind == JsonValueKind.Object)
            {
                ParseJsonTree(bodyElement, sourceQuery, results);
            }
        }
        else
        {
            // Fallback if Bright Data returns direct JSON.
            ParseJsonTree(outerRoot, sourceQuery, results);
        }

        return results
            .Where(r =>
                !string.IsNullOrWhiteSpace(r.Title) &&
                !string.IsNullOrWhiteSpace(r.Url) &&
                r.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Where(r => !IsLowValueSearchResult(r))
            .GroupBy(r => r.Url)
            .Select(g => g.First())
            .Take(12)
            .ToList();
    }

    private static bool IsLowValueSearchResult(BrightDataSearchResult result)
    {
        var title = result.Title.Trim().ToLowerInvariant();
        var url = result.Url.Trim().ToLowerInvariant();

        var blockedExactTitles = new[]
        {
            "maps",
            "images",
            "videos",
            "news",
            "shopping",
            "books",
            "flights",
            "finance",
            "all"
        };

        if (blockedExactTitles.Contains(title))
            return true;

        var blockedUrlParts = new[]
        {
            "google.com/search",
            "maps.google.com",
            "google.com/maps",
            "google.com/imgres",
            "google.com/preferences",
            "accounts.google.com",
            "support.google.com",
            "webcache.googleusercontent.com",
            "/search?",
            "tbm=isch",
            "tbm=vid",
            "tbm=nws"
        };

        if (blockedUrlParts.Any(url.Contains))
            return true;

        if (title.Length <= 3)
            return true;

        return false;
    }

    private void ParseJsonTree(
        JsonElement element,
        string sourceQuery,
        List<BrightDataSearchResult> results)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            TryAddSearchResult(element, sourceQuery, results);

            foreach (var property in element.EnumerateObject())
            {
                ParseJsonTree(property.Value, sourceQuery, results);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ParseJsonTree(item, sourceQuery, results);
            }
        }
    }

    private void TryAddSearchResult(
        JsonElement item,
        string sourceQuery,
        List<BrightDataSearchResult> results)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return;

        var title = TryGetFirstString(
            item,
            "title",
            "name",
            "headline"
        );

        var url = TryGetFirstString(
            item,
            "link",
            "url",
            "href",
            "source_url",
            "display_link"
        );

        var description = TryGetFirstString(
            item,
            "description",
            "desc",
            "snippet",
            "text",
            "content"
        );

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url))
            return;

        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return;

        var result = new BrightDataSearchResult
        {
            Title = title,
            Url = url,
            Description = description,
            SourceQuery = sourceQuery
        };

        if (IsLowValueSearchResult(result))
            return;

        results.Add(result);
    }

    private static string TryGetFirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;

                if (value.ValueKind == JsonValueKind.Number)
                    return value.ToString();
            }
        }

        return string.Empty;
    }
}