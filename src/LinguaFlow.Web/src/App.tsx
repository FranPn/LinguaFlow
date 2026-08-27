import { useState } from 'react'
import './App.css'
import { ApiError, startSession } from './api/client'
import { ChatScreen } from './components/ChatScreen'
import { ProgressScreen } from './components/ProgressScreen'
import { SetupScreen } from './components/SetupScreen'

type View = 'setup' | 'chat' | 'progress'

interface Session {
  id: string
  targetLanguage: string
  topic: string
}

function App() {
  const [view, setView] = useState<View>('setup')
  const [session, setSession] = useState<Session | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleStart(targetLanguage: string, topic: string) {
    setBusy(true)
    setError(null)
    try {
      const { sessionId } = await startSession(targetLanguage, topic)
      setSession({ id: sessionId, targetLanguage, topic })
      setView('chat')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Impossibile contattare il server.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="app">
      {view === 'setup' && <SetupScreen onStart={handleStart} busy={busy} error={error} />}
      {view === 'chat' && session && (
        <ChatScreen
          sessionId={session.id}
          targetLanguage={session.targetLanguage}
          topic={session.topic}
          onShowProgress={() => setView('progress')}
        />
      )}
      {view === 'progress' && <ProgressScreen onBack={() => setView('chat')} />}
    </div>
  )
}

export default App
