import {
  getDisplayLabel,
  hasDisplayName,
} from '../../utils/participantDisplay'

interface ParticipantNameWithIdProps {
  participantId: string
  displayName?: string | null
  layout?: 'inline' | 'stacked'
}

export function ParticipantNameWithId({
  participantId,
  displayName,
  layout = 'stacked',
}: ParticipantNameWithIdProps) {
  const label = getDisplayLabel(displayName, participantId)
  const showSmallId = hasDisplayName(displayName)

  if (layout === 'inline') {
    return (
      <span>
        <span className="font-medium text-slate-900">{label}</span>
        {showSmallId && (
          <span className="mr-1 text-xs text-slate-400">{participantId}</span>
        )}
      </span>
    )
  }

  return (
    <span className="block">
      <span className="font-medium text-slate-900">{label}</span>
      {showSmallId && (
        <span className="block text-xs text-slate-400">{participantId}</span>
      )}
    </span>
  )
}
