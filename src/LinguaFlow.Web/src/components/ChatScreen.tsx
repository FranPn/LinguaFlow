import { useEffect, useRef, useState } from 'react'
import { ApiError, sendTurn } from '../api/client'
import type { ChatMessage } from '../api/types'
import { CorrectionsList } from './CorrectionsList'

interface Props {
  sessionId: string
  targetLanguage: string
  topic: string
  onShowProgress: () => void
}

export function ChatScreen({ sessionId, targetLanguage, topic, onShowProgress }: Props) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  async function handleSend(e: React.FormEvent) {
    e.preventDefault()
    const text = input.trim()
    if (!text || sending) return

    const userMessage: ChatMessage = { id: crypto.randomUUID(), role: 'user', text }
    setMessages((prev) => [...prev, userMessage])
    setInput('')
    setSending(true)
    setError(null)

    try {
      const result = await sendTurn(sessionId, text)
      setMessages((prev) => [
        ...prev.map((m) => (m.id === userMessage.id ? { ...m, corrections: result.corrections } : m)),
        { id: crypto.randomUUID(), role: 'assistant', text: result.reply },
      ])
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Errore di rete, riprova.')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="chat-screen">
      <header className="chat-header">
        <div>
          <strong>{targetLanguage}</strong>
          <span className="topic"> · {topic}</span>
        </div>
        <button type="button" className="link-button" onClick={onShowProgress}>
          Progressi
        </button>
      </header>

      <div className="messages">
        {messages.map((m) => (
          <div key={m.id} className={`message message-${m.role}`}>
            <p>{m.text}</p>
            {m.corrections && <CorrectionsList corrections={m.corrections} />}
          </div>
        ))}
        {sending && <div className="message message-assistant message-pending">…</div>}
        <div ref={bottomRef} />
      </div>

      {error && <p className="error">{error}</p>}

      <form className="composer" onSubmit={handleSend}>
        <button
          type="button"
          className="mic-button"
          disabled
          title="Input vocale — in arrivo (Azure Speech, vedi BRIEF.md punto 3)"
        >
          🎤
        </button>
        <input
          type="text"
          placeholder="Scrivi un messaggio…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={sending}
          autoFocus
        />
        <button type="submit" disabled={sending || !input.trim()}>
          Invia
        </button>
      </form>
    </div>
  )
}
