import { useEffect, useState, type FormEvent } from 'react'
import { api, ApiError, type Generation, type InputType } from '../api'
import { samples } from '../samples'

const MAX_CHARS = 200_000

const modes: { value: InputType; label: string; placeholder: string }[] = [
  {
    value: 'openApi',
    label: 'OpenAPI / Swagger JSON',
    placeholder: '{\n  "openapi": "3.0.0",\n  "info": { ... },\n  "paths": { ... }\n}',
  },
  {
    value: 'sampleResponse',
    label: 'Sample API response',
    placeholder: '{\n  "id": 42,\n  "name": "...",\n  ...\n}',
  },
]

const progressMessages = [
  'Reading your input…',
  'Designing positive and negative test cases…',
  'Building the Postman collection…',
  'Writing the pytest suite…',
  'Almost there…',
]

interface Props {
  isGuest: boolean
  onCreated: (generation: Generation) => void
}

export default function GeneratorForm({ isGuest, onCreated }: Props) {
  const [inputType, setInputType] = useState<InputType>('openApi')
  const [input, setInput] = useState('')
  const [baseUrl, setBaseUrl] = useState('')
  const [title, setTitle] = useState('')
  const [busy, setBusy] = useState(false)
  const [progress, setProgress] = useState(0)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!busy) return
    const timer = window.setInterval(() => setProgress((p) => Math.min(p + 1, progressMessages.length - 1)), 4000)
    return () => window.clearInterval(timer)
  }, [busy])

  const mode = modes.find((m) => m.value === inputType)!
  const tooLong = input.length > MAX_CHARS

  function loadSample() {
    const sample = samples[inputType]
    setInput(sample.input)
    setBaseUrl(sample.baseUrl)
    setError(null)
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setProgress(0)
    setBusy(true)
    setError(null)
    try {
      const generation = await api.createGeneration({
        inputType,
        input,
        baseUrl: baseUrl.trim() || undefined,
        title: title.trim() || undefined,
      })
      onCreated(generation)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Generation failed. Please try again.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="generator card" onSubmit={onSubmit}>
      <div className="generator-header">
        <div>
          <h1>New generation</h1>
          <p className="muted">Paste a spec or a response, then generate test cases, a Postman collection and pytest code.</p>
        </div>
      </div>

      <div className="segmented" role="radiogroup" aria-label="Input type">
        {modes.map((m) => (
          <button
            key={m.value}
            type="button"
            role="radio"
            aria-checked={inputType === m.value}
            className={inputType === m.value ? 'selected' : ''}
            onClick={() => setInputType(m.value)}
            disabled={busy}
          >
            {m.label}
          </button>
        ))}
      </div>

      <div className="field">
        <div className="field-label-row">
          <label htmlFor="spec-input">{inputType === 'openApi' ? 'Spec (JSON)' : 'Response body'}</label>
          <button type="button" className="link-button small" onClick={loadSample} disabled={busy}>
            Load sample: {samples[inputType].label}
          </button>
        </div>
        <textarea
          id="spec-input"
          className="code-input"
          spellCheck={false}
          value={input}
          placeholder={mode.placeholder}
          onChange={(e) => setInput(e.target.value)}
          disabled={busy}
          required
        />
        <span className={`field-hint ${tooLong ? 'error-text' : ''}`}>
          {input.length.toLocaleString()} / {MAX_CHARS.toLocaleString()} characters
          {inputType === 'openApi' && ' · YAML specs: convert to JSON first'}
        </span>
      </div>

      <div className="field-row">
        <label className="field">
          Base URL <span className="muted">(optional)</span>
          <input type="url" placeholder="https://api.example.com" value={baseUrl} onChange={(e) => setBaseUrl(e.target.value)} disabled={busy} />
        </label>
        <label className="field">
          Title <span className="muted">(optional)</span>
          <input maxLength={200} placeholder="Named automatically if empty" value={title} onChange={(e) => setTitle(e.target.value)} disabled={busy} />
        </label>
      </div>

      {error && <p className="error-text" role="alert">{error}</p>}

      <div className="generator-actions">
        <button className="btn btn-primary btn-lg" disabled={busy || !input.trim() || tooLong}>
          {busy ? <><span className="spinner" /> Generating…</> : 'Generate tests'}
        </button>
        {busy && <span className="muted">{progressMessages[progress]}</span>}
        {!busy && isGuest && <span className="muted small">Guest demo: up to 5 generations per hour.</span>}
      </div>
    </form>
  )
}
