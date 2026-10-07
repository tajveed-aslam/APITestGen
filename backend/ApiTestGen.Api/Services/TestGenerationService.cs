using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;
using ApiTestGen.Api.Options;
using ApiTestGen.Api.Services.Llm;
using Microsoft.Extensions.Options;

namespace ApiTestGen.Api.Services;

public sealed record GenerationInput(InputType InputType, string Input, string? BaseUrl, string? Title);

public sealed record GenerationResult(
    string Title,
    string Input,
    string? BaseUrl,
    IReadOnlyList<TestCase> TestCases,
    JsonObject PostmanCollection,
    string PytestCode,
    string Model,
    int DurationMs);

public interface ITestGenerationService
{
    Task<GenerationResult> GenerateAsync(GenerationInput input, CancellationToken cancellationToken);
}

/// <summary>
/// Two-step pipeline: the model designs structured test cases, then those same cases drive both the
/// Postman collection (built deterministically) and the pytest module (written by the model), so all
/// three outputs always describe the same tests.
/// </summary>
public sealed class TestGenerationService(ILlmClient llm, IOptions<GenerationOptions> options) : ITestGenerationService
{
    private const int MaxTitleLength = 200;

    public async Task<GenerationResult> GenerateAsync(GenerationInput input, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var text = InputValidator.ValidateInput(input.InputType, input.Input, opts.MaxInputChars);
        var baseUrl = InputValidator.NormalizeBaseUrl(input.BaseUrl);
        var stopwatch = Stopwatch.StartNew();

        var casesJson = await llm.CompleteAsync(
            Prompts.TestCaseSystem,
            Prompts.TestCaseUser(input.InputType, text, opts.MaxTestCases),
            jsonMode: true,
            cancellationToken);
        var parsed = TestCaseParser.Parse(casesJson, opts.MaxTestCases);

        var title = Truncate(FirstNonBlank(input.Title, parsed.Title, "Untitled API"), MaxTitleLength);
        var postman = PostmanCollectionBuilder.Build(title, baseUrl, parsed.TestCases);

        var pytestRaw = await llm.CompleteAsync(
            Prompts.PytestSystem,
            Prompts.PytestUser(
                JsonSerializer.Serialize(parsed.TestCases, JsonDefaults.Options),
                baseUrl ?? PostmanCollectionBuilder.DefaultBaseUrl),
            jsonMode: false,
            cancellationToken);
        var pytest = LlmText.StripCodeFences(pytestRaw);
        if (pytest.Length == 0)
            throw new LlmException("The model returned no pytest code.");

        return new GenerationResult(
            title, text, baseUrl, parsed.TestCases, postman, pytest, llm.Model, (int)stopwatch.ElapsedMilliseconds);
    }

    private static string FirstNonBlank(params string?[] values) =>
        values.First(v => !string.IsNullOrWhiteSpace(v))!.Trim();

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
