import { useState } from 'react'

const LANGUAGES = [
  { code: 'english', label: 'Inglese' },
  { code: 'swedish', label: 'Svedese' },
]

interface Props {
  onStart: (targetLanguage: string, topic: string) => void
  busy: boolean
  error: string | null
}

export function SetupScreen({ onStart, busy, error }: Props) {
  const [targetLanguage, setTargetLanguage] = useState(LANGUAGES[0].code)
  const [topic, setTopic] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const trimmedTopic = topic.trim() || 'conversazione libera'
    onStart(targetLanguage, trimmedTopic)
  }

  return (
    <form className="setup-screen" onSubmit={handleSubmit}>
      <h1>LinguaFlow</h1>
      <p className="subtitle">Pratica conversazionale con correzioni in tempo reale</p>

      <label>
        Lingua target
        <select value={targetLanguage} onChange={(e) => setTargetLanguage(e.target.value)}>
          {LANGUAGES.map((lang) => (
            <option key={lang.code} value={lang.code}>
              {lang.label}
            </option>
          ))}
        </select>
      </label>

      <label>
        Argomento (opzionale)
        <input
          type="text"
          placeholder="es. viaggi, lavoro, hobby..."
          value={topic}
          onChange={(e) => setTopic(e.target.value)}
        />
      </label>

      {error && <p className="error">{error}</p>}

      <button type="submit" disabled={busy}>
        {busy ? 'Avvio…' : 'Inizia conversazione'}
      </button>
    </form>
  )
}
