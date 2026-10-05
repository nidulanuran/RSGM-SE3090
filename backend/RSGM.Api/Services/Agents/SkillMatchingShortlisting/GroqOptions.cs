namespace RSGM.Api.Services.Agents.SkillMatchingShortlisting;

public sealed class GroqOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } =
        "https://api.groq.com/openai/v1/chat/completions";

    public string Model { get; set; } = "openai/gpt-oss-20b";

    public int TimeoutSeconds { get; set; } = 60;
}