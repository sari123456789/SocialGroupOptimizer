const AVATAR_COLORS = [
  'bg-teal-500',
  'bg-emerald-500',
  'bg-sky-500',
  'bg-indigo-500',
  'bg-violet-500',
  'bg-rose-500',
  'bg-amber-500',
  'bg-cyan-500',
]

export function getParticipantInitial(
  displayName: string | null | undefined,
  participantId: string,
): string {
  const source = displayName?.trim() || participantId
  return source.charAt(0).toUpperCase()
}

export function getAvatarColorClass(participantId: string): string {
  let hash = 0
  for (let index = 0; index < participantId.length; index += 1) {
    hash = participantId.charCodeAt(index) + ((hash << 5) - hash)
  }

  return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length]
}

export function getDisplayLabel(
  displayName: string | null | undefined,
  participantId: string,
): string {
  return displayName?.trim() || participantId
}
