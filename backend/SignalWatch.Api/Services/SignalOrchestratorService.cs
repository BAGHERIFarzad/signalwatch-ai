using SignalWatch.Api.Models;

namespace SignalWatch.Api.Services;

public class SignalOrchestratorService
{
    private readonly BrightDataService _brightDataService;
    private readonly AiAnalysisService _aiAnalysisService;

    public SignalOrchestratorService(
        BrightDataService brightDataService,
        AiAnalysisService aiAnalysisService)
    {
        _brightDataService = brightDataService;
        _aiAnalysisService = aiAnalysisService;
    }

    public Task<IntelligenceReport> GenerateDemoReportAsync(IntelligenceRequest request)
    {
        var companyName = string.IsNullOrWhiteSpace(request.CompanyName)
            ? "Target Company"
            : request.CompanyName;

        var report = new IntelligenceReport
        {
            CompanyName = companyName,
            Track = request.Track,
            ExecutiveSummary =
                $"{companyName} shows several live market signals across competitor activity, hiring movement, pricing changes, and public web visibility. " +
                "The current intelligence profile suggests strong opportunity potential with moderate operational and competitive risk.",
            RiskScore = 42,
            OpportunityScore = 81,
            GeneratedAt = DateTime.UtcNow,
            Signals = new List<WebSignal>
            {
                new WebSignal
                {
                    Title = "Competitor messaging shift detected",
                    Source = "Demo data",
                    Url = "https://example.com/competitor-update",
                    SignalType = "GTM",
                    Summary = "A competitor appears to be repositioning its offer around AI automation and enterprise productivity.",
                    ConfidenceScore = 87
                }
            },
            RecommendedActions = new List<string>
            {
                "Monitor competitor website and pricing pages weekly.",
                "Track hiring signals to detect strategic investment areas.",
                "Create a risk review workflow for supplier and partner mentions."
            }
        };

        return Task.FromResult(report);
    }

    public async Task<IntelligenceReport> GenerateLiveReportAsync(
        IntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var companyName = string.IsNullOrWhiteSpace(request.CompanyName)
            ? "Target Company"
            : request.CompanyName;

        var liveResults = await _brightDataService.SearchCompanySignalsAsync(
            request,
            cancellationToken);

        var signals = liveResults.Select(result => new WebSignal
        {
            Title = result.Title,
            Source = "Bright Data SERP API",
            Url = result.Url,
            SignalType = DetectSignalType(result),
            Summary = string.IsNullOrWhiteSpace(result.Description)
                ? $"Live web result found for query: {result.SourceQuery}"
                : CleanSummary(result.Description),
            ConfidenceScore = CalculateConfidenceScore(result)
        }).ToList();

        var ruleBasedRiskScore = CalculateRiskScore(signals);
        var ruleBasedOpportunityScore = CalculateOpportunityScore(signals);

        var ruleBasedSummary = BuildExecutiveSummary(
            companyName,
            signals,
            ruleBasedRiskScore,
            ruleBasedOpportunityScore);

        var ruleBasedActions = BuildRecommendedActions(signals);

        var aiAnalysis = await _aiAnalysisService.AnalyzeSignalsAsync(
            companyName,
            request.Track,
            signals,
            cancellationToken);

        return new IntelligenceReport
        {
            CompanyName = companyName,
            Track = request.Track,
            ExecutiveSummary = aiAnalysis?.ExecutiveSummary ?? ruleBasedSummary,
            RiskScore = aiAnalysis?.RiskScore ?? ruleBasedRiskScore,
            OpportunityScore = aiAnalysis?.OpportunityScore ?? ruleBasedOpportunityScore,
            Signals = signals,
            RecommendedActions = aiAnalysis?.RecommendedActions.Any() == true
                ? aiAnalysis.RecommendedActions
                : ruleBasedActions,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static string DetectSignalType(BrightDataSearchResult result)
    {
        var text = $"{result.Title} {result.Description} {result.SourceQuery}".ToLowerInvariant();

        if (
            text.Contains("security") ||
            text.Contains("compliance") ||
            text.Contains("risk") ||
            text.Contains("breach") ||
            text.Contains("vulnerability") ||
            text.Contains("incident") ||
            text.Contains("regulation") ||
            text.Contains("privacy") ||
            text.Contains("soc 2") ||
            text.Contains("iso 27001")
        )
            return "Security / Compliance";

        if (
            text.Contains("hiring") ||
            text.Contains("jobs") ||
            text.Contains("career") ||
            text.Contains("revenue") ||
            text.Contains("stock") ||
            text.Contains("earnings") ||
            text.Contains("investor") ||
            text.Contains("financial") ||
            text.Contains("market")
        )
            return "Finance / Market";

        if (
            text.Contains("pricing") ||
            text.Contains("price") ||
            text.Contains("competitor") ||
            text.Contains("vs") ||
            text.Contains("comparison") ||
            text.Contains("product") ||
            text.Contains("launch") ||
            text.Contains("customers") ||
            text.Contains("sales") ||
            text.Contains("gtm") ||
            text.Contains("press release") ||
            text.Contains("newsroom") ||
            text.Contains("enterprise") ||
            text.Contains("platform")
        )
            return "GTM";

        return "General Intelligence";
    }

    private static string CleanSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return string.Empty;

        return summary
            .Replace("...Read more", "...")
            .Replace("Read more", "")
            .Replace("read more", "")
            .Trim();
    }

    private static int CalculateConfidenceScore(BrightDataSearchResult result)
    {
        var score = 65;

        if (!string.IsNullOrWhiteSpace(result.Title))
            score += 10;

        if (!string.IsNullOrWhiteSpace(result.Description))
            score += 10;

        if (!string.IsNullOrWhiteSpace(result.Url))
            score += 5;

        return Math.Min(score, 95);
    }

    private static int CalculateRiskScore(List<WebSignal> signals)
    {
        var riskKeywords = new[]
        {
            "risk",
            "security",
            "breach",
            "lawsuit",
            "compliance",
            "warning",
            "incident"
        };

        var score = 25;

        foreach (var signal in signals)
        {
            var text = $"{signal.Title} {signal.Summary}".ToLowerInvariant();

            if (riskKeywords.Any(text.Contains))
                score += 8;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static int CalculateOpportunityScore(List<WebSignal> signals)
    {
        var opportunityKeywords = new[]
        {
            "launch",
            "growth",
            "hiring",
            "partnership",
            "pricing",
            "market",
            "ai",
            "data"
        };

        var score = 40;

        foreach (var signal in signals)
        {
            var text = $"{signal.Title} {signal.Summary}".ToLowerInvariant();

            if (opportunityKeywords.Any(text.Contains))
                score += 6;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static string BuildExecutiveSummary(
        string companyName,
        List<WebSignal> signals,
        int riskScore,
        int opportunityScore)
    {
        if (!signals.Any())
        {
            return $"No live public web signals were found for {companyName}. Try another company name or broaden the monitoring goal.";
        }

        return
            $"{companyName} has {signals.Count} live public web signals collected through Bright Data. " +
            $"The current profile shows a risk score of {riskScore} and an opportunity score of {opportunityScore}. " +
            "The detected signals include competitor, market, hiring, pricing, and risk-related information that can support GTM, finance, and compliance decisions.";
    }

    private static List<string> BuildRecommendedActions(List<WebSignal> signals)
    {
        var actions = new List<string>
        {
            "Review the highest-confidence live web signals first.",
            "Validate critical findings with source links before business action.",
            "Set up recurring monitoring for competitor, pricing, hiring, and risk-related queries."
        };

        if (signals.Any(s => s.SignalType == "GTM"))
            actions.Add("Send GTM-related findings to sales or revenue operations teams.");

        if (signals.Any(s => s.SignalType == "Finance / Market"))
            actions.Add("Use market and hiring signals as alternative data for planning.");

        if (signals.Any(s => s.SignalType == "Security / Compliance"))
            actions.Add("Escalate security or compliance signals for internal review.");

        return actions;
    }
}