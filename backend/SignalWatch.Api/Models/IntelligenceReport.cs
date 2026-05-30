namespace SignalWatch.Api.Models;

public class IntelligenceReport
{
    public string CompanyName { get; set; } = string.Empty;
    public string Track { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public int OpportunityScore { get; set; }
    public List<WebSignal> Signals { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}