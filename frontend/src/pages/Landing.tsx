import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth-context'
import Brand from '../components/Brand'
import { useServerWake } from '../components/useServerWake'

const GITHUB_URL = 'https://github.com/tajveed-aslam/APITestGen'

const outputs = [
  {
    icon: '✅',
    title: 'Positive & negative test cases',
    text: 'Happy paths plus missing fields, wrong types, bad enums, boundaries, 404s and auth failures — each with an expected status and concrete assertions.',
  },
  {
    icon: '📮',
    title: 'Postman collection',
    text: 'A v2.1 collection grouped into positive/negative folders, with baseUrl and authToken variables and a status-code test on every request.',
  },
  {
    icon: '🐍',
    title: 'pytest suite',
    text: 'A ready-to-run module using requests, with a shared session fixture, env-based config and one test function per case.',
  },
]

const steps = [
  ['Paste', 'An OpenAPI / Swagger JSON spec, or a sample response from any endpoint.'],
  ['Generate', 'Gemini designs the test cases; the Postman collection and pytest suite are built from the same cases.'],
  ['Use', 'Copy or download each output. Every run is saved to your history.'],
]

const stack = ['ASP.NET Core 8', 'EF Core', 'PostgreSQL', 'JWT auth', 'React', 'TypeScript', 'Vite', 'Gemini API', 'xUnit']

export default function Landing() {
  const { session, startGuest } = useAuth()
  const navigate = useNavigate()
  const server = useServerWake()
  const [starting, setStarting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function tryDemo() {
    if (session) return navigate('/app')
    setStarting(true)
    setError(null)
    try {
      await startGuest()
      navigate('/app')
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Could not start the demo.')
    } finally {
      setStarting(false)
    }
  }

  return (
    <div className="landing">
      <header className="topbar">
        <Brand />
        <nav className="topbar-actions">
          <a href={GITHUB_URL} target="_blank" rel="noreferrer" className="link-muted">
            GitHub
          </a>
          {session ? (
            <Link to="/app" className="btn btn-secondary">Open app</Link>
          ) : (
            <Link to="/login" className="btn btn-secondary">Sign in</Link>
          )}
        </nav>
      </header>

      <main>
        <section className="hero">
          <p className="eyebrow">AI-assisted API testing</p>
          <h1>
            {/* Non-breaking hyphens (U+2011) keep "ready-to-run" on one line. */}
            Turn an API spec into a <span className="accent">ready‑to‑run test suite</span>
          </h1>
          <p className="lead">
            Paste an OpenAPI spec or a sample API response. Get positive and negative test cases, a Postman collection
            and a pytest module, all generated from the same test design.
          </p>
          <div className="hero-actions">
            <button className="btn btn-primary btn-lg" onClick={tryDemo} disabled={starting}>
              {starting ? <><span className="spinner" /> Starting demo…</> : session ? 'Open the app' : 'Try the live demo'}
            </button>
            {!session && <Link to="/login" className="btn btn-ghost btn-lg">Create an account</Link>}
          </div>
          <p className="hint">No sign-up needed for the demo. Guest sessions last two hours.</p>
          {error && <p className="error-text">{error}</p>}
          <ServerStatus state={server} />
        </section>

        <section className="cards">
          {outputs.map((o) => (
            <article key={o.title} className="card feature">
              <span className="feature-icon" aria-hidden>{o.icon}</span>
              <h3>{o.title}</h3>
              <p>{o.text}</p>
            </article>
          ))}
        </section>

        <section className="steps">
          <h2>How it works</h2>
          <ol>
            {steps.map(([title, text], i) => (
              <li key={title}>
                <span className="step-number">{i + 1}</span>
                <div>
                  <strong>{title}</strong>
                  <p>{text}</p>
                </div>
              </li>
            ))}
          </ol>
        </section>

        <section className="stack">
          <h2>Built with</h2>
          <ul className="chips">
            {stack.map((s) => <li key={s} className="chip">{s}</li>)}
          </ul>
        </section>
      </main>

      <footer className="footer">
        <span>Built by Tajveed Aslam</span>
        <a href={GITHUB_URL} target="_blank" rel="noreferrer">Source on GitHub</a>
      </footer>
    </div>
  )
}

function ServerStatus({ state }: { state: ReturnType<typeof useServerWake> }) {
  if (state === 'waking')
    return (
      <p className="server-status">
        <span className="spinner" /> Waking up the demo server — free hosting sleeps when idle, this can take up to a
        minute.
      </p>
    )
  if (state === 'down')
    return <p className="server-status error-text">The demo server isn't responding right now. Please try again later.</p>
  if (state === 'ready') return <p className="server-status ok-text">● Demo server online</p>
  return null
}
