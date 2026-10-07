using System.Text.Json.Nodes;
using ApiTestGen.Api.Services;

namespace ApiTestGen.Tests;

public class PostmanCollectionBuilderTests
{
    private static JsonObject Build(string? baseUrl = "https://api.example.com") =>
        PostmanCollectionBuilder.Build("Pet Store", baseUrl, TestCaseParser.Parse(Samples.TwoCasesJson, 10).TestCases);

    [Fact]
    public void Uses_the_v21_schema_and_title()
    {
        var collection = Build();

        Assert.Equal(PostmanCollectionBuilder.SchemaUrl, collection["info"]!["schema"]!.GetValue<string>());
        Assert.Equal("Pet Store", collection["info"]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void Groups_cases_into_positive_and_negative_folders()
    {
        var folders = Build()["item"]!.AsArray();

        Assert.Equal(["Positive tests", "Negative tests"], folders.Select(f => f!["name"]!.GetValue<string>()));
        Assert.Equal("TC-001 Create a pet", folders[0]!["item"]![0]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void Exposes_base_url_and_auth_token_as_variables()
    {
        var variables = Build()["variable"]!.AsArray();

        Assert.Equal("https://api.example.com", variables.Single(v => v!["key"]!.GetValue<string>() == "baseUrl")!["value"]!.GetValue<string>());
        Assert.Contains(variables, v => v!["key"]!.GetValue<string>() == "authToken");
    }

    [Fact]
    public void Falls_back_to_a_default_base_url()
    {
        var variables = Build(baseUrl: null)["variable"]!.AsArray();

        Assert.Equal(PostmanCollectionBuilder.DefaultBaseUrl, variables[0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public void Builds_request_with_body_query_and_json_content_type()
    {
        var request = Build()["item"]![0]!["item"]![0]!["request"]!;

        Assert.Equal("POST", request["method"]!.GetValue<string>());
        Assert.Equal("{{baseUrl}}/pets?dryRun=false", request["url"]!["raw"]!.GetValue<string>());
        Assert.Equal(["pets"], request["url"]!["path"]!.AsArray().Select(s => s!.GetValue<string>()));
        Assert.Contains("\"name\": \"Rex\"", request["body"]!["raw"]!.GetValue<string>());

        var headers = request["header"]!.AsArray().Select(h => h!["key"]!.GetValue<string>()).ToList();
        Assert.Contains("Content-Type", headers);
        Assert.Contains("Authorization", headers);
    }

    [Fact]
    public void Adds_a_status_code_test_script()
    {
        var exec = Build()["item"]![1]!["item"]![0]!["event"]![0]!["script"]!["exec"]!.AsArray()
            .Select(l => l!.GetValue<string>()).ToList();

        Assert.Contains("    pm.response.to.have.status(404);", exec);
        Assert.Contains(exec, line => line.Contains("error message mentions the pet"));
    }
}
