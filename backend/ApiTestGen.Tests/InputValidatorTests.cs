using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;
using ApiTestGen.Api.Services;

namespace ApiTestGen.Tests;

public class InputValidatorTests
{
    [Fact]
    public void Accepts_a_minimal_openapi_spec()
    {
        Assert.Equal(Samples.MinimalSpec, InputValidator.ValidateInput(InputType.OpenApi, "  " + Samples.MinimalSpec + "\n", 10_000));
    }

    [Theory]
    [InlineData("openapi: 3.0.0\npaths: {}", "valid JSON")]
    [InlineData("""{ "info": {}, "paths": { "/a": {} } }""", "doesn't look like")]
    [InlineData("""{ "openapi": "3.0.0", "paths": {} }""", "no \"paths\"")]
    [InlineData("   ", "Paste")]
    public void Rejects_unusable_specs_with_a_helpful_message(string input, string expectedMessage)
    {
        var ex = Assert.Throws<InputValidationException>(() => InputValidator.ValidateInput(InputType.OpenApi, input, 10_000));

        Assert.Contains(expectedMessage, ex.Message);
    }

    [Fact]
    public void Accepts_any_non_empty_sample_response()
    {
        Assert.Equal("""{"id": 1}""", InputValidator.ValidateInput(InputType.SampleResponse, """{"id": 1}""", 100));
    }

    [Fact]
    public void Rejects_input_over_the_size_limit()
    {
        Assert.Throws<InputValidationException>(() => InputValidator.ValidateInput(InputType.SampleResponse, new string('x', 101), 100));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("https://api.example.com/", "https://api.example.com")]
    [InlineData("http://localhost:8080/v1", "http://localhost:8080/v1")]
    public void Normalizes_base_urls(string? input, string? expected)
    {
        Assert.Equal(expected, InputValidator.NormalizeBaseUrl(input));
    }

    [Theory]
    [InlineData("api.example.com")]
    [InlineData("ftp://example.com")]
    public void Rejects_non_http_base_urls(string input)
    {
        Assert.Throws<InputValidationException>(() => InputValidator.NormalizeBaseUrl(input));
    }
}
