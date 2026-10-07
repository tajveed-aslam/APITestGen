namespace ApiTestGen.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "ApiTestGen";
    public string Audience { get; set; } = "ApiTestGen.Client";
    public int ExpiryMinutes { get; set; } = 480;
}

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"Gemini" or "OpenAI".</summary>
    public string Provider { get; set; } = "Gemini";
}

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-flash-lite-latest";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    /// <summary>Includes the model's thinking tokens, so keep this generous.</summary>
    public int MaxOutputTokens { get; set; } = 16384;
}

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4.1-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public int MaxCompletionTokens { get; set; } = 8000;
}

public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Allow one-click guest accounts for the public live demo.</summary>
    public bool GuestAccessEnabled { get; set; } = true;
    public int GuestTokenMinutes { get; set; } = 120;
    public int GuestSessionsPerHourPerIp { get; set; } = 5;
    public int GuestGenerationsPerHour { get; set; } = 5;
    public int UserGenerationsPerHour { get; set; } = 30;
}

public sealed class GenerationOptions
{
    public const string SectionName = "Generation";

    public int MaxInputChars { get; set; } = 200_000;
    public int MaxTestCases { get; set; } = 30;
}
