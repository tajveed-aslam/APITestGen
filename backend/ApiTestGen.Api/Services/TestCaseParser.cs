using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;

namespace ApiTestGen.Api.Services;

public sealed record ParsedTestCases(string? Title, IReadOnlyList<TestCase> TestCases);

/// <summary>
/// Turns the model's JSON into validated <see cref="TestCase"/>s. Malformed entries are dropped rather than
/// failing the whole generation; ids are assigned here so they're always sequential.
/// </summary>
public static class TestCaseParser
{
    private static readonly HashSet<string> AllowedMethods =
        ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    public static ParsedTestCases Parse(string raw, int maxCases)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(LlmText.StripCodeFences(raw));
        }
        catch (JsonException ex)
        {
            throw new LlmException("The model returned malformed JSON for the test cases.", ex);
        }

        var items = root switch
        {
            JsonArray array => array,
            JsonObject obj when obj["testCases"] is JsonArray array => array,
            _ => throw new LlmException("The model's response had no \"testCases\" array."),
        };

        var cases = new List<TestCase>();
        foreach (var item in items.OfType<JsonObject>())
        {
            if (cases.Count >= maxCases)
                break;
            if (TryParseCase(item, cases.Count + 1) is { } testCase)
                cases.Add(testCase);
        }

        if (cases.Count == 0)
            throw new LlmException("The model did not return any usable test cases.");

        return new ParsedTestCases(GetString((root as JsonObject)?["title"])?.Trim(), cases);
    }

    private static TestCase? TryParseCase(JsonObject o, int index)
    {
        var method = GetString(o["method"])?.Trim().ToUpperInvariant();
        if (method is null || !AllowedMethods.Contains(method))
            return null;

        var path = NormalizePath(GetString(o["path"]));
        if (path is null)
            return null;

        var status = GetInt(o["expectedStatus"]);
        if (status is null or < 100 or > 599)
            return null;

        var category = GetString(o["category"])?.Trim().ToLowerInvariant() switch
        {
            "positive" => TestCategory.Positive,
            "negative" => TestCategory.Negative,
            _ => status < 400 ? TestCategory.Positive : TestCategory.Negative,
        };

        var body = o["body"];
        return new TestCase
        {
            Id = $"TC-{index:000}",
            Title = GetString(o["title"])?.Trim() is { Length: > 0 } title ? title : $"{method} {path}",
            Category = category,
            Method = method,
            Path = path,
            Description = GetString(o["description"])?.Trim() ?? "",
            Headers = ToStringMap(o["headers"]),
            QueryParams = ToStringMap(o["queryParams"]),
            Body = body is null ? null : JsonSerializer.SerializeToElement(body),
            ExpectedStatus = status.Value,
            Assertions = (o["assertions"] as JsonArray)?
                .Select(GetString)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a!.Trim())
                .ToList() ?? [],
        };
    }

    private static string? NormalizePath(string? path)
    {
        path = path?.Trim();
        if (string.IsNullOrEmpty(path))
            return null;

        // Models occasionally return a full URL; keep only the path part.
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http"))
            path = uri.AbsolutePath;

        path = path.Split('?')[0];
        return path.StartsWith('/') ? path : "/" + path;
    }

    private static string? GetString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    private static int? GetInt(JsonNode? node)
    {
        if (node is not JsonValue value)
            return null;
        if (value.TryGetValue<int>(out var i))
            return i;
        if (value.TryGetValue<double>(out var d) && d == Math.Floor(d))
            return (int)d;
        if (value.TryGetValue<string>(out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            return i;
        return null;
    }

    private static Dictionary<string, string> ToStringMap(JsonNode? node)
    {
        var map = new Dictionary<string, string>();
        if (node is not JsonObject obj)
            return map;

        foreach (var (key, value) in obj)
        {
            if (value is null)
                continue;
            map[key] = GetString(value) ?? value.ToJsonString();
        }
        return map;
    }
}
