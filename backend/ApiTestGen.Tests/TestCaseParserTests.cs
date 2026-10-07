using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;
using ApiTestGen.Api.Services;

namespace ApiTestGen.Tests;

public class TestCaseParserTests
{
    [Fact]
    public void Parses_valid_cases_and_assigns_sequential_ids()
    {
        var parsed = TestCaseParser.Parse(Samples.TwoCasesJson, maxCases: 10);

        Assert.Equal("Pet Store", parsed.Title);
        Assert.Collection(parsed.TestCases,
            first =>
            {
                Assert.Equal("TC-001", first.Id);
                Assert.Equal(TestCategory.Positive, first.Category);
                Assert.Equal("POST", first.Method);
                Assert.Equal("/pets", first.Path);
                Assert.Equal(201, first.ExpectedStatus);
                Assert.Equal("Bearer {{authToken}}", first.Headers["Authorization"]);
                Assert.Equal("Rex", first.Body!.Value.GetProperty("name").GetString());
            },
            second =>
            {
                Assert.Equal("TC-002", second.Id);
                Assert.Equal(TestCategory.Negative, second.Category);
                Assert.Null(second.Body);
            });
    }

    [Fact]
    public void Strips_markdown_fences_around_json()
    {
        var parsed = TestCaseParser.Parse("```json\n" + Samples.TwoCasesJson + "\n```", maxCases: 10);

        Assert.Equal(2, parsed.TestCases.Count);
    }

    [Fact]
    public void Drops_invalid_entries_and_keeps_ids_contiguous()
    {
        const string json = """
            { "testCases": [
              { "title": "bad method", "method": "FETCH", "path": "/a", "expectedStatus": 200 },
              { "title": "bad status", "method": "GET", "path": "/a", "expectedStatus": 42 },
              { "title": "no path", "method": "GET", "expectedStatus": 200 },
              { "title": "ok", "method": "get", "path": "users", "expectedStatus": "200" }
            ] }
            """;

        var parsed = TestCaseParser.Parse(json, maxCases: 10);

        var only = Assert.Single(parsed.TestCases);
        Assert.Equal("TC-001", only.Id);
        Assert.Equal("GET", only.Method);
        Assert.Equal("/users", only.Path);
        Assert.Equal(200, only.ExpectedStatus);
    }

    [Fact]
    public void Infers_category_from_status_when_missing()
    {
        const string json = """
            { "testCases": [
              { "method": "GET", "path": "/a", "expectedStatus": 200 },
              { "method": "GET", "path": "/b", "expectedStatus": 404 }
            ] }
            """;

        var parsed = TestCaseParser.Parse(json, maxCases: 10);

        Assert.Equal(TestCategory.Positive, parsed.TestCases[0].Category);
        Assert.Equal(TestCategory.Negative, parsed.TestCases[1].Category);
    }

    [Fact]
    public void Reduces_absolute_urls_to_paths()
    {
        const string json = """{ "testCases": [ { "method": "GET", "path": "https://api.example.com/v1/pets?limit=5", "expectedStatus": 200 } ] }""";

        Assert.Equal("/v1/pets", TestCaseParser.Parse(json, 10).TestCases[0].Path);
    }

    [Fact]
    public void Respects_the_max_case_limit()
    {
        var parsed = TestCaseParser.Parse(Samples.TwoCasesJson, maxCases: 1);

        Assert.Single(parsed.TestCases);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "cases": [] }""")]
    [InlineData("""{ "testCases": [ { "title": "no method" } ] }""")]
    public void Throws_when_nothing_usable_is_returned(string raw)
    {
        Assert.Throws<LlmException>(() => TestCaseParser.Parse(raw, 10));
    }
}
