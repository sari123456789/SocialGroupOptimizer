import {
  authFetch,
  saveAuthSession,
  type AuthSession,
} from './apiClient'

export interface LoginRequest {
  managerName: string
  password: string
}

export class AuthError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'AuthError'
  }
}

export async function login(request: LoginRequest): Promise<AuthSession> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (response.status === 401) {
    throw new AuthError('שם מנהל או סיסמה שגויים.')
  }

  if (!response.ok) {
    throw new AuthError('לא ניתן להתחבר לשרת.')
  }

  const data = (await response.json()) as AuthSession
  saveAuthSession(data)
  return data
}

export async function verifySession(): Promise<boolean> {
  const response = await authFetch('/api/assignments')
  return response.ok
}
