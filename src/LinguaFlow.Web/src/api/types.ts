// Rispecchia i record C# in LinguaFlow.Core (serializzazione JSON camelCase di default).

export interface Correction {
  id: string
  original: string
  corrected: string
  category: string
  explanation: string
}

export interface TurnResponse {
  reply: string
  corrections: Correction[]
}

export interface StartSessionResponse {
  sessionId: string
}

export interface CategoryCount {
  category: string
  count: number
}

export interface SessionProgress {
  sessionId: string
  startedAt: string
  topic: string
  correctionCount: number
}

export interface ProgressSummary {
  totalSessions: number
  totalTurns: number
  totalCorrections: number
  byCategory: CategoryCount[]
  sessionTrend: SessionProgress[]
}

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  text: string
  corrections?: Correction[]
}
