import { useMemo, useState } from 'react'
import { ParticipantForm } from '../participants/ParticipantForm'
import { ParticipantNameWithId } from '../participants/ParticipantNameWithId'
import type { ParticipantListItem } from '../../services/participantsService'
import { getDisplayLabel } from '../../utils/participantDisplay'
import type { SelectedParticipantPayload } from './CreateAssignmentParticipantPicker'

interface CreateAssignmentManualEntryProps {
  participants: SelectedParticipantPayload[]
  onParticipantsChange: (next: SelectedParticipantPayload[]) => void
}

function normalizeIdentity(raw: string): string {
  const trimmed = raw.trim()
  const dotIndex = trimmed.indexOf('.')
  return dotIndex >= 0 ? trimmed.slice(0, dotIndex) : trimmed
}

function isValidIdentity(raw: string): boolean {
  const normalized = normalizeIdentity(raw)
  return normalized.length === 9 && /^\d{9}$/.test(normalized)
}

function toListItem(participant: SelectedParticipantPayload): ParticipantListItem {
  return {
    participantId: participant.participantId,
    displayName: participant.displayName ?? null,
    classifications: participant.classifications,
    preferences: (participant.preferences ?? []).map((participantId, index) => ({
      rank: index + 1,
      participantId,
      displayName: null,
    })),
  }
}

export function CreateAssignmentManualEntry({
  participants,
  onParticipantsChange,
}: CreateAssignmentManualEntryProps) {
  const [showForm, setShowForm] = useState(false)
  const [editingParticipantId, setEditingParticipantId] = useState<string | null>(null)

  const dimensionCodes = useMemo(() => {
    const codes = participants.flatMap((participant) =>
      Object.keys(participant.classifications),
    )
    return Array.from(new Set(codes)).sort()
  }, [participants])

  const participantListItems = useMemo(
    () => participants.map(toListItem),
    [participants],
  )

  const labelById = useMemo(
    () =>
      new Map(
        participants.map((participant) => [
          participant.participantId,
          getDisplayLabel(participant.displayName, participant.participantId),
        ]),
      ),
    [participants],
  )

  const editingParticipant = editingParticipantId
    ? participants.find((entry) => entry.participantId === editingParticipantId)
    : null

  function closeForm() {
    setShowForm(false)
    setEditingParticipantId(null)
  }

  function upsertParticipant(payload: {
    participantId: string
    displayName: string
    classifications: Record<string, string>
    preferences: string[]
  }) {
    if (!isValidIdentity(payload.participantId)) {
      return 'תעודת זהות חייבת להיות 9 ספרות.'
    }

    const normalizedId = normalizeIdentity(payload.participantId)
    const duplicate = participants.some(
      (entry) =>
        entry.participantId === normalizedId
        && entry.participantId !== editingParticipantId,
    )

    if (duplicate) {
      return 'משתתף עם תעודת זהות זו כבר קיים ברשימה.'
    }

    const knownIds = new Set(
      participants
        .filter((entry) => entry.participantId !== editingParticipantId)
        .map((entry) => entry.participantId),
    )

    const invalidPreference = payload.preferences.find(
      (participantId) => !knownIds.has(participantId),
    )
    if (invalidPreference) {
      return 'העדפה חייבת להתייחס למשתתף אחר שכבר נמצא ברשימה.'
    }

    const nextEntry: SelectedParticipantPayload = {
      participantId: normalizedId,
      displayName: payload.displayName || null,
      classifications: payload.classifications,
      preferences: payload.preferences,
    }

    if (editingParticipantId) {
      onParticipantsChange(
        participants.map((entry) =>
          entry.participantId === editingParticipantId ? nextEntry : entry,
        ),
      )
    } else {
      onParticipantsChange([...participants, nextEntry])
    }

    closeForm()
    return null
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-600">
          {participants.length} משתתפים ברשימה. אפשר להוסיף, לערוך ולהסיר לפני יצירת החלוקה.
        </p>
        <button
          type="button"
          onClick={() => {
            setEditingParticipantId(null)
            setShowForm(true)
          }}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white"
        >
          הוספת משתתף
        </button>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200">
        {participants.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-slate-500">
            עדיין אין משתתפים. לחצי על &quot;הוספת משתתף&quot; כדי להתחיל.
          </p>
        )}

        {participants.map((participant) => (
          <div
            key={participant.participantId}
            className="group flex items-center justify-between gap-3 border-b border-slate-100 px-4 py-3 last:border-b-0"
          >
            <button
              type="button"
              onClick={() => {
                setEditingParticipantId(participant.participantId)
                setShowForm(true)
              }}
              className="min-w-0 flex-1 text-start"
            >
              <ParticipantNameWithId
                displayName={participant.displayName}
                participantId={participant.participantId}
              />
              {Object.keys(participant.classifications).length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {Object.entries(participant.classifications).map(([dimension, level]) => (
                    <span
                      key={`${participant.participantId}-${dimension}`}
                      className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600"
                    >
                      {dimension}: {level}
                    </span>
                  ))}
                </div>
              )}
              {(participant.preferences?.length ?? 0) > 0 && (
                <p className="mt-2 text-xs text-slate-500">
                  העדפות:{' '}
                  {participant.preferences
                    ?.map(
                      (participantId, index) =>
                        `${index + 1}. ${labelById.get(participantId) ?? participantId}`,
                    )
                    .join(' · ')}
                </p>
              )}
            </button>

            <button
              type="button"
              onClick={() =>
                onParticipantsChange(
                  participants.filter(
                    (entry) => entry.participantId !== participant.participantId,
                  ),
                )
              }
              className="rounded-lg border border-red-200 px-2 py-1 text-xs font-medium text-red-700 opacity-100 sm:opacity-0 sm:group-hover:opacity-100"
            >
              הסרה
            </button>
          </div>
        ))}
      </div>

      {showForm && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 py-6"
          onClick={closeForm}
        >
          <div
            className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white p-5 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <ParticipantForm
              title={editingParticipant ? 'עריכת משתתף' : 'משתתף חדש'}
              dimensionCodes={dimensionCodes}
              allParticipants={participantListItems}
              initialIdentity={editingParticipant?.participantId ?? ''}
              initialName={editingParticipant?.displayName ?? ''}
              initialClassifications={editingParticipant?.classifications ?? {}}
              initialPreferences={editingParticipant?.preferences ?? []}
              showPreferences
              requireClassifications={false}
              onCancel={closeForm}
              onSubmit={(payload) => {
                const error = upsertParticipant(payload)
                if (error) {
                  window.alert(error)
                }
              }}
            />
          </div>
        </div>
      )}
    </div>
  )
}
