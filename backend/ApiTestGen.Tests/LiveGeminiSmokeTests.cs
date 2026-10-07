using ApiTestGen.Api.Models;
using ApiTestGen.Api.Options;
using ApiTestGen.Api.Services;
using ApiTestGen.Api.Services.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace ApiTestGen.Tests;

/// <summary>
/// Hits the real Gemini API. Does nothing unless GEMINI_API_KEY is set, so the normal suite stays offline:
///   $env:GEMINI_API_KEY = "..."; dotnet test --filter Category=Live
/// </summary>
[Trait("Category", "Live")]
public class LiveGeminiSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Generates_cases_postman_and_pytest_for_a_real_spec()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine("GEMINI_API_KEY not set; skipping live call.");
            return;
        }

        var llm = new GeminiClient(
            new HttpClient { Timeout = TimeSpan.FromMinutes(3) },
            Microsoft.Extensions.Options.Options.Create(new GeminiOptions { ApiKey = apiKey }),
            NullLogger<GeminiClient>.Instance);
        var service = new TestGenerationService(
            llm, Microsoft.Extensions.Options.Options.Create(new GenerationOptions { MaxTestCases = 8 }));

        var result = await service.GenerateAsync(
            new GenerationInput(InputType.OpenApi, Samples.PetStoreSpec, "https://petstore.example.com/v1", null),
            CancellationToken.None);

        output.WriteLine($"{result.Title}: {result.TestCases.Count} cases in {result.DurationMs} ms");
        foreach (var tc in result.TestCases)
            output.WriteLine($"  {tc.Id} [{tc.Category}] {tc.Method} {tc.Path} -> {tc.ExpectedStatus}  {tc.Title}");
        output.WriteLine(result.PytestCode[..Math.Min(600, result.PytestCode.Length)]);

        Assert.Contains(result.TestCases, t => t.Category == TestCategory.Positive);
        Assert.Contains(result.TestCases, t => t.Category == TestCategory.Negative);
        Assert.Contains("def test_", result.PytestCode);
        Assert.DoesNotContain("```", result.PytestCode);
    }
}
