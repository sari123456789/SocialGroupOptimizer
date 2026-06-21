import { ParticipantNameWithId } from '../participants/ParticipantNameWithId'
import type { InitialPlacementResponse } from '../../services/initialPlacementService'
import type { ParticipantListItem } from '../../services/participantsService'

interface PlacementResultProps {
  result: InitialPlacementResponse
  participants?: ParticipantListItem[]
  onExplain?: (participantId: string) => void
}

export function PlacementResult({
  result,
  participants = [],
  onExplain,
}: PlacementResultProps) {
  const participantById = new Map(
    participants.map((participant) => [participant.participantId, participant]),
  )
  return (
    <div className="mt-6 space-y-4">
      <p className="rounded-lg bg-slate-50 px-4 py-3 text-sm">
        <span className="font-medium">סטטוס:</span> {result.status}
      </p>

      {result.groups.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2">
          {result.groups.map((group) => (
            <article
              key={group.groupId}
              className="rounded-xl border border-slate-200 bg-white p-4"
            >
              <h3 className="font-semibold text-slate-900">
                קבוצה {group.groupId}
              </h3>
              <ul className="mt-2 space-y-2 text-sm text-slate-700">
                {group.participantIds.map((participantId) => {
                  const participant = participantById.get(participantId)
                  return (
                    <li
                      key={participantId}
                      className="flex items-center justify-between gap-2"
                    >
                      <ParticipantNameWithId
                        participantId={participantId}
                        displayName={participant?.displayName}
                      />
                      {onExplain && (
                        <button
                          type="button"
                          onClick={() => onExplain(participantId)}
                          className="shrink-0 rounded-lg border border-indigo-200 px-2 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-50"
                        >
                          הסבר שיבוץ
                        </button>
                      )}
                    </li>
                  )
                })}
              </ul>
            </article>
          ))}
        </div>
      )}

      {result.errors.length > 0 && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4" role="alert">
          <h3 className="font-semibold text-red-800">שגיאות</h3>
          <ul className="mt-2 list-disc space-y-1 pr-5 text-sm text-red-700">
            {result.errors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
