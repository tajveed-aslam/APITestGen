using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ApiTestGen.Api.Models;

public sealed record RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = "";

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = "";
}

public sealed record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}

public sealed record AuthResponse(string Token, DateTime ExpiresAt, string Email);

public sealed record UserDto(Guid Id, string Email);

public sealed record CreateGenerationRequest
{
    [Required]
    public InputType InputType { get; init; }

    [Required]
    public string Input { get; init; } = "";

    [MaxLength(2048)]
    public string? BaseUrl { get; init; }

    [MaxLength(200)]
    public string? Title { get; init; }
}

public sealed record GenerationSummaryDto(
    Guid Id,
    string Title,
    InputType InputType,
    int PositiveCount,
    int NegativeCount,
    DateTime CreatedAt);

public sealed record GenerationDto(
    Guid Id,
    string Title,
    InputType InputType,
    string Input,
    string? BaseUrl,
    IReadOnlyList<TestCase> TestCases,
    JsonElement PostmanCollection,
    string PytestCode,
    string Model,
    int DurationMs,
    DateTime CreatedAt);
