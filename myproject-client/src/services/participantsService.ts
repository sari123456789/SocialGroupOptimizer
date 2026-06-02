import { authFetch } from './apiClient'

export interface ClassificationGroup {
  dimensionCode: string
  levelCode: string
  participantCount: number
}

export interface ParticipantListItem {
  participantId: string
  displayName?: string | null
  classifications: Record<string, string>
}

export interface AssignmentParticipants {
  assignmentId: number
  assignmentName: string
  classificationGroups: ClassificationGroup[]
  participants: ParticipantListItem[]
}

export class ParticipantsConnectionError extends Error {
  constructor(message = 'לא ניתן לטעון משתתפים') {
    super(message)
    this.name = 'ParticipantsConnectionError'
  }
}

export async function fetchAssignmentParticipants(
  assignmentId: number,
): Promise<AssignmentParticipants> {
  const response = await authFetch(`/api/assignments/${assignmentId}/participants`)

  if (response.status === 404) {
    throw new ParticipantsConnectionError('החלוקה לא נמצאה.')
  }

  if (!response.ok) {
    throw new ParticipantsConnectionError()
  }

  return (await response.json()) as AssignmentParticipants
}
