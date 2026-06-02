const AUTH_STORAGE_KEY = 'myproject.auth'

export interface AuthSession {
  token: string
  managerId: number
  managerName: string
}

export function loadAuthSession(): AuthSession | null {
  const raw = sessionStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as AuthSession
  } catch {
    sessionStorage.removeItem(AUTH_STORAGE_KEY)
    return null
  }
}

export function saveAuthSession(session: AuthSession): void {
  sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
}

export function clearAuthSession(): void {
  sessionStorage.removeItem(AUTH_STORAGE_KEY)
}

export function getAuthToken(): string | null {
  return loadAuthSession()?.token ?? null
}

export class SessionExpiredError extends Error {
  constructor() {
    super('נדרשת התחברות מחדש.')
    this.name = 'SessionExpiredError'
  }
}

export const AUTH_SESSION_EXPIRED_EVENT = 'auth:session-expired'

function notifySessionExpired(): void {
  clearAuthSession()
  window.dispatchEvent(new CustomEvent(AUTH_SESSION_EXPIRED_EVENT))
}

function resolveRequestUrl(input: RequestInfo | URL): RequestInfo | URL {
  if (typeof input !== 'string' || !input.startsWith('/')) {
    return input
  }

  const apiBase = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? ''
  return apiBase ? `${apiBase}${input}` : input
}

export async function authFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<Response> {
  const token = getAuthToken()
  const headers = new Headers(init.headers)

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  try {
    const response = await fetch(resolveRequestUrl(input), {
      ...init,
      headers,
    })

    if (response.status === 401) {
      notifySessionExpired()
      throw new SessionExpiredError()
    }

    return response
  } catch (error) {
    if (error instanceof SessionExpiredError) {
      throw error
    }

    throw new Error('NETWORK_ERROR')
  }
}
