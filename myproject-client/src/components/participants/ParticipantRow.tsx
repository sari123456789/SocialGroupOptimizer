import type { ParticipantListItem } from '../../services/participantsService'
import {
  getAvatarColorClass,
  getDisplayLabel,
  getParticipantInitial,
} from '../../utils/participantDisplay'
import { ParticipantNameWithId } from './ParticipantNameWithId'

interface ParticipantRowProps {
  participant: ParticipantListItem
}

export function ParticipantRow({ participant }: ParticipantRowProps) {
  const initial = getParticipantInitial(participant.displayName, participant.participantId)
  const avatarColor = getAvatarColorClass(participant.participantId)

  return (
    <article className="flex items-center gap-4 border-b border-slate-100 px-4 py-3 transition hover:bg-slate-50">
      <div
        className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-semibold text-white ${avatarColor}`}
      >
        {initial}
      </div>

      <div className="min-w-0 flex-1">
        <h3 className="truncate">
          <ParticipantNameWithId
            participantId={participant.participantId}
            displayName={participant.displayName}
          />
        </h3>
        {participant.preferences && participant.preferences.length > 0 && (
          <p className="mt-1 text-xs text-slate-500">
            העדפות:{' '}
            {participant.preferences.map((preference, index) => (
              <span key={`${preference.participantId}-${preference.rank}`}>
                {index > 0 && ' · '}
                {preference.rank}. {getDisplayLabel(preference.displayName, preference.participantId)}
              </span>
            ))}
          </p>
        )}
      </div>

      <div className="hidden flex-wrap justify-end gap-1 sm:flex">
        {Object.entries(participant.classifications).map(([dimension, level]) => (
          <span
            key={`${dimension}-${level}`}
            className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600"
          >
            {dimension}: {level}
          </span>
        ))}
      </div>
    </article>
  )
}
