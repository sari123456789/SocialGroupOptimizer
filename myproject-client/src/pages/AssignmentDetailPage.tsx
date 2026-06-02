import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { AssignmentConstraintsTab } from '../components/assignment/AssignmentConstraintsTab'
import { AssignmentOverviewTab } from '../components/assignment/AssignmentOverviewTab'
import { AssignmentParticipantsTab } from '../components/assignment/AssignmentParticipantsTab'
import { AppShell } from '../components/layout/AppShell'
import {
  addClassificationConstraint,
  addForbiddenPair,
  addMandatoryPair,
  addParticipant,
  deleteClassificationConstraint,
  deleteForbiddenPair,
  deleteMandatoryPair,
  deleteParticipant,
  fetchAssignmentDetail,
  isEditError,
  updateAssignment,
  updateParticipant,
  validateAssignment,
  type AssignmentDetail,
} from '../services/assignmentDetailService'

type TabId = 'overview' | 'participants' | 'constraints'

const tabs: { id: TabId; label: string }[] = [
  { id: 'overview', label: 'פרטים ואימות' },
  { id: 'participants', label: 'משתתפים' },
  { id: 'constraints', label: 'אילוצים' },
]

export function AssignmentDetailPage() {
  const { assignmentId } = useParams()
  const numericAssignmentId = Number(assignmentId)

  const [detail, setDetail] = useState<AssignmentDetail | null>(null)
  const [activeTab, setActiveTab] = useState<TabId>('overview')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [validating, setValidating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const [editingSettings, setEditingSettings] = useState(false)
  const [assignmentName, setAssignmentName] = useState('')
  const [minGroups, setMinGroups] = useState('')
  const [maxGroups, setMaxGroups] = useState('')
  const [minGroupSize, setMinGroupSize] = useState('')
  const [maxGroupSize, setMaxGroupSize] = useState('')

  const loadDetail = useCallback(async () => {
    if (!Number.isFinite(numericAssignmentId)) {
      setError('מזהה חלוקה לא תקין.')
      setLoading(false)
      return
    }

    setLoading(true)
    setError(null)

    try {
      const data = await fetchAssignmentDetail(numericAssignmentId)

      if (!data) {
        setError('החלוקה לא נמצאה.')
        setDetail(null)
        return
      }

      setDetail(data)
      resetSettingsForm(data)
    } catch {
      setError('לא ניתן לטעון את החלוקה.')
    } finally {
      setLoading(false)
    }
  }, [numericAssignmentId])

  useEffect(() => {
    void loadDetail()
  }, [loadDetail])

  function resetSettingsForm(data: AssignmentDetail) {
    setAssignmentName(data.assignmentName)
    setMinGroups(String(data.settings.minGroups))
    setMaxGroups(String(data.settings.maxGroups))
    setMinGroupSize(String(data.settings.minGroupSize))
    setMaxGroupSize(String(data.settings.maxGroupSize))
  }

  async function applyEditResult(
    result: AssignmentDetail | { success: false; errors: string[] },
  ) {
    if (isEditError(result)) {
      setActionError(result.errors.join(' '))
      return
    }

    setDetail(result)
    resetSettingsForm(result)
    setActionError(null)
  }

  async function handleSaveSettings() {
    if (!detail) {
      return
    }

    setSaving(true)
    setActionError(null)

    const result = await updateAssignment(detail.assignmentId, {
      assignmentName: assignmentName.trim(),
      settings: {
        minGroups: Number(minGroups),
        maxGroups: Number(maxGroups),
        minGroupSize: Number(minGroupSize),
        maxGroupSize: Number(maxGroupSize),
      },
    })

    await applyEditResult(result)
    setSaving(false)

    if (!isEditError(result)) {
      setEditingSettings(false)
    }
  }

  async function handleValidate() {
    if (!Number.isFinite(numericAssignmentId)) {
      return
    }

    setValidating(true)
    setActionError(null)

    try {
      const data = await validateAssignment(numericAssignmentId)
      if (data) {
        setDetail(data)
        resetSettingsForm(data)
      }
    } catch {
      setActionError('לא ניתן לאמת את החלוקה.')
    } finally {
      setValidating(false)
    }
  }

  async function withSaving(action: () => Promise<void>) {
    setSaving(true)
    setActionError(null)

    try {
      await action()
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppShell>
      <div className="mb-6">
        <Link to="/" className="text-sm text-indigo-700 hover:underline">
          חזרה לדשבורד
        </Link>
      </div>

      {loading && (
        <div className="h-40 animate-pulse rounded-3xl bg-white/70" />
      )}

      {!loading && error && (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-red-700" role="alert">
          {error}
        </p>
      )}

      {!loading && detail && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-100 px-6 py-6 sm:px-8">
            <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">
              {detail.assignmentName}
            </h1>
          </div>

          <nav className="flex gap-1 overflow-x-auto border-b border-slate-100 px-4 sm:px-6">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={`shrink-0 border-b-2 px-4 py-3 text-sm font-medium transition ${
                  activeTab === tab.id
                    ? 'border-indigo-600 text-indigo-700'
                    : 'border-transparent text-slate-500 hover:text-slate-800'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </nav>

          <div className="px-6 py-6 sm:px-8">
            {actionError && (
              <p className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
                {actionError}
              </p>
            )}

            {activeTab === 'overview' && (
              <AssignmentOverviewTab
                detail={detail}
                editing={editingSettings}
                validating={validating}
                assignmentName={assignmentName}
                minGroups={minGroups}
                maxGroups={maxGroups}
                minGroupSize={minGroupSize}
                maxGroupSize={maxGroupSize}
                saving={saving}
                onStartEdit={() => setEditingSettings(true)}
                onCancelEdit={() => {
                  resetSettingsForm(detail)
                  setEditingSettings(false)
                }}
                onSave={() => void handleSaveSettings()}
                onValidate={() => void handleValidate()}
                onAssignmentNameChange={setAssignmentName}
                onMinGroupsChange={setMinGroups}
                onMaxGroupsChange={setMaxGroups}
                onMinGroupSizeChange={setMinGroupSize}
                onMaxGroupSizeChange={setMaxGroupSize}
              />
            )}

            {activeTab === 'participants' && (
              <AssignmentParticipantsTab
                detail={detail}
                saving={saving}
                onAdd={(payload) =>
                  void withSaving(async () => {
                    const result = await addParticipant(detail.assignmentId, payload)
                    await applyEditResult(result)
                  })
                }
                onUpdate={(identity, payload) =>
                  void withSaving(async () => {
                    const result = await updateParticipant(
                      detail.assignmentId,
                      identity,
                      payload,
                    )
                    await applyEditResult(result)
                  })
                }
                onDelete={(identity) =>
                  void withSaving(async () => {
                    const result = await deleteParticipant(detail.assignmentId, identity)
                    await applyEditResult(result)
                  })
                }
              />
            )}

            {activeTab === 'constraints' && (
              <AssignmentConstraintsTab
                detail={detail}
                saving={saving}
                onAddMandatory={(a, b) =>
                  void withSaving(async () => {
                    const result = await addMandatoryPair(detail.assignmentId, a, b)
                    await applyEditResult(result)
                  })
                }
                onDeleteMandatory={(id) =>
                  void withSaving(async () => {
                    const result = await deleteMandatoryPair(detail.assignmentId, id)
                    await applyEditResult(result)
                  })
                }
                onAddForbidden={(a, b) =>
                  void withSaving(async () => {
                    const result = await addForbiddenPair(detail.assignmentId, a, b)
                    await applyEditResult(result)
                  })
                }
                onDeleteForbidden={(id) =>
                  void withSaving(async () => {
                    const result = await deleteForbiddenPair(detail.assignmentId, id)
                    await applyEditResult(result)
                  })
                }
                onAddClassification={(dimension, rule) =>
                  void withSaving(async () => {
                    const result = await addClassificationConstraint(
                      detail.assignmentId,
                      dimension,
                      rule,
                    )
                    await applyEditResult(result)
                  })
                }
                onDeleteClassification={(dimension) =>
                  void withSaving(async () => {
                    const result = await deleteClassificationConstraint(
                      detail.assignmentId,
                      dimension,
                    )
                    await applyEditResult(result)
                  })
                }
              />
            )}
          </div>
        </div>
      )}
    </AppShell>
  )
}
