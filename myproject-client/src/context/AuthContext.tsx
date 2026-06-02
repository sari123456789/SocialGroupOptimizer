import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  AUTH_SESSION_EXPIRED_EVENT,
  clearAuthSession,
  loadAuthSession,
  type AuthSession,
} from '../services/apiClient'
import { login as loginRequest, verifySession, type LoginRequest } from '../services/authService'

interface AuthContextValue {
  session: AuthSession | null
  isAuthenticated: boolean
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() =>
    loadAuthSession(),
  )

  const handleLogin = useCallback(async (request: LoginRequest) => {
    const newSession = await loginRequest(request)
    setSession(newSession)
  }, [])

  const logout = useCallback(() => {
    clearAuthSession()
    setSession(null)
  }, [])

  useEffect(() => {
    const handleExpired = () => setSession(null)
    window.addEventListener(AUTH_SESSION_EXPIRED_EVENT, handleExpired)
    return () => window.removeEventListener(AUTH_SESSION_EXPIRED_EVENT, handleExpired)
  }, [])

  useEffect(() => {
    if (!session) {
      return
    }

    void verifySession().then((isValid) => {
      if (!isValid) {
        logout()
      }
    })
  }, [session, logout])

  const value = useMemo(
    () => ({
      session,
      isAuthenticated: session !== null,
      login: handleLogin,
      logout,
    }),
    [session, handleLogin, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}
