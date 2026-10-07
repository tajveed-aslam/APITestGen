import { useState } from 'react'
import type { TestCase, TestCategory } from '../api'
import { pretty } from '../utils'

type Filter = 'all' | TestCategory

export default function TestCaseList({ testCases }: { testCases: TestCase[] }) {
  const [filter, setFilter] = useState<Filter>('all')
  const [open, setOpen] = useState<Set<string>>(() => new Set())

  const visible = testCases.filter((t) => filter === 'all' || t.category === filter)

  function toggle(id: string) {
    setOpen((current) => {
      const next = new Set(current)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  return (
    <div className="case-list">
      <div className="case-filters">
        {(['all', 'positive', 'negative'] as const).map((f) => (
          <button key={f} className={`chip-button ${filter === f ? 'selected' : ''}`} onClick={() => setFilter(f)}>
            {f === 'all' ? `All (${testCases.length})` : `${f[0].toUpperCase()}${f.slice(1)} (${testCases.filter((t) => t.category === f).length})`}
          </button>
        ))}
        <button className="link-button small" onClick={() => setOpen(open.size ? new Set() : new Set(visible.map((t) => t.id)))}>
          {open.size ? 'Collapse all' : 'Expand all'}
        </button>
      </div>

      <ul>
        {visible.map((tc) => {
          const isOpen = open.has(tc.id)
          return (
            <li key={tc.id} className={`case ${tc.category}`}>
              <button className="case-summary" aria-expanded={isOpen} onClick={() => toggle(tc.id)}>
                <span className="case-id mono">{tc.id}</span>
                <span className={`method method-${tc.method.toLowerCase()}`}>{tc.method}</span>
                <span className="case-path mono">{tc.path}</span>
                <span className="case-title">{tc.title}</span>
                <span className={`status status-${Math.floor(tc.expectedStatus / 100)}xx`}>{tc.expectedStatus}</span>
              </button>
              {isOpen && <CaseDetails tc={tc} />}
            </li>
          )
        })}
      </ul>
    </div>
  )
}

function CaseDetails({ tc }: { tc: TestCase }) {
  const hasHeaders = Object.keys(tc.headers).length > 0
  const hasQuery = Object.keys(tc.queryParams).length > 0

  return (
    <div className="case-details">
      {tc.description && <p>{tc.description}</p>}
      <dl>
        {hasQuery && (
          <>
            <dt>Query</dt>
            <dd className="mono">{Object.entries(tc.queryParams).map(([k, v]) => `${k}=${v}`).join('&')}</dd>
          </>
        )}
        {hasHeaders && (
          <>
            <dt>Headers</dt>
            <dd className="mono">
              {Object.entries(tc.headers).map(([k, v]) => <div key={k}>{k}: {v}</div>)}
            </dd>
          </>
        )}
        {tc.body != null && (
          <>
            <dt>Body</dt>
            <dd><pre className="code-block small">{typeof tc.body === 'string' ? tc.body : pretty(tc.body)}</pre></dd>
          </>
        )}
        <dt>Expect</dt>
        <dd>
          <strong>HTTP {tc.expectedStatus}</strong>
          {tc.assertions.length > 0 && (
            <ul className="assertions">
              {tc.assertions.map((a) => <li key={a}>{a}</li>)}
            </ul>
          )}
        </dd>
      </dl>
    </div>
  )
}
