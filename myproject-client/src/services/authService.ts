import {
  authFetch,
  saveAuthSession,
  type AuthSession,
} from './apiClient'

export interface LoginRequest {
  email: string
  password: string
}

export type RegisterRequest = LoginRequest

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
    throw new AuthError('כתובת מייל או סיסמה שגויים.')
  }

  if (!response.ok) {
    throw new AuthError('לא ניתן להתחבר לשרת.')
  }

  const data = (await response.json()) as AuthSession
  saveAuthSession(data)
  return data
}

export async function register(request: RegisterRequest): Promise<AuthSession> {
  const response = await fetch('/api/auth/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (response.status === 400) {
    const data = (await response.json().catch(() => null)) as { message?: string } | null
    throw new AuthError(data?.message ?? 'לא ניתן ליצור מנהל חדש.')
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
