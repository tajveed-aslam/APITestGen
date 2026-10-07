import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { GenerationSummary } from '../api'
import { formatDate } from '../utils'

interface Props {
  items: GenerationSummary[] | null
  error: string | null
  activeId?: string
  onDelete: (id: string) => Promise<void>
}

export default function HistoryList({ items, error, activeId, onDelete }: Props) {
  const [deleting, setDeleting] = useState<string | null>(null)

  async function remove(id: string, title: string) {
    if (!window.confirm(`Delete "${title}" from your history?`)) return
    setDeleting(id)
    try {
      await onDelete(id)
    } finally {
      setDeleting(null)
    }
  }

  return (
    <section className="history" aria-label="Generation history">
      <h2 className="history-heading">History</h2>
      {error && <p className="error-text small">{error}</p>}
      {!error && items === null && <p className="muted small"><span className="spinner" /> Loading…</p>}
      {items?.length === 0 && <p className="muted small">Your generations will appear here.</p>}
      <ul>
        {items?.map((g) => (
          <li key={g.id} className={g.id === activeId ? 'active' : ''}>
            <Link to={`/app/g/${g.id}`} className="history-item">
              <span className="history-title">{g.title}</span>
              <span className="history-meta">
                <span className="count-pos">{g.positiveCount}+</span>
                <span className="count-neg">{g.negativeCount}−</span>
                <span>· {g.inputType === 'openApi' ? 'Spec' : 'Sample'}</span>
                <span>· {formatDate(g.createdAt)}</span>
              </span>
            </Link>
            <button
              className="icon-button history-delete"
              aria-label={`Delete ${g.title}`}
              disabled={deleting === g.id}
              onClick={() => void remove(g.id, g.title)}
            >
              ×
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
