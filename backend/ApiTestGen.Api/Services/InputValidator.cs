using System.Text.Json;
using ApiTestGen.Api.Infrastructure;
using ApiTestGen.Api.Models;

namespace ApiTestGen.Api.Services;

public static class InputValidator
{
    /// <summary>Checks the pasted input and returns it trimmed, or throws <see cref="InputValidationException"/>.</summary>
    public static string ValidateInput(InputType inputType, string? input, int maxChars)
    {
        var text = input?.Trim() ?? "";
        if (text.Length == 0)
            throw new InputValidationException("Paste an OpenAPI/Swagger JSON spec or a sample API response.");
        if (text.Length > maxChars)
            throw new InputValidationException($"Input is {text.Length:N0} characters; the limit is {maxChars:N0}.");

        if (inputType == InputType.OpenApi)
            ValidateOpenApi(text);

        return text;
    }

    /// <summary>Returns an absolute http(s) base URL without a trailing slash, or null when none was given.</summary>
    public static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InputValidationException("Base URL must be an absolute http(s) URL, e.g. https://api.example.com.");

        return uri.ToString().TrimEnd('/');
    }

    private static void ValidateOpenApi(string text)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            throw new InputValidationException(
                "The spec isn't valid JSON. Paste the OpenAPI/Swagger document as JSON (YAML isn't supported).");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                (!root.TryGetProperty("openapi", out _) && !root.TryGetProperty("swagger", out _)))
                throw new InputValidationException(
                    "This doesn't look like an OpenAPI/Swagger spec (no \"openapi\" or \"swagger\" field). " +
                    "Use the \"Sample response\" mode for plain API responses.");

            if (!root.TryGetProperty("paths", out var paths) ||
                paths.ValueKind != JsonValueKind.Object ||
                !paths.EnumerateObject().Any())
                throw new InputValidationException("The spec has no \"paths\", so there's nothing to test.");
        }
    }
}
