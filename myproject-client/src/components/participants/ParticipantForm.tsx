import { useState } from 'react'
import type { ParticipantListItem } from '../../services/participantsService'
import { getDisplayLabel } from '../../utils/participantDisplay'

export type ParticipantFormPayload = {
  participantId: string
  displayName: string
  classifications: Record<string, string>
  preferences: string[]
}

interface ParticipantFormProps {
  title: string
  dimensionCodes: string[]
  saving?: boolean
  allParticipants: ParticipantListItem[]
  initialIdentity?: string
  initialName?: string
  initialClassifications?: Record<string, string>
  initialPreferences?: string[]
  showPreferences?: boolean
  requireClassifications?: boolean
  onCancel: () => void
  onSubmit: (payload: ParticipantFormPayload) => void
}

export function ParticipantForm({
  title,
  dimensionCodes,
  saving = false,
  allParticipants,
  initialIdentity = '',
  initialName = '',
  initialClassifications = {},
  initialPreferences = [],
  showPreferences = true,
  requireClassifications = true,
  onCancel,
  onSubmit,
}: ParticipantFormProps) {
  const [identity, setIdentity] = useState(initialIdentity)
  const [name, setName] = useState(initialName)
  const [classifications, setClassifications] = useState<Record<string, string>>(
    () => ({ ...initialClassifications }),
  )
  const [preferences, setPreferences] = useState<string[]>(() => [
    ...initialPreferences,
  ])
  const [selectedPreference, setSelectedPreference] = useState('')
  const [newDimension, setNewDimension] = useState('')
  const [newLevel, setNewLevel] = useState('')

  const isEdit = Boolean(initialIdentity)
  const trimmedIdentity = identity.trim()

  const availablePreferences = allParticipants.filter(
    (participant) =>
      participant.participantId !== trimmedIdentity &&
      !preferences.includes(participant.participantId),
  )

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

  function handleAddPreference() {
    if (!selectedPreference) {
      return
    }

    setPreferences((current) => [...current, selectedPreference])
    setSelectedPreference('')
  }

  function movePreference(index: number, direction: -1 | 1) {
    const nextIndex = index + direction
    if (nextIndex < 0 || nextIndex >= preferences.length) {
      return
    }

    setPreferences((current) => {
      const next = [...current]
      const currentPreference = next[index]
      next[index] = next[nextIndex]
      next[nextIndex] = currentPreference
      return next
    })
  }

  const participantLabelById = new Map(
    allParticipants.map((participant) => [
      participant.participantId,
      getDisplayLabel(participant.displayName, participant.participantId),
    ]),
  )

  const missingClassifications =
    requireClassifications && Object.keys(classifications).length === 0

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit({
          participantId: trimmedIdentity,
          displayName: name.trim(),
          classifications,
          preferences,
        })
      }}
    >
      <div className="flex items-start justify-between gap-4">
        <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-slate-200 px-3 py-1 text-sm text-slate-600"
        >
          סגירה
        </button>
      </div>

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

      <div className="mt-5">
        <p className="text-sm font-medium text-slate-700">סיווגים</p>
        <div className="mt-2 flex flex-wrap gap-2">
          {Object.entries(classifications).map(([dimension, level]) => (
            <span
              key={dimension}
              className="inline-flex items-center gap-1 rounded-full bg-slate-50 px-2 py-1 text-xs ring-1 ring-slate-200"
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
                x
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

      {showPreferences && (
        <div className="mt-5">
          <p className="text-sm font-medium text-slate-700">העדפות חברתיות</p>
          <div className="mt-2 space-y-2">
            {preferences.length === 0 && (
              <p className="text-sm text-slate-500">לא הוגדרו העדפות.</p>
            )}

            {preferences.map((participantId, index) => (
              <div
                key={participantId}
                className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-slate-200 px-3 py-2 text-sm"
              >
                <span>
                  {index + 1}. {participantLabelById.get(participantId) ?? participantId}
                </span>
                <span className="flex gap-1">
                  <button
                    type="button"
                    onClick={() => movePreference(index, -1)}
                    disabled={index === 0}
                    className="rounded border border-slate-200 px-2 py-1 disabled:opacity-40"
                  >
                    למעלה
                  </button>
                  <button
                    type="button"
                    onClick={() => movePreference(index, 1)}
                    disabled={index === preferences.length - 1}
                    className="rounded border border-slate-200 px-2 py-1 disabled:opacity-40"
                  >
                    למטה
                  </button>
                  <button
                    type="button"
                    onClick={() =>
                      setPreferences((current) =>
                        current.filter((id) => id !== participantId),
                      )
                    }
                    className="rounded border border-slate-200 px-2 py-1 text-red-700"
                  >
                    הסרה
                  </button>
                </span>
              </div>
            ))}
          </div>

          <div className="mt-3 flex flex-wrap gap-2">
            <select
              value={selectedPreference}
              onChange={(event) => setSelectedPreference(event.target.value)}
              className="min-w-56 rounded-lg border border-slate-200 px-3 py-2 text-sm"
            >
              <option value="">בחירת משתתף</option>
              {availablePreferences.map((participant) => (
                <option key={participant.participantId} value={participant.participantId}>
                  {getDisplayLabel(participant.displayName, participant.participantId)}
                </option>
              ))}
            </select>
            <button
              type="button"
              onClick={handleAddPreference}
              disabled={!selectedPreference}
              className="rounded-lg border border-slate-200 px-3 py-2 text-sm disabled:opacity-50"
            >
              הוספת העדפה
            </button>
          </div>
        </div>
      )}

      <div className="mt-6 flex gap-2">
        <button
          type="submit"
          disabled={saving || missingClassifications}
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
