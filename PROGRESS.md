# Progress — APITestGen
_Last updated: 2026-10-07 by Claude Code_

## Goal
Web app that turns an OpenAPI/Swagger JSON spec or a sample API response into positive + negative test cases,
a Postman collection and a pytest module, with per-user saved history and a public live demo (guest mode).
Stack: ASP.NET Core 8 Web API, React + TypeScript (Vite), PostgreSQL via EF Core, JWT auth, Gemini (OpenAI optional).

## Done
- Backend (`backend/ApiTestGen.Api`): JWT register/login/guest/me, generations CRUD (`/api/generations`),
  EF Core migrations (`InitialCreate`, `AddGuestUsers`), ProblemDetails errors, Swagger, `/api/health`.
- LLM layer: `ILlmClient` → `GeminiClient` (default) / `OpenAiChatClient`, via `Llm:Provider`; retry/backoff in `LlmHttp`.
- Pipeline: LLM → structured cases (`TestCaseParser`) → Postman built in C# (`PostmanCollectionBuilder`) + pytest by LLM.
- Demo guardrails: guest accounts (`POST /api/auth/guest`, 2 h tokens), rate limits in `Infrastructure/RateLimiting.cs`
  (per-IP guest sessions, per-user generations; guests stricter). Settings under `Demo:*`.
- Deploy config: `backend/Dockerfile`, `render.yaml` (Render), `frontend/vercel.json` (Vercel SPA rewrites);
  `ConnectionStrings.NormalizePostgres` accepts `postgresql://` URLs from Neon/Render.
- Frontend (`frontend/`): landing page with "Try the live demo" + server wake-up indicator, login/register,
  workspace (generator form with samples, results tabs with copy/download, history sidebar, mobile drawer).
  `npm run build` and `npm run lint` clean.
- Tests: 37 offline xUnit tests passing + live Gemini smoke test (`--filter Category=Live`) passing.
- README with usage, local setup, API reference and deployment steps.
- Portfolio: APITestGen card added to `tajveed-portfolio/components/Projects.tsx` and committed locally
  (commit `Add APITestGen to projects`) but **NOT pushed** — `demo: null` until the live URL exists.

- Neon database (AWS eu-central-1 / Frankfurt) connected via `appsettings.Development.json`; both migrations applied.
  Scripted API end-to-end run against it: 24/24 checks pass (guest, both input types, history, per-user isolation,
  validation 400s, 401/404/409, delete, guest-session 429). Dev server + proxy verified.

## In progress
- Owner clicking through the UI locally (http://localhost:5173). No browser tool was available to Claude, so the
  UI itself hasn't been visually checked by an agent yet.

## Next steps
1. Fix anything the owner finds in the UI.
2. Deploy: Render (Blueprint from `render.yaml`; env vars `ConnectionStrings__Default`, `Gemini__ApiKey`,
   `Cors__Origins__0`) then Vercel (root `frontend`, `VITE_API_BASE_URL` = Render URL). Owner does account steps.
3. Set the live URL in README ("Live demo") and in Projects.tsx `demo:`, take a screenshot for
   `tajveed-portfolio/public/screenshots/`, then push both repos.

## Decisions & gotchas
- Only .NET 10 SDK/runtime installed: projects target `net8.0` with `<RollForward>Major</RollForward>`;
  `dotnet-ef` 8 local tool has `rollForward: true` in `backend/dotnet-tools.json`.
- Secrets live only in `backend/ApiTestGen.Api/appsettings.Development.json` (gitignored). Never commit it.
- Gemini default model is `gemini-flash-lite-latest`: on 2026-10-07 `gemini-flash-latest` was overloaded
  (503s / multi-minute responses) while flash-lite answered in ~4 s. `thinkingBudget: 0` and
  `thinkingLevel: "minimal"` are rejected by the flash model — don't add them.
- Postman collection is built in code, not by the LLM, so it always imports cleanly and matches the cases.
- Test cases use `Bearer {{authToken}}` literally; Postman has an `authToken` variable, pytest reads `API_AUTH_TOKEN`.
- Windows PowerShell 5.1 `Get-Content`/`Set-Content` mangle UTF-8 files (emoji, em dashes) — edit files with the
  editor tool, not PowerShell string replacement.
