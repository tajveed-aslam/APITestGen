import { useState, type ReactNode } from 'react'
import { copyText, downloadFile } from '../utils'

interface Props {
  content: string
  filename: string
  mimeType: string
  copyLabel?: string
  note?: string
  /** Custom view of the content; defaults to the raw content in a code block. */
  children?: ReactNode
}

export default function CodePanel({ content, filename, mimeType, copyLabel = 'Copy', note, children }: Props) {
  const [copied, setCopied] = useState(false)

  async function copy() {
    if (await copyText(content)) {
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1800)
    }
  }

  return (
    <div className="code-panel">
      <div className="code-toolbar">
        <span className="mono muted small">{filename}</span>
        <div className="code-actions">
          <button className="btn btn-secondary btn-sm" onClick={() => void copy()}>
            {copied ? '✓ Copied' : copyLabel}
          </button>
          <button className="btn btn-secondary btn-sm" onClick={() => downloadFile(filename, content, mimeType)}>
            Download
          </button>
        </div>
      </div>
      {note && <p className="code-note">{note}</p>}
      {children ?? <pre className="code-block"><code>{content}</code></pre>}
    </div>
  )
}
