import { useEffect, useState } from 'react'
import { ApiError, getProgress } from '../api/client'
import type { ProgressSummary } from '../api/types'

export function ProgressScreen({ onBack }: { onBack: () => void }) {
  const [summary, setSummary] = useState<ProgressSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getProgress()
      .then(setSummary)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Errore di rete'))
  }, [])

  return (
    <div className="progress-screen">
      <header className="chat-header">
        <strong>Progressi</strong>
        <button type="button" className="link-button" onClick={onBack}>
          Chiudi
        </button>
      </header>

      {error && <p className="error">{error}</p>}
      {!summary && !error && <p>Caricamento…</p>}

      {summary && (
        <>
          <div className="stats-grid">
            <div className="stat">
              <span className="stat-value">{summary.totalSessions}</span>
              <span className="stat-label">Sessioni</span>
            </div>
            <div className="stat">
              <span className="stat-value">{summary.totalTurns}</span>
              <span className="stat-label">Turni</span>
            </div>
            <div className="stat">
              <span className="stat-value">{summary.totalCorrections}</span>
              <span className="stat-label">Correzioni</span>
            </div>
          </div>

          <h2>Errori per categoria</h2>
          {summary.byCategory.length === 0 ? (
            <p className="empty">Nessun dato ancora.</p>
          ) : (
            <ul className="category-list">
              {summary.byCategory.map((c) => (
                <li key={c.category}>
                  <span>{c.category}</span>
                  <span>{c.count}</span>
                </li>
              ))}
            </ul>
          )}

          <h2>Andamento sessioni</h2>
          {summary.sessionTrend.length === 0 ? (
            <p className="empty">Nessuna sessione ancora.</p>
          ) : (
            <ul className="trend-list">
              {summary.sessionTrend.map((s) => (
                <li key={s.sessionId}>
                  <span>{new Date(s.startedAt).toLocaleDateString()}</span>
                  <span>{s.topic}</span>
                  <span>{s.correctionCount} correzioni</span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  )
}
