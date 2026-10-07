import { useMemo, useState } from 'react'
import type { Generation } from '../api'
import { formatDate, pretty, slugify } from '../utils'
import CodePanel from './CodePanel'
import TestCaseList from './TestCaseList'

type Tab = 'cases' | 'postman' | 'pytest' | 'input'

interface Props {
  generation: Generation
  onDelete: () => Promise<void>
}

export default function ResultsView({ generation, onDelete }: Props) {
  const [tab, setTab] = useState<Tab>('cases')
  const [deleting, setDeleting] = useState(false)

  const slug = slugify(generation.title)
  const casesJson = useMemo(() => pretty(generation.testCases), [generation])
  const postmanJson = useMemo(() => pretty(generation.postmanCollection), [generation])
  const positive = generation.testCases.filter((t) => t.category === 'positive').length
  const negative = generation.testCases.length - positive

  const tabs: { id: Tab; label: string }[] = [
    { id: 'cases', label: `Test cases (${generation.testCases.length})` },
    { id: 'postman', label: 'Postman collection' },
    { id: 'pytest', label: 'pytest' },
    { id: 'input', label: 'Input' },
  ]

  async function remove() {
    if (!window.confirm(`Delete "${generation.title}" from your history?`)) return
    setDeleting(true)
    try {
      await onDelete()
    } finally {
      setDeleting(false)
    }
  }

  return (
    <section className="results">
      <header className="results-header">
        <div>
          <h1>{generation.title}</h1>
          <p className="results-meta">
            <span className="pill pill-pos">{positive} positive</span>
            <span className="pill pill-neg">{negative} negative</span>
            <span>{generation.inputType === 'openApi' ? 'OpenAPI spec' : 'Sample response'}</span>
            {generation.baseUrl && <span className="mono">{generation.baseUrl}</span>}
            <span>{formatDate(generation.createdAt)}</span>
            <span>
              {generation.model} · {(generation.durationMs / 1000).toFixed(1)} s
            </span>
          </p>
        </div>
        <button className="btn btn-ghost btn-sm" onClick={() => void remove()} disabled={deleting}>
          Delete
        </button>
      </header>

      <div className="tabs" role="tablist">
        {tabs.map((t) => (
          <button
            key={t.id}
            role="tab"
            aria-selected={tab === t.id}
            className={tab === t.id ? 'active' : ''}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="tab-panel" role="tabpanel">
        {tab === 'cases' && (
          <CodePanel
            content={casesJson}
            filename={`${slug}-test-cases.json`}
            mimeType="application/json"
            copyLabel="Copy JSON"
          >
            <TestCaseList testCases={generation.testCases} />
          </CodePanel>
        )}
        {tab === 'postman' && (
          <CodePanel
            content={postmanJson}
            filename={`${slug}.postman_collection.json`}
            mimeType="application/json"
            note="Import into Postman (File → Import), then set the baseUrl and authToken collection variables."
          />
        )}
        {tab === 'pytest' && (
          <CodePanel
            content={generation.pytestCode}
            filename={`test_${slugify(generation.title, '_')}.py`}
            mimeType="text/x-python"
            note="Run with: pip install pytest requests, then API_BASE_URL=… API_AUTH_TOKEN=… pytest -v"
          />
        )}
        {tab === 'input' && (
          <CodePanel
            content={generation.input}
            filename={`${slug}-input.json`}
            mimeType="application/json"
          />
        )}
      </div>
    </section>
  )
}
