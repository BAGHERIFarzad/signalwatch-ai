namespace SignalWatch.Api.Models;

public class BrightDataOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string SerpEndpoint { get; set; } = "https://api.brightdata.com/request";
    public string SerpZone { get; set; } = "serp_api1";
}