# APITestGen

**Turn an API spec into a ready-to-run test suite.** Paste an OpenAPI/Swagger JSON spec, or just a sample
response from an endpoint, and APITestGen generates:

- ✅ **Positive and negative test cases**: happy paths plus missing fields, wrong types, invalid enums,
  boundary values, 404s and auth failures, each with an expected status code and concrete assertions
- 📮 **A Postman collection** (v2.1) you can import, with `baseUrl` / `authToken` variables and a status-code test
  on every request
- 🐍 **A pytest module** using `requests`, with one test function per case

Every generation is saved to your personal history.

**Live demo:** _coming soon_ · No sign-up needed: click **Try the live demo** for a two-hour guest session.

| | |
|---|---|
| **Backend** | ASP.NET Core 8 Web API · EF Core 8 · PostgreSQL · JWT auth · rate limiting |
| **Frontend** | React 19 · TypeScript · Vite · React Router |
| **AI** | Google Gemini (default) or OpenAI, chosen in config |
| **Tests** | xUnit: 37 offline tests + an opt-in live Gemini smoke test |

---

## Using the app

1. Open the app and click **Try the live demo** (guest) or **Sign in / Create an account** (keeps your history).
2. On **New generation**, pick the input type:
   - **OpenAPI / Swagger JSON**: paste a full spec (OpenAPI 3.x or Swagger 2.0, as JSON; convert YAML to JSON first).
   - **Sample API response**: paste a response body from any endpoint; the API shape is inferred from it.

   No spec handy? Click **Load sample** to fill in a Pet Store spec or a sample user response.
3. Optionally set a **Base URL** (used in the Postman collection and pytest file) and a **Title**.
4. Click **Generate tests**. It usually takes 10–30 seconds.
5. Review the results in four tabs:

   | Tab | What you get | Actions |
   |---|---|---|
   | **Test cases** | Expandable list with method, path, expected status, headers, body and assertions; filter positive/negative | Copy JSON · Download `.json` |
   | **Postman collection** | Collection v2.1 JSON | Copy · Download `.postman_collection.json` |
   | **pytest** | Python test module | Copy · Download `test_<name>.py` |
   | **Input** | The spec/response you pasted | Copy · Download |

6. Every generation appears in the **History** sidebar. Click one to reopen it, or × to delete it.

### Running the generated tests

**Postman:** *File → Import* the downloaded collection, open the collection's **Variables** tab and set
`baseUrl` (and `authToken` if the API needs auth), then run the collection.

**pytest:**

```bash
pip install pytest requests
# macOS/Linux
API_BASE_URL=https://api.example.com API_AUTH_TOKEN=your-token pytest -v test_pet_store.py
# Windows PowerShell
$env:API_BASE_URL="https://api.example.com"; $env:API_AUTH_TOKEN="your-token"; pytest -v test_pet_store.py
```

---

## How it works

```
 paste spec / response
          │
          ▼
 ┌──────────────────┐   1 LLM call (JSON mode)   ┌──────────────────────────┐
 │ InputValidator   │ ─────────────────────────▶ │ TestCaseParser           │
 │ (JSON, paths,    │                            │ validates & normalises:  │
 │  size, base URL) │                            │ method, path, status,    │
 └──────────────────┘                            │ category, sequential ids │
                                                 └────────────┬─────────────┘
                              ┌───────────────────────────────┴───────────────┐
                              ▼                                               ▼
              PostmanCollectionBuilder (C#, deterministic)     1 LLM call → pytest module
                              │                                               │
                              └──────────── saved to PostgreSQL ──────────────┘
```

- The model designs the test cases once, as structured JSON. Both runnable outputs come from **that same
  validated list**, so the test cases, the Postman collection and the pytest file always describe the same tests.
- The Postman collection is **built in code** rather than written by the model, so it always imports cleanly.
- LLM calls retry with backoff on `429`/`5xx`. Errors come back as RFC 7807 problem details with readable messages.
- A public demo needs guardrails: guest sessions are rate limited per IP, and generations per user/guest per hour.

---

## Running it locally

### Prerequisites

- [.NET SDK 8 or newer](https://dotnet.microsoft.com/download). The projects target `net8.0` and roll forward to newer runtimes.
- [Node.js 20+](https://nodejs.org)
- A PostgreSQL database: a free [Neon](https://neon.tech) project, a local PostgreSQL install, or Docker:
  `docker run -d --name apitestgen-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17`
- A [Gemini API key](https://aistudio.google.com/apikey) (or an OpenAI key)

### 1. Configure the backend

```bash
cd backend/ApiTestGen.Api
cp appsettings.Development.example.json appsettings.Development.json   # gitignored
```

Edit `appsettings.Development.json`:

| Setting | Value |
|---|---|
| `ConnectionStrings:Default` | `Host=localhost;Port=5432;Database=apitestgen;Username=postgres;Password=postgres`, or a `postgresql://…` URL from Neon |
| `Jwt:Key` | Any random string of 32+ characters |
| `Gemini:ApiKey` | Your Gemini key |
| `Database:MigrateOnStartup` | `true` creates/updates the tables automatically on start |

To use OpenAI instead, set `Llm:Provider` to `OpenAI` and fill in `OpenAI:ApiKey`.

### 2. Run the API

```bash
cd backend
dotnet run --project ApiTestGen.Api
```

The API listens on **http://localhost:5080**. Swagger UI is at http://localhost:5080/swagger.

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**. In development, Vite proxies `/api` to the API on port 5080.

### 4. Run the tests

```bash
cd backend
dotnet test                                   # 37 offline tests, no API key or database needed

# Optional: a real end-to-end call to Gemini
$env:GEMINI_API_KEY="..."                     # PowerShell (bash: export GEMINI_API_KEY=...)
dotnet test --filter Category=Live
```

---

## API reference

All endpoints except `register`, `login`, `guest` and `health` need `Authorization: Bearer <token>`.

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/auth/register` | `{ email, password }` → `{ token, expiresAt, email, isGuest }` |
| `POST` | `/api/auth/login` | Same shape as register |
| `POST` | `/api/auth/guest` | Creates a temporary guest account (rate limited per IP) |
| `GET` | `/api/auth/me` | Current user |
| `POST` | `/api/generations` | `{ inputType: "openApi" \| "sampleResponse", input, baseUrl?, title? }` → full generation (rate limited) |
| `GET` | `/api/generations` | Your last 100 generations (summaries) |
| `GET` | `/api/generations/{id}` | One generation with test cases, Postman collection and pytest code |
| `DELETE` | `/api/generations/{id}` | Delete from history |
| `GET` | `/api/health` | Liveness check |

---

## Deploying (Vercel + Render + Neon)

**1. Database (Neon).** Create a free project and copy its connection string (`postgresql://…`).

**2. API (Render).** *New → Blueprint* and select this repo. `render.yaml` sets up a Docker web service from
`backend/`. Fill in the secret environment variables:

| Variable | Value |
|---|---|
| `ConnectionStrings__Default` | The Neon connection string |
| `Gemini__ApiKey` | Your Gemini key |
| `Cors__Origins__0` | Your Vercel URL, e.g. `https://apitestgen.vercel.app` |

`Jwt__Key` is generated automatically. Migrations run on startup. Health check: `/api/health`.

**3. Frontend (Vercel).** Import the repo, set **Root Directory** to `frontend` (framework: Vite), and add the
environment variable `VITE_API_BASE_URL` = your Render URL (e.g. `https://apitestgen-api.onrender.com`).
`vercel.json` handles client-side routing.

> Render's free tier sleeps after ~15 minutes idle, so the first request can take up to a minute. The landing page
> pings the API on load and shows a "waking up" message instead of looking broken.

### Tuning the demo limits

| Setting | Default |
|---|---|
| `Demo__GuestAccessEnabled` | `true` |
| `Demo__GuestSessionsPerHourPerIp` | `5` |
| `Demo__GuestGenerationsPerHour` | `5` |
| `Demo__UserGenerationsPerHour` | `30` |
| `Generation__MaxTestCases` | `30` |
| `Gemini__Model` | `gemini-flash-lite-latest` |

---

## Project structure

```
backend/
  ApiTestGen.Api/
    Controllers/        AuthController, GenerationsController
    Services/           TestGenerationService, TestCaseParser, PostmanCollectionBuilder,
                        InputValidator, Prompts, TokenService
    Services/Llm/       ILlmClient, GeminiClient, OpenAiChatClient, retry helper
    Data/               AppDbContext + EF Core migrations
    Infrastructure/     error handling, rate limiting, JSON + connection-string helpers
  ApiTestGen.Tests/     xUnit tests
  Dockerfile
frontend/
  src/pages/            Landing, Login, Workspace
  src/components/       GeneratorForm, ResultsView, TestCaseList, CodePanel, HistoryList
render.yaml             Render blueprint for the API
```

---

Built by [Tajveed Aslam](https://tajveed-portfolio.vercel.app) · [GitHub](https://github.com/tajveed-aslam)
