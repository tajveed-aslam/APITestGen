# Progress — APITestGen
_Last updated: 2026-10-07 by Claude Code_

## Goal
Web app that turns an OpenAPI/Swagger JSON spec or a sample API response into positive + negative test cases,
a Postman collection and a pytest module, with per-user saved history.
Stack: ASP.NET Core 8 Web API, React + TypeScript (Vite), PostgreSQL via EF Core, JWT auth, Gemini (OpenAI optional).

## Done
- Backend (`backend/ApiTestGen.Api`): JWT register/login/me, generations CRUD (`/api/generations`),
  EF Core model + `InitialCreate` migration, ProblemDetails error handling, Swagger in Development.
- LLM layer: `ILlmClient` with `GeminiClient` (default) and `OpenAiChatClient`, chosen by `Llm:Provider`.
  Shared retry with backoff on 429/5xx in `Services/Llm/LlmHttp.cs`.
- Pipeline (`Services/TestGenerationService.cs`): LLM → structured test cases (validated by `TestCaseParser`) →
  Postman v2.1 built deterministically (`PostmanCollectionBuilder`) + pytest written by the LLM from the same cases.
- Tests (`backend/ApiTestGen.Tests`): 32 offline unit tests + 1 live Gemini smoke test (`--filter Category=Live`,
  needs `GEMINI_API_KEY` env var). All passing.
- GitHub repo created: https://github.com/tajveed-aslam/APITestGen (remote `origin`).

## In progress
- Frontend (`frontend/`, Vite React-TS) — not started yet.

## Next steps
1. Scaffold `frontend/` (Vite react-ts, react-router-dom), Vite proxy `/api` → `http://localhost:5080`.
2. Pages: login/register; workspace with generator form (input type toggle, base URL, title, "load sample"),
   results tabs (Test cases table / Postman / pytest) with copy + download; history sidebar with delete.
3. README with setup (PostgreSQL, `appsettings.Development.json`), screenshots.
4. Run end to end — PostgreSQL is NOT installed on the owner's PC yet (no Docker either); ask before installing.
5. Add APITestGen to `tajveed-portfolio/components/Projects.tsx` (Rule 1), commit + push.

## Decisions & gotchas
- Only .NET 10 SDK/runtime installed: projects target `net8.0` with `<RollForward>Major</RollForward>`;
  `dotnet-ef` 8 local tool has `rollForward: true` in `backend/dotnet-tools.json`.
- Secrets live in `backend/ApiTestGen.Api/appsettings.Development.json` (gitignored; template in
  `appsettings.Development.example.json`). Never commit it.
- Gemini model default is `gemini-flash-lite-latest`: on 2026-10-07 `gemini-flash-latest` was overloaded
  (503s / multi-minute responses) while flash-lite answered in ~4 s. `thinkingBudget: 0` and
  `thinkingLevel: "minimal"` are rejected by the flash model — don't add them.
- Postman collection is built in code, not by the LLM, so it always imports cleanly and matches the cases.
- Test cases use `Bearer {{authToken}}` literally; Postman has an `authToken` variable, pytest reads `API_AUTH_TOKEN`.
