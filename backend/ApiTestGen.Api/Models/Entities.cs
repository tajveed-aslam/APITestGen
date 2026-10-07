namespace ApiTestGen.Api.Models;

public enum InputType
{
    OpenApi,
    SampleResponse,
}

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public List<Generation> Generations { get; set; } = [];
}

public sealed class Generation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = "";
    public InputType InputType { get; set; }
    public string Input { get; set; } = "";
    public string? BaseUrl { get; set; }

    /// <summary>Serialized <see cref="TestCase"/> list (jsonb).</summary>
    public string TestCasesJson { get; set; } = "[]";
    /// <summary>Postman collection v2.1 document (jsonb).</summary>
    public string PostmanCollectionJson { get; set; } = "{}";
    public string PytestCode { get; set; } = "";

    public int PositiveCount { get; set; }
    public int NegativeCount { get; set; }
    public string Model { get; set; } = "";
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
