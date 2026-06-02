import { useCallback, useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { SessionExpiredError } from '../services/apiClient'
import { AssignmentList } from '../components/assignments/AssignmentList'
import { AppShell } from '../components/layout/AppShell'
import {
  AssignmentsConnectionError,
  fetchAssignments,
  type AssignmentSummary,
} from '../services/assignmentService'

export function DashboardPage() {
  const [assignments, setAssignments] = useState<AssignmentSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [sessionExpired, setSessionExpired] = useState(false)

  const loadAssignments = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const list = await fetchAssignments()
      setAssignments(list)
    } catch (loadError) {
      if (loadError instanceof SessionExpiredError) {
        setSessionExpired(true)
        return
      }

      if (loadError instanceof AssignmentsConnectionError) {
        setError(loadError.message)
      } else {
        setError('לא ניתן לטעון חלוקות.')
      }
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadAssignments()
  }, [loadAssignments])

  if (sessionExpired) {
    return <Navigate to="/login" replace />
  }

  return (
    <AppShell>
      <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-900">החלוקות שלי</h1>
          <p className="mt-2 text-slate-600">
            ניהול חלוקות, העלאת Excel והרצת חלוקה ראשונית.
          </p>
        </div>
        <Link
          to="/assignments/new"
          className="rounded-xl bg-indigo-600 px-5 py-3 font-medium text-white transition hover:bg-indigo-700"
        >
          חלוקה חדשה
        </Link>
      </div>

      {loading && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="h-36 animate-pulse rounded-2xl bg-white/70"
            />
          ))}
        </div>
      )}

      {!loading && error && (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-red-700" role="alert">
          {error}
        </p>
      )}

      {!loading && !error && assignments.length === 0 && (
        <div className="rounded-3xl border border-dashed border-slate-300 bg-white/70 p-10 text-center">
          <h2 className="text-xl font-semibold text-slate-900">
            עדיין אין חלוקות
          </h2>
          <p className="mt-2 text-slate-600">
            התחילי ביצירת חלוקה חדשה מהעלאת קובץ Excel.
          </p>
          <Link
            to="/assignments/new"
            className="mt-6 inline-flex rounded-xl bg-indigo-600 px-5 py-3 font-medium text-white"
          >
            הוספת חלוקה
          </Link>
        </div>
      )}

      {!loading && !error && assignments.length > 0 && (
        <AssignmentList assignments={assignments} />
      )}
    </AppShell>
  )
}
