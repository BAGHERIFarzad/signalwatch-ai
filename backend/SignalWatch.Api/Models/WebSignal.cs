namespace SignalWatch.Api.Models;

public class WebSignal
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
}