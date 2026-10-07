using ApiTestGen.Api.Models;

namespace ApiTestGen.Api.Services;

public static class Prompts
{
    public const string TestCaseSystem =
        "You are a senior QA engineer who designs thorough, realistic API test suites. You respond with JSON only.";

    public const string PytestSystem =
        "You are a senior SDET who writes clean, idiomatic pytest suites. " +
        "Respond with Python source code only: no explanations and no markdown fences.";

    public static string TestCaseUser(InputType inputType, string input, int maxCases)
    {
        var source = inputType == InputType.OpenApi
            ? "the OpenAPI/Swagger specification below"
            : "the sample API response below";

        var sourceRules = inputType == InputType.OpenApi
            ? """
              - Cover every operation in the spec that matters; prioritise ones with request bodies, path parameters and auth.
              - Use the spec's schemas, required fields, enums, formats and documented status codes.
              """
            : """
              - The input is a response from a single endpoint. Infer the resource schema from it and the most plausible
                REST endpoint(s) that would return it (e.g. GET /users/42 for a single user object, GET /users for a list).
              - Positive cases should assert the response shape and field types seen in the sample.
              """;

        return $$$"""
            Design API test cases for {{{source}}}.

            Return a JSON object with exactly this shape:
            {
              "title": "short name for the API under test",
              "testCases": [
                {
                  "title": "short imperative summary",
                  "category": "positive" | "negative",
                  "method": "GET" | "POST" | "PUT" | "PATCH" | "DELETE" | "HEAD" | "OPTIONS",
                  "path": "/path/relative/to/base/url",
                  "description": "what this verifies and why it matters",
                  "headers": { "Header-Name": "value" },
                  "queryParams": { "name": "value" },
                  "body": <JSON request body, or null>,
                  "expectedStatus": 200,
                  "assertions": ["concrete checks on the response, e.g. \"body.id is an integer\""]
                }
              ]
            }

            Rules:
            - At most {{{maxCases}}} test cases, roughly half positive and half negative.
            - Negative cases: missing required fields, wrong types, invalid enum values, boundary values,
              nonexistent resources (404), malformed bodies, and missing/invalid auth when the API declares security.
            - Paths are relative to the base URL and use concrete example values, never {placeholders}.
            - Put query parameters in "queryParams", not in "path".
            - expectedStatus is the single most likely status code.
            - When auth is needed use the header "Authorization": "Bearer {{authToken}}" literally; never invent tokens.
            {{{sourceRules.TrimEnd()}}}

            Input:
            {{{input}}}
            """;
    }

    public static string PytestUser(string testCasesJson, string baseUrl) =>
        $$$"""
        Write a pytest module that implements every test case in the JSON below.

        Requirements:
        - Use only pytest and the requests library.
        - Read the base URL from the API_BASE_URL environment variable, defaulting to "{{{baseUrl}}}".
        - Wherever a test case uses "{{authToken}}", read the token from the API_AUTH_TOKEN environment variable instead.
        - Provide a module-scoped `session` fixture (requests.Session) and a `url(path)` helper.
        - One test function per test case, named test_<id>_<short_snake_case_title> (e.g. test_tc_001_create_pet),
          with the test case id and title in its docstring.
        - Assert the expected status code first, then turn each listed assertion into concrete asserts.
        - Pass timeout=10 to every request.

        Test cases:
        {{{testCasesJson}}}
        """;
}
