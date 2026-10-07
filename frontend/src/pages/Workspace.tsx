import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError, type Generation, type GenerationSummary } from '../api'
import { useAuth } from '../auth-context'
import Brand from '../components/Brand'
import GeneratorForm from '../components/GeneratorForm'
import HistoryList from '../components/HistoryList'
import ResultsView from '../components/ResultsView'

interface Loaded {
  id: string
  generation: Generation | null
  error: string | null
}

export default function Workspace() {
  const { id } = useParams()
  const { session, logout } = useAuth()
  const navigate = useNavigate()

  const [history, setHistory] = useState<GenerationSummary[] | null>(null)
  const [historyError, setHistoryError] = useState<string | null>(null)
  // The last generation fetched (or just created), keyed by id; what's shown is derived from the route id.
  const [loaded, setLoaded] = useState<Loaded | null>(null)
  const [sidebarOpen, setSidebarOpen] = useState(false)

  const current = id && loaded?.id === id ? loaded.generation : null
  const loadError = id && loaded?.id === id ? loaded.error : null

  useEffect(() => {
    api.listGenerations()
      .then(setHistory)
      .catch((e) => setHistoryError(e instanceof ApiError ? e.message : 'Could not load history.'))
  }, [])

  const loadedId = loaded?.id
  useEffect(() => {
    if (!id || loadedId === id) return
    let cancelled = false
    api.getGeneration(id)
      .then((generation) => !cancelled && setLoaded({ id, generation, error: null }))
      .catch((e) => !cancelled && setLoaded({
        id,
        generation: null,
        error: e instanceof ApiError && e.status === 404 ? 'This generation no longer exists.' : 'Could not load this generation.',
      }))
    return () => { cancelled = true }
  }, [id, loadedId])

  const onCreated = useCallback((generation: Generation) => {
    setLoaded({ id: generation.id, generation, error: null })
    setHistory((h) => [
      {
        id: generation.id,
        title: generation.title,
        inputType: generation.inputType,
        positiveCount: generation.testCases.filter((t) => t.category === 'positive').length,
        negativeCount: generation.testCases.filter((t) => t.category === 'negative').length,
        createdAt: generation.createdAt,
      },
      ...(h ?? []),
    ])
    navigate(`/app/g/${generation.id}`)
  }, [navigate])

  const onDelete = useCallback(async (deleteId: string) => {
    await api.deleteGeneration(deleteId)
    setHistory((h) => h?.filter((g) => g.id !== deleteId) ?? null)
    if (deleteId === id) navigate('/app')
  }, [id, navigate])

  function signOut() {
    logout()
    navigate('/')
  }

  return (
    <div className="workspace">
      <header className="topbar topbar-app">
        <div className="topbar-left">
          <button className="icon-button mobile-only" aria-label="Show history" onClick={() => setSidebarOpen((o) => !o)}>
            ☰
          </button>
          <Brand to="/app" />
        </div>
        <div className="topbar-actions">
          {session?.isGuest ? (
            <span className="badge badge-guest" title="Guest sessions expire after two hours">Guest demo</span>
          ) : (
            <span className="muted small hide-mobile">{session?.email}</span>
          )}
          {session?.isGuest && <Link to="/login" className="btn btn-ghost btn-sm hide-mobile">Create account</Link>}
          <button className="btn btn-ghost btn-sm" onClick={signOut}>Sign out</button>
        </div>
      </header>

      <div className="workspace-body">
        <aside
          className={`sidebar ${sidebarOpen ? 'open' : ''}`}
          // On mobile the sidebar is a drawer: close it once a link inside is followed.
          onClick={(e) => (e.target as HTMLElement).closest('a') && setSidebarOpen(false)}
        >
          <Link to="/app" className="btn btn-primary btn-block">+ New generation</Link>
          <HistoryList items={history} error={historyError} activeId={id} onDelete={onDelete} />
        </aside>
        {sidebarOpen && <div className="scrim mobile-only" onClick={() => setSidebarOpen(false)} />}

        <main className="workspace-main">
          {!id && <GeneratorForm isGuest={!!session?.isGuest} onCreated={onCreated} />}
          {id && loadError && (
            <div className="empty-state card">
              <p>{loadError}</p>
              <Link to="/app" className="btn btn-secondary">Start a new generation</Link>
            </div>
          )}
          {id && !loadError && !current && <div className="loading-block"><span className="spinner" /> Loading…</div>}
          {id && current && <ResultsView generation={current} onDelete={() => onDelete(current.id)} />}
        </main>
      </div>
    </div>
  )
}
