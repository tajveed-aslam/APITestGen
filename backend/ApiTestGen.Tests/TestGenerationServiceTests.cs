using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;
using ApiTestGen.Api.Options;
using ApiTestGen.Api.Services;
using ApiTestGen.Api.Services.Llm;
using Microsoft.Extensions.Options;

namespace ApiTestGen.Tests;

public class TestGenerationServiceTests
{
    private const string PytestCode = "import pytest\n\ndef test_tc_001_create_a_pet(session):\n    pass";

    private static (TestGenerationService Service, FakeLlmClient Llm) Create(params string[] responses)
    {
        var llm = new FakeLlmClient(responses);
        var service = new TestGenerationService(llm, Microsoft.Extensions.Options.Options.Create(new GenerationOptions()));
        return (service, llm);
    }

    [Fact]
    public async Task Produces_all_three_outputs_from_one_set_of_test_cases()
    {
        var (service, llm) = Create(Samples.TwoCasesJson, "```python\n" + PytestCode + "\n```");

        var result = await service.GenerateAsync(
            new GenerationInput(InputType.OpenApi, Samples.MinimalSpec, "https://api.example.com/", null),
            CancellationToken.None);

        Assert.Equal("Pet Store", result.Title);
        Assert.Equal("https://api.example.com", result.BaseUrl);
        Assert.Equal(2, result.TestCases.Count);
        Assert.Equal(PytestCode, result.PytestCode);
        Assert.Equal("fake-model", result.Model);
        Assert.Equal(2, result.PostmanCollection["item"]!.AsArray().Count);

        Assert.Equal(2, llm.Calls.Count);
        Assert.True(llm.Calls[0].JsonMode);
        Assert.False(llm.Calls[1].JsonMode);
        // The pytest prompt is built from the parsed cases, so all outputs describe the same tests.
        Assert.Contains("\"id\":\"TC-002\"", llm.Calls[1].UserPrompt);
        Assert.Contains("https://api.example.com", llm.Calls[1].UserPrompt);
    }

    [Fact]
    public async Task User_supplied_title_wins_over_the_model_title()
    {
        var (service, _) = Create(Samples.TwoCasesJson, PytestCode);

        var result = await service.GenerateAsync(
            new GenerationInput(InputType.SampleResponse, """{"id": 1}""", null, "  My API  "),
            CancellationToken.None);

        Assert.Equal("My API", result.Title);
    }

    [Fact]
    public async Task Invalid_input_fails_before_calling_the_model()
    {
        var (service, llm) = Create();

        await Assert.ThrowsAsync<InputValidationException>(() => service.GenerateAsync(
            new GenerationInput(InputType.OpenApi, "not a spec", null, null), CancellationToken.None));

        Assert.Empty(llm.Calls);
    }

    [Fact]
    public async Task Empty_pytest_output_is_an_error()
    {
        var (service, _) = Create(Samples.TwoCasesJson, "```python\n```");

        await Assert.ThrowsAsync<LlmException>(() => service.GenerateAsync(
            new GenerationInput(InputType.SampleResponse, "{}", null, null), CancellationToken.None));
    }

    private sealed class FakeLlmClient(IEnumerable<string> responses) : ILlmClient
    {
        private readonly Queue<string> _responses = new(responses);

        public List<(string SystemPrompt, string UserPrompt, bool JsonMode)> Calls { get; } = [];

        public string Model => "fake-model";

        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool jsonMode, CancellationToken cancellationToken)
        {
            Calls.Add((systemPrompt, userPrompt, jsonMode));
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
