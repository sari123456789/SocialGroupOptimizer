import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AppShell } from '../components/layout/AppShell'
import {
  CreateAssignmentParticipantPicker,
  type SelectedParticipantPayload,
} from '../components/create-assignment/CreateAssignmentParticipantPicker'
import {
  AssignmentsConnectionError,
  createAssignmentFromParticipants,
  downloadImportTemplate,
  fetchAssignments,
  importAssignmentFromExcel,
  type AssignmentSummary,
} from '../services/assignmentService'

type CreationMode = 'excel' | 'participants'

export function CreateAssignmentPage() {
  const navigate = useNavigate()
  const [mode, setMode] = useState<CreationMode>('excel')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [assignmentName, setAssignmentName] = useState('חלוקה חדשה')
  const [groupCount, setGroupCount] = useState('2')
  const [minGroupSize, setMinGroupSize] = useState('2')
  const [maxGroupSize, setMaxGroupSize] = useState('2')
  const [importing, setImporting] = useState(false)
  const [errors, setErrors] = useState<string[]>([])
  const [connectionError, setConnectionError] = useState<string | null>(null)

  const [assignments, setAssignments] = useState<AssignmentSummary[]>([])
  const [loadingAssignments, setLoadingAssignments] = useState(true)
  const [selectedParticipantIds, setSelectedParticipantIds] = useState<Set<string>>(
    new Set(),
  )
  const [participantDetailsById, setParticipantDetailsById] = useState<
    Map<string, SelectedParticipantPayload>
  >(new Map())

  const loadAssignments = useCallback(async () => {
    setLoadingAssignments(true)
    setConnectionError(null)

    try {
      const list = await fetchAssignments()
      setAssignments(list)
      setConnectionError(null)
    } catch (error) {
      if (error instanceof AssignmentsConnectionError) {
        setConnectionError(error.message)
      } else {
        setConnectionError('לא ניתן לטעון חלוקות.')
      }
    } finally {
      setLoadingAssignments(false)
    }
  }, [])

  useEffect(() => {
    void loadAssignments()
  }, [loadAssignments])

  async function handleDownloadTemplate() {
    setConnectionError(null)

    try {
      await downloadImportTemplate()
    } catch {
      setConnectionError('לא ניתן להוריד את התבנית.')
    }
  }

  async function handleImport() {
    if (!selectedFile) {
      return
    }

    setImporting(true)
    setConnectionError(null)
    setErrors([])

    try {
      const result = await importAssignmentFromExcel(selectedFile, {
        assignmentName: assignmentName.trim(),
        groupCount: Number(groupCount),
        minGroupSize: Number(minGroupSize),
        maxGroupSize: Number(maxGroupSize),
      })

      if (!result.success) {
        setErrors(result.errors)
        return
      }

      if (result.assignmentId) {
        navigate(`/assignments/${result.assignmentId}`)
      } else {
        navigate('/')
      }
    } catch {
      setConnectionError('לא ניתן להעלות את הקובץ.')
    } finally {
      setImporting(false)
    }
  }

  async function handleCreateFromParticipants() {
    if (selectedParticipantIds.size === 0) {
      setErrors(['יש לבחור לפחות משתתף אחד.'])
      return
    }

    setImporting(true)
    setConnectionError(null)
    setErrors([])

    const participants = Array.from(selectedParticipantIds)
      .map((id) => participantDetailsById.get(id))
      .filter((entry): entry is SelectedParticipantPayload => entry !== undefined)

    try {
      const result = await createAssignmentFromParticipants({
        assignmentName: assignmentName.trim(),
        groupCount: Number(groupCount),
        minGroupSize: Number(minGroupSize),
        maxGroupSize: Number(maxGroupSize),
        participants,
      })

      if (!result.success) {
        setErrors(result.errors)
        return
      }

      if (result.assignmentId) {
        navigate(`/assignments/${result.assignmentId}`)
      } else {
        navigate('/participants')
      }
    } catch {
      setConnectionError('לא ניתן ליצור את החלוקה.')
    } finally {
      setImporting(false)
    }
  }

  const settingsFields = (
    <div className="grid gap-4 sm:grid-cols-2">
      <label className="block sm:col-span-2">
        <span className="mb-2 block text-sm font-medium text-slate-700">שם חלוקה</span>
        <input
          type="text"
          value={assignmentName}
          onChange={(event) => setAssignmentName(event.target.value)}
          className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm"
        />
      </label>
      <label className="block">
        <span className="mb-2 block text-sm font-medium text-slate-700">מספר קבוצות</span>
        <input
          type="number"
          min={1}
          value={groupCount}
          onChange={(event) => setGroupCount(event.target.value)}
          className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm"
        />
      </label>
      <label className="block">
        <span className="mb-2 block text-sm font-medium text-slate-700">גודל קבוצה מינימום</span>
        <input
          type="number"
          min={1}
          value={minGroupSize}
          onChange={(event) => setMinGroupSize(event.target.value)}
          className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm"
        />
      </label>
      <label className="block">
        <span className="mb-2 block text-sm font-medium text-slate-700">גודל קבוצה מקסימום</span>
        <input
          type="number"
          min={1}
          value={maxGroupSize}
          onChange={(event) => setMaxGroupSize(event.target.value)}
          className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm"
        />
      </label>
    </div>
  )

  return (
    <AppShell>
      <div className="mb-6">
        <Link to="/participants" className="text-sm text-indigo-700 hover:underline">
          חזרה למשתתפים
        </Link>
      </div>

      <div className="mx-auto max-w-5xl rounded-3xl border border-slate-200 bg-white p-8 shadow-sm">
        <h1 className="text-2xl font-bold text-slate-900">חלוקה חדשה</h1>
        <p className="mt-2 text-slate-600">
          העלי קובץ Excel או בחרי משתתפים מחלוקות קיימות.
        </p>

        <div className="mt-6 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setMode('excel')}
            className={`rounded-full px-4 py-2 text-sm font-medium transition ${
              mode === 'excel'
                ? 'bg-indigo-600 text-white'
                : 'border border-slate-200 text-slate-700 hover:bg-slate-50'
            }`}
          >
            העלאת Excel
          </button>
          <button
            type="button"
            onClick={() => setMode('participants')}
            className={`rounded-full px-4 py-2 text-sm font-medium transition ${
              mode === 'participants'
                ? 'bg-indigo-600 text-white'
                : 'border border-slate-200 text-slate-700 hover:bg-slate-50'
            }`}
          >
            בחירת משתתפים
          </button>
        </div>

        <div className="mt-6">{settingsFields}</div>

        {mode === 'excel' && (
          <>
            <div className="mt-6 flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => void handleDownloadTemplate()}
                className="rounded-xl border border-indigo-200 px-4 py-2 text-sm font-medium text-indigo-700 hover:bg-indigo-50"
              >
                הורדת תבנית Excel
              </button>
            </div>

            <label className="mt-6 block">
              <span className="mb-2 block text-sm font-medium text-slate-700">קובץ Excel</span>
              <input
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                onChange={(event) => {
                  setSelectedFile(event.target.files?.[0] ?? null)
                  setErrors([])
                }}
                disabled={importing}
                className="block w-full text-sm text-slate-600"
              />
            </label>

            <button
              type="button"
              onClick={() => void handleImport()}
              disabled={importing || selectedFile === null}
              className="mt-6 rounded-xl bg-indigo-600 px-5 py-3 font-medium text-white disabled:opacity-60"
            >
              {importing ? 'מייבא...' : 'יצירת חלוקה'}
            </button>
          </>
        )}

        {mode === 'participants' && (
          <>
            <p className="mt-4 text-sm text-slate-600">
              סמני V על חלוקה שלמה, על קטגוריית סיווג, או על משתתפים בודדים. אפשר לבחור
              מכמה חלוקות.
            </p>

            <div className="mt-4">
              <CreateAssignmentParticipantPicker
                assignments={assignments}
                loadingAssignments={loadingAssignments}
                selectedParticipantIds={selectedParticipantIds}
                onSelectedParticipantIdsChange={setSelectedParticipantIds}
                participantDetailsById={participantDetailsById}
                onParticipantDetailsChange={setParticipantDetailsById}
              />
            </div>

            <button
              type="button"
              onClick={() => void handleCreateFromParticipants()}
              disabled={importing || selectedParticipantIds.size === 0}
              className="mt-6 rounded-xl bg-indigo-600 px-5 py-3 font-medium text-white disabled:opacity-60"
            >
              {importing
                ? 'יוצר חלוקה...'
                : `יצירת חלוקה (${selectedParticipantIds.size} משתתפים)`}
            </button>
          </>
        )}

        {connectionError && (
          <div
            className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700"
            role="alert"
          >
            <span>{connectionError}</span>
            <button
              type="button"
              onClick={() => void loadAssignments()}
              className="rounded-lg border border-red-200 bg-white px-3 py-1 text-sm font-medium text-red-800 hover:bg-red-100"
            >
              נסה שוב
            </button>
          </div>
        )}

        {errors.length > 0 && (
          <div className="mt-4 rounded-xl border border-red-200 bg-red-50 p-4" role="alert">
            <h2 className="font-semibold text-red-800">שגיאות</h2>
            <ul className="mt-2 list-disc space-y-1 pr-5 text-sm text-red-700">
              {errors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </AppShell>
  )
}
