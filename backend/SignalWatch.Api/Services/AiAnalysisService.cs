using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SignalWatch.Api.Models;

namespace SignalWatch.Api.Services;

public class AiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<AiAnalysisService> _logger;

    public AiAnalysisService(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<AiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiSignalAnalysisResult?> AnalyzeSignalsAsync(
        string companyName,
        string track,
        List<WebSignal> signals,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("AI analysis is disabled.");
            return null;
        }

        if (!IsGeminiProvider())
        {
            _logger.LogWarning("Only Gemini provider is configured in this version.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Gemini API key is missing. Falling back to rule-based analysis.");
            return null;
        }

        if (!signals.Any())
        {
            return new AiSignalAnalysisResult
            {
                ExecutiveSummary =
                    $"No live public web signals were available for {companyName}. The system could not perform a reliable AI analysis.",
                RiskScore = 20,
                OpportunityScore = 0,
                RecommendedActions = new List<string>
                {
                    "Try a broader company name or a different competitor list.",
                    "Run the live web collection again with a more specific monitoring goal."
                }
            };
        }

        var prompt = BuildPrompt(companyName, track, signals);
        var endpoint = BuildGeminiEndpoint();

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini analysis failed. StatusCode: {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    responseContent);

                return null;
            }

            var geminiJson = ExtractGeminiText(responseContent);

            if (string.IsNullOrWhiteSpace(geminiJson))
            {
                _logger.LogWarning("Gemini response did not contain text content.");
                return null;
            }

            geminiJson = CleanJsonText(geminiJson);

            var result = JsonSerializer.Deserialize<AiSignalAnalysisResult>(
                geminiJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return NormalizeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini analysis failed unexpectedly.");
            return null;
        }
    }

    private bool IsGeminiProvider()
    {
        return string.Equals(
            _options.Provider,
            "Gemini",
            StringComparison.OrdinalIgnoreCase);
    }

    private string BuildGeminiEndpoint()
    {
        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? "gemini-2.5-flash"
            : _options.Model.Trim();

        var endpointTemplate = string.IsNullOrWhiteSpace(_options.GeminiEndpoint)
            ? "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent"
            : _options.GeminiEndpoint;

        var endpoint = endpointTemplate.Replace("{model}", Uri.EscapeDataString(model));

        var separator = endpoint.Contains("?") ? "&" : "?";

        return $"{endpoint}{separator}key={Uri.EscapeDataString(_options.ApiKey)}";
    }

    private static string BuildPrompt(
        string companyName,
        string track,
        List<WebSignal> signals)
    {
        var signalsForPrompt = signals
            .Take(12)
            .Select((signal, index) => new
            {
                Index = index + 1,
                Title = signal.Title,
                SignalType = signal.SignalType,
                Summary = signal.Summary,
                Source = signal.Source,
                Url = signal.Url,
                ConfidenceScore = signal.ConfidenceScore
            })
            .ToList();

        var signalsJson = JsonSerializer.Serialize(
            signalsForPrompt,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var expectedJsonShape = """
        {
          "executiveSummary": "A concise enterprise-ready summary of the most important findings.",
          "riskScore": 0,
          "opportunityScore": 0,
          "recommendedActions": [
            "Action 1",
            "Action 2",
            "Action 3"
          ]
        }
        """;

        return
            "You are an enterprise market intelligence analyst.\n" +
            "You analyze live public web signals collected through Bright Data.\n" +
            "Return only valid JSON. Do not use markdown.\n\n" +
            "Analyze the following live public web intelligence signals.\n\n" +
            $"Company:\n{companyName}\n\n" +
            $"Hackathon track:\n{track}\n\n" +
            $"Signals:\n{signalsJson}\n\n" +
            "Return only a JSON object with this exact shape:\n" +
            expectedJsonShape +
            "\n\nScoring rules:\n" +
            "- riskScore must be an integer from 0 to 100.\n" +
            "- opportunityScore must be an integer from 0 to 100.\n" +
            "- recommendedActions must contain 4 to 6 practical business actions.\n" +
            "- Focus on enterprise GTM, finance, market, security, and compliance value.\n" +
            "- Do not invent sources that are not present in the signals.\n" +
            "- Mention that signals are based on live public web data only when useful.";
    }

    private static string? ExtractGeminiText(string responseContent)
    {
        using var document = JsonDocument.Parse(responseContent);
        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates))
            return null;

        var firstCandidate = candidates.EnumerateArray().FirstOrDefault();

        if (firstCandidate.ValueKind == JsonValueKind.Undefined)
            return null;

        if (!firstCandidate.TryGetProperty("content", out var content))
            return null;

        if (!content.TryGetProperty("parts", out var parts))
            return null;

        var firstPart = parts.EnumerateArray().FirstOrDefault();

        if (firstPart.ValueKind == JsonValueKind.Undefined)
            return null;

        if (!firstPart.TryGetProperty("text", out var text))
            return null;

        return text.GetString();
    }

    private static string CleanJsonText(string text)
    {
        var cleaned = text.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring("```json".Length).Trim();
        }

        if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring("```".Length).Trim();
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - "```".Length).Trim();
        }

        return cleaned;
    }

    private static AiSignalAnalysisResult? NormalizeResult(
        AiSignalAnalysisResult? result)
    {
        if (result is null)
            return null;

        result.ExecutiveSummary = string.IsNullOrWhiteSpace(result.ExecutiveSummary)
            ? "AI analysis completed, but no executive summary was returned."
            : result.ExecutiveSummary.Trim();

        result.RiskScore = Math.Clamp(result.RiskScore, 0, 100);
        result.OpportunityScore = Math.Clamp(result.OpportunityScore, 0, 100);

        result.RecommendedActions = result.RecommendedActions
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Select(action => action.Trim())
            .Distinct()
            .Take(6)
            .ToList();

        if (!result.RecommendedActions.Any())
        {
            result.RecommendedActions = new List<string>
            {
                "Review the highest-confidence public web signals.",
                "Validate findings with source links before business action.",
                "Set up recurring monitoring for this company and its competitors."
            };
        }

        return result;
    }
}