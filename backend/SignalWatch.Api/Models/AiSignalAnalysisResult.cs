namespace SignalWatch.Api.Models;

public class AiSignalAnalysisResult
{
    public string ExecutiveSummary { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public int OpportunityScore { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}