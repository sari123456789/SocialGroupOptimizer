import { useMemo, useState } from 'react'
import { ParticipantRow } from '../participants/ParticipantRow'
import type { AssignmentDetail } from '../../services/assignmentDetailService'
import type { ParticipantListItem } from '../../services/participantsService'

interface AssignmentParticipantsTabProps {
  detail: AssignmentDetail
  saving: boolean
  onAdd: (payload: {
    participantId: string
    displayName: string
    classifications: Record<string, string>
  }) => void
  onUpdate: (
    identity: string,
    payload: { displayName: string; classifications: Record<string, string> },
  ) => void
  onDelete: (identity: string) => void
}

export function AssignmentParticipantsTab({
  detail,
  saving,
  onAdd,
  onUpdate,
  onDelete,
}: AssignmentParticipantsTabProps) {
  const [showAddForm, setShowAddForm] = useState(false)
  const [editingParticipant, setEditingParticipant] =
    useState<ParticipantListItem | null>(null)

  const dimensionCodes = useMemo(() => {
    const fromParticipants = detail.participants.flatMap((participant) =>
      Object.keys(participant.classifications),
    )
    const merged = new Set([...detail.availableDimensions, ...fromParticipants])
    return Array.from(merged).sort()
  }, [detail.availableDimensions, detail.participants])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <p className="mt-2 text-sm text-slate-600">
          {detail.participants.length} משתתפים — שינוי בנתונים מפעיל אימות מחדש אוטומטית
        </p>
        <button
          type="button"
          onClick={() => {
            setShowAddForm(true)
            setEditingParticipant(null)
          }}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white"
        >
          הוספת משתתף
        </button>
      </div>

      {showAddForm && (
        <ParticipantForm
          title="משתתף חדש"
          dimensionCodes={dimensionCodes}
          saving={saving}
          onCancel={() => setShowAddForm(false)}
          onSubmit={(payload) => {
            onAdd(payload)
            setShowAddForm(false)
          }}
        />
      )}

      {editingParticipant && (
        <ParticipantForm
          title="עריכת משתתף"
          dimensionCodes={dimensionCodes}
          saving={saving}
          initialIdentity={editingParticipant.participantId}
          initialName={editingParticipant.displayName ?? ''}
          initialClassifications={editingParticipant.classifications}
          onCancel={() => setEditingParticipant(null)}
          onSubmit={(payload) => {
            onUpdate(editingParticipant.participantId, {
              displayName: payload.displayName,
              classifications: payload.classifications,
            })
            setEditingParticipant(null)
          }}
        />
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200">
        {detail.participants.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-slate-500">
            עדיין אין משתתפים. הוסיפי משתתף ידנית או ייבאי מקובץ.
          </p>
        )}

        {detail.participants.map((participant) => (
          <div key={participant.participantId} className="group relative">
            <ParticipantRow participant={participant} />
            <div className="absolute inset-y-0 end-3 flex items-center gap-2 opacity-100 sm:opacity-0 sm:group-hover:opacity-100">
              <button
                type="button"
                onClick={() => {
                  setEditingParticipant(participant)
                  setShowAddForm(false)
                }}
                className="rounded-lg bg-white px-2 py-1 text-xs font-medium text-indigo-700 shadow-sm ring-1 ring-slate-200"
              >
                עריכה
              </button>
              <button
                type="button"
                onClick={() => {
                  if (window.confirm('למחוק את המשתתף מהחלוקה?')) {
                    onDelete(participant.participantId)
                  }
                }}
                className="rounded-lg bg-white px-2 py-1 text-xs font-medium text-red-700 shadow-sm ring-1 ring-slate-200"
              >
                מחיקה
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

interface ParticipantFormProps {
  title: string
  dimensionCodes: string[]
  saving: boolean
  initialIdentity?: string
  initialName?: string
  initialClassifications?: Record<string, string>
  onCancel: () => void
  onSubmit: (payload: {
    participantId: string
    displayName: string
    classifications: Record<string, string>
  }) => void
}

function ParticipantForm({
  title,
  dimensionCodes,
  saving,
  initialIdentity = '',
  initialName = '',
  initialClassifications = {},
  onCancel,
  onSubmit,
}: ParticipantFormProps) {
  const [identity, setIdentity] = useState(initialIdentity)
  const [name, setName] = useState(initialName)
  const [classifications, setClassifications] = useState<Record<string, string>>(
    () => ({ ...initialClassifications }),
  )
  const [newDimension, setNewDimension] = useState('')
  const [newLevel, setNewLevel] = useState('')

  const isEdit = Boolean(initialIdentity)

  function handleAddClassification() {
    const dimension = newDimension.trim()
    const level = newLevel.trim()

    if (!dimension || !level) {
      return
    }

    setClassifications((current) => ({ ...current, [dimension]: level }))
    setNewDimension('')
    setNewLevel('')
  }

  return (
    <form
      className="rounded-xl border border-indigo-100 bg-indigo-50/40 p-4"
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit({
          participantId: identity.trim(),
          displayName: name.trim(),
          classifications,
        })
      }}
    >
      <h3 className="font-semibold text-slate-900">{title}</h3>

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <label className="block">
          <span className="text-sm text-slate-700">תעודת זהות</span>
          <input
            type="text"
            value={identity}
            onChange={(event) => setIdentity(event.target.value)}
            disabled={isEdit}
            className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm disabled:bg-slate-100"
            required
          />
        </label>
        <label className="block">
          <span className="text-sm text-slate-700">שם</span>
          <input
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
        </label>
      </div>

      <div className="mt-4">
        <p className="text-sm font-medium text-slate-700">סיווגים</p>
        <div className="mt-2 flex flex-wrap gap-2">
          {Object.entries(classifications).map(([dimension, level]) => (
            <span
              key={dimension}
              className="inline-flex items-center gap-1 rounded-full bg-white px-2 py-1 text-xs ring-1 ring-slate-200"
            >
              {dimension}: {level}
              <button
                type="button"
                onClick={() =>
                  setClassifications((current) => {
                    const next = { ...current }
                    delete next[dimension]
                    return next
                  })
                }
                className="text-slate-400 hover:text-red-600"
              >
                ×
              </button>
            </span>
          ))}
        </div>

        <div className="mt-2 flex flex-wrap gap-2">
          <input
            type="text"
            list="dimension-suggestions"
            placeholder="מימד"
            value={newDimension}
            onChange={(event) => setNewDimension(event.target.value)}
            className="rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
          <datalist id="dimension-suggestions">
            {dimensionCodes.map((code) => (
              <option key={code} value={code} />
            ))}
          </datalist>
          <input
            type="text"
            placeholder="רמה"
            value={newLevel}
            onChange={(event) => setNewLevel(event.target.value)}
            className="rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
          <button
            type="button"
            onClick={handleAddClassification}
            className="rounded-lg border border-slate-200 px-3 py-2 text-sm"
          >
            הוספת סיווג
          </button>
        </div>
      </div>

      <div className="mt-4 flex gap-2">
        <button
          type="submit"
          disabled={saving || Object.keys(classifications).length === 0}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {saving ? 'שומר...' : 'שמירה'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-slate-200 px-4 py-2 text-sm"
        >
          ביטול
        </button>
      </div>
    </form>
  )
}
