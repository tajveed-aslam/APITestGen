using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using ApiTestGen.Api.Data;
using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;
using ApiTestGen.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiTestGen.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/generations")]
public sealed class GenerationsController(AppDbContext db, ITestGenerationService generator) : ControllerBase
{
    private const int HistoryLimit = 100;

    private Guid UserId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    [ProducesResponseType<GenerationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GenerationDto>> Create(CreateGenerationRequest request, CancellationToken cancellationToken)
    {
        var result = await generator.GenerateAsync(
            new GenerationInput(request.InputType, request.Input, request.BaseUrl, request.Title),
            cancellationToken);

        var generation = new Generation
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Title = result.Title,
            InputType = request.InputType,
            Input = result.Input,
            BaseUrl = result.BaseUrl,
            TestCasesJson = JsonSerializer.Serialize(result.TestCases, JsonDefaults.Options),
            PostmanCollectionJson = result.PostmanCollection.ToJsonString(),
            PytestCode = result.PytestCode,
            PositiveCount = result.TestCases.Count(t => t.Category == TestCategory.Positive),
            NegativeCount = result.TestCases.Count(t => t.Category == TestCategory.Negative),
            Model = result.Model,
            DurationMs = result.DurationMs,
            CreatedAt = DateTime.UtcNow,
        };
        db.Generations.Add(generation);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = generation.Id }, ToDto(generation));
    }

    [HttpGet]
    public async Task<IReadOnlyList<GenerationSummaryDto>> List(CancellationToken cancellationToken) =>
        await db.Generations.AsNoTracking()
            .Where(g => g.UserId == UserId)
            .OrderByDescending(g => g.CreatedAt)
            .Take(HistoryLimit)
            .Select(g => new GenerationSummaryDto(g.Id, g.Title, g.InputType, g.PositiveCount, g.NegativeCount, g.CreatedAt))
            .ToListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GenerationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerationDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var generation = await db.Generations.AsNoTracking()
            .SingleOrDefaultAsync(g => g.Id == id && g.UserId == UserId, cancellationToken);
        return generation is null ? NotFound() : ToDto(generation);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await db.Generations
            .Where(g => g.Id == id && g.UserId == UserId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? NotFound() : NoContent();
    }

    private static GenerationDto ToDto(Generation g)
    {
        using var postman = JsonDocument.Parse(g.PostmanCollectionJson);
        return new GenerationDto(
            g.Id,
            g.Title,
            g.InputType,
            g.Input,
            g.BaseUrl,
            JsonSerializer.Deserialize<List<TestCase>>(g.TestCasesJson, JsonDefaults.Options) ?? [],
            postman.RootElement.Clone(),
            g.PytestCode,
            g.Model,
            g.DurationMs,
            g.CreatedAt);
    }
}
