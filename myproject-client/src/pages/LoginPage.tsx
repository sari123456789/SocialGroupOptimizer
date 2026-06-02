import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { AuthError } from '../services/authService'

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const [managerName, setManagerName] = useState('מנהל דמו')
  const [password, setPassword] = useState('demo1234')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError(null)

    try {
      await login({ managerName, password })
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

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-md rounded-3xl border border-slate-200 bg-white p-8 shadow-xl">
        <div className="mb-8 text-center">
          <h1 className="text-2xl font-bold text-slate-900">מערכת חלוקות</h1>
          <p className="mt-2 text-sm text-slate-600">התחברות למנהל</p>
        </div>

        <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">
              שם מנהל
            </span>
            <input
              className="w-full rounded-xl border border-slate-300 px-4 py-3 outline-none ring-indigo-200 focus:ring-2"
              value={managerName}
              onChange={(event) => setManagerName(event.target.value)}
              autoComplete="username"
              disabled={loading}
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
              autoComplete="current-password"
              disabled={loading}
            />
          </label>

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
            {loading ? 'מתחבר...' : 'כניסה'}
          </button>
        </form>
      </div>
    </div>
  )
}
