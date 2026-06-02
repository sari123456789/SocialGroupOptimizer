import type { ParticipantListItem } from '../../services/participantsService'
import {
  getAvatarColorClass,
  getDisplayLabel,
  getParticipantInitial,
} from '../../utils/participantDisplay'

interface SelectableParticipantRowProps {
  participant: ParticipantListItem
  selected: boolean
  onToggle: (participantId: string) => void
}

export function SelectableParticipantRow({
  participant,
  selected,
  onToggle,
}: SelectableParticipantRowProps) {
  const label = getDisplayLabel(participant.displayName, participant.participantId)
  const initial = getParticipantInitial(participant.displayName, participant.participantId)
  const avatarColor = getAvatarColorClass(participant.participantId)

  return (
    <label className="flex cursor-pointer items-center gap-3 border-b border-slate-100 px-4 py-3 transition hover:bg-slate-50">
      <input
        type="checkbox"
        checked={selected}
        onChange={() => onToggle(participant.participantId)}
        className="h-4 w-4 shrink-0 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
      />

      <div
        className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-semibold text-white ${avatarColor}`}
      >
        {initial}
      </div>

      <div className="min-w-0 flex-1">
        <h3 className="truncate font-medium text-slate-900">{label}</h3>
        <p className="truncate text-sm text-slate-500">{participant.participantId}</p>
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
    </label>
  )
}
