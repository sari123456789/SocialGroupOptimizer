import { useMemo, useState } from 'react'
import { ParticipantRow } from '../participants/ParticipantRow'
import { ParticipantForm, type ParticipantFormPayload } from '../participants/ParticipantForm'
import type { AssignmentDetail } from '../../services/assignmentDetailService'
import type { ParticipantListItem } from '../../services/participantsService'

interface AssignmentParticipantsTabProps {
  detail: AssignmentDetail
  saving: boolean
  onAdd: (payload: ParticipantFormPayload) => void
  onUpdate: (identity: string, payload: Omit<ParticipantFormPayload, 'participantId'>) => void
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

  const closeModal = () => {
    setShowAddForm(false)
    setEditingParticipant(null)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <p className="mt-2 text-sm text-slate-600">
          {detail.participants.length} משתתפים - שינוי בנתונים מפעיל אימות מחדש אוטומטי
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

      {(showAddForm || editingParticipant) && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 py-6"
          onClick={closeModal}
        >
          <div
            className="max-h-full w-full max-w-3xl overflow-y-auto rounded-2xl bg-white p-5 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <ParticipantForm
              title={editingParticipant ? 'עריכת משתתף' : 'משתתף חדש'}
              dimensionCodes={dimensionCodes}
              saving={saving}
              allParticipants={detail.participants}
              initialIdentity={editingParticipant?.participantId ?? ''}
              initialName={editingParticipant?.displayName ?? ''}
              initialClassifications={editingParticipant?.classifications ?? {}}
              initialPreferences={
                editingParticipant?.preferences
                  ?.slice()
                  .sort((a, b) => a.rank - b.rank)
                  .map((preference) => preference.participantId) ?? []
              }
              onCancel={closeModal}
              onSubmit={(payload) => {
                if (editingParticipant) {
                  onUpdate(editingParticipant.participantId, {
                    displayName: payload.displayName,
                    classifications: payload.classifications,
                    preferences: payload.preferences,
                  })
                } else {
                  onAdd(payload)
                }

                closeModal()
              }}
            />
          </div>
        </div>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200">
        {detail.participants.length === 0 && (
          <p className="px-4 py-8 text-center text-sm text-slate-500">
            עדיין אין משתתפים. הוסיפי משתתף ידנית או ייבאי מקובץ.
          </p>
        )}

        {detail.participants.map((participant) => (
          <div key={participant.participantId} className="group relative">
            <div
              role="button"
              tabIndex={0}
              onClick={() => {
                setEditingParticipant(participant)
                setShowAddForm(false)
              }}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault()
                  setEditingParticipant(participant)
                  setShowAddForm(false)
                }
              }}
              className="cursor-pointer text-start focus:outline-none focus:ring-2 focus:ring-inset focus:ring-indigo-500"
            >
              <ParticipantRow participant={participant} />
            </div>
            <div className="absolute inset-y-0 end-3 flex items-center gap-2 opacity-100 sm:opacity-0 sm:group-hover:opacity-100">
              <button
                type="button"
                onClick={(event) => {
                  event.stopPropagation()
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
