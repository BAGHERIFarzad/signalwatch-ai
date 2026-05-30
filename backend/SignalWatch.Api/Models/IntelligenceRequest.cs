namespace SignalWatch.Api.Models;

public class IntelligenceRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyWebsite { get; set; } = string.Empty;
    public List<string> Competitors { get; set; } = new();
    public string Track { get; set; } = "GTM Intelligence";
    public string MonitoringGoal { get; set; } = string.Empty;
}