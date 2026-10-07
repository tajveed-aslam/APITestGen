using System.Text.Json;

namespace ApiTestGen.Api.Models;

public enum TestCategory
{
    Positive,
    Negative,
}

public sealed class TestCase
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public TestCategory Category { get; init; }
    public string Method { get; init; } = "GET";
    public string Path { get; init; } = "/";
    public string Description { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = [];
    public Dictionary<string, string> QueryParams { get; init; } = [];
    public JsonElement? Body { get; init; }
    public int ExpectedStatus { get; init; }
    public List<string> Assertions { get; init; } = [];
}
