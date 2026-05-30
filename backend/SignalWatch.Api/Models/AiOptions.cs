namespace SignalWatch.Api.Models;

public class AiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Provider { get; set; } = "Gemini";

    public string GeminiEndpoint { get; set; } =
        "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

    public string Model { get; set; } = "gemini-2.5-flash";

    public bool Enabled { get; set; } = true;
}