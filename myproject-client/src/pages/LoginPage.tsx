import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { AuthError } from '../services/authService'

type AuthMode = 'login' | 'register'

export function LoginPage() {
  const { isAuthenticated, login, register } = useAuth()
  const [mode, setMode] = useState<AuthMode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError(null)

    if (mode === 'register' && password !== confirmPassword) {
      setError('הסיסמאות אינן תואמות.')
      setLoading(false)
      return
    }

    try {
      if (mode === 'register') {
        await register({ email, password })
      } else {
        await login({ email, password })
      }
    } catch (submitError) {
      if (submitError instanceof AuthError) {
        setError(submitError.message)
      } else {
        setError('אירעה שגיאה בלתי צפויה.')
      }
    } finally {
      setLoading(false)
    }
  }

  function switchMode(nextMode: AuthMode) {
    setMode(nextMode)
    setError(null)
    setPassword('')
    setConfirmPassword('')
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-md rounded-3xl border border-slate-200 bg-white p-8 shadow-xl">
        <div className="mb-8 text-center">
          <h1 className="text-2xl font-bold text-slate-900">מערכת חלוקות</h1>
          <p className="mt-2 text-sm text-slate-600">
            {mode === 'register' ? 'רישום מנהל חדש' : 'התחברות למנהל'}
          </p>
        </div>

        <div className="mb-5 grid grid-cols-2 rounded-xl bg-slate-100 p-1 text-sm font-medium">
          <button
            type="button"
            onClick={() => switchMode('login')}
            className={`rounded-lg px-3 py-2 ${
              mode === 'login' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-600'
            }`}
          >
            כניסה
          </button>
          <button
            type="button"
            onClick={() => switchMode('register')}
            className={`rounded-lg px-3 py-2 ${
              mode === 'register'
                ? 'bg-white text-indigo-700 shadow-sm'
                : 'text-slate-600'
            }`}
          >
            רישום
          </button>
        </div>

        <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">
              כתובת מייל
            </span>
            <input
              className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none ring-indigo-200 focus:ring-2"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              autoComplete="email"
              disabled={loading}
              required
            />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">
              סיסמה
            </span>
            <input
              className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none ring-indigo-200 focus:ring-2"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
              disabled={loading}
              required
              minLength={mode === 'register' ? 6 : undefined}
            />
          </label>

          {mode === 'register' && (
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-slate-700">
                אימות סיסמה
              </span>
              <input
                className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none ring-indigo-200 focus:ring-2"
                type="password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                autoComplete="new-password"
                disabled={loading}
                required
                minLength={6}
              />
            </label>
          )}

          {error && (
            <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700" role="alert">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-xl bg-indigo-600 py-3 font-medium text-white transition hover:bg-indigo-700 disabled:opacity-60"
          >
            {loading
              ? mode === 'register'
                ? 'נרשם...'
                : 'מתחבר...'
              : mode === 'register'
                ? 'יצירת מנהל'
                : 'כניסה'}
          </button>
        </form>
      </div>
    </div>
  )
}
