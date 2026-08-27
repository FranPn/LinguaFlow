import type { ProgressSummary, StartSessionResponse, TurnResponse } from './types'

// Gli endpoint del backend leggono targetLanguage/topic/message come query string
// (minimal API binda automaticamente i parametri string da query, non da body).
const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7073'

class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, init)
  if (!response.ok) {
    const body = await response.text().catch(() => '')
    throw new ApiError(response.status, body || response.statusText)
  }
  return response.json() as Promise<T>
}

export function startSession(targetLanguage: string, topic: string): Promise<StartSessionResponse> {
  const params = new URLSearchParams({ targetLanguage, topic })
  return request(`/api/session/start?${params}`, { method: 'POST' })
}

export function sendTurn(sessionId: string, message: string): Promise<TurnResponse> {
  const params = new URLSearchParams({ message })
  return request(`/api/session/${sessionId}/turn?${params}`, { method: 'POST' })
}

export function getProgress(targetLanguage?: string): Promise<ProgressSummary> {
  const params = targetLanguage ? `?${new URLSearchParams({ targetLanguage })}` : ''
  return request(`/api/progress${params}`)
}

export { ApiError }
