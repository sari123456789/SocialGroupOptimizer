import { authFetch } from './apiClient'
import type { ParticipantListItem } from './participantsService'

export interface AssignmentSettings {
  minGroups: number
  maxGroups: number
  minGroupSize: number
  maxGroupSize: number
}

export interface PairConstraintItem {
  constraintId: number
  participantA: string
  participantB: string
}

export interface ClassificationConstraintItem {
  dimensionCode: string
  ruleType: string
}

export interface PlacementGroup {
  groupId: number
  participantIds: string[]
}

export interface AssignmentDetail {
  assignmentId: number
  assignmentName: string
  status: string
  participantCount: number
  lastPlacementStatus?: string | null
  validationErrors: string[]
  placementGroups: PlacementGroup[]
  placementScore?: number | null
  initialPlacementScore?: number | null
  settings: AssignmentSettings
  participants: ParticipantListItem[]
  mandatoryPairs: PairConstraintItem[]
  forbiddenPairs: PairConstraintItem[]
  classificationConstraints: ClassificationConstraintItem[]
  availableDimensions: string[]
}

export interface AssignmentEditError {
  success: false
  errors: string[]
}

async function parseEditResponse(
  response: Response,
): Promise<AssignmentDetail | AssignmentEditError> {
  const data = (await response.json()) as AssignmentDetail | AssignmentEditError

  if (!response.ok) {
    if ('errors' in data && Array.isArray(data.errors)) {
      return data as AssignmentEditError
    }

    return { success: false, errors: ['הפעולה נכשלה.'] }
  }

  return data as AssignmentDetail
}

export async function fetchAssignmentDetail(
  assignmentId: number,
): Promise<AssignmentDetail | null> {
  const response = await authFetch(`/api/assignments/${assignmentId}`)

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error('לא ניתן לטעון את החלוקה.')
  }

  return (await response.json()) as AssignmentDetail
}

export async function updateAssignment(
  assignmentId: number,
  payload: {
    assignmentName?: string
    settings?: AssignmentSettings
  },
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(`/api/assignments/${assignmentId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })

  return parseEditResponse(response)
}

export async function addParticipant(
  assignmentId: number,
  payload: {
    participantId: string
    displayName?: string
    classifications: Record<string, string>
    preferences?: string[]
  },
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(`/api/assignments/${assignmentId}/participants`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })

  return parseEditResponse(response)
}

export async function updateParticipant(
  assignmentId: number,
  identity: string,
  payload: {
    displayName?: string
    classifications?: Record<string, string>
    preferences?: string[]
  },
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/participants/${identity}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    },
  )

  return parseEditResponse(response)
}

export async function deleteParticipant(
  assignmentId: number,
  identity: string,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/participants/${identity}`,
    { method: 'DELETE' },
  )

  return parseEditResponse(response)
}

export async function addMandatoryPair(
  assignmentId: number,
  participantA: string,
  participantB: string,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/constraints/mandatory-pairs`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ participantA, participantB }),
    },
  )

  return parseEditResponse(response)
}

export async function deleteMandatoryPair(
  assignmentId: number,
  constraintId: number,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/constraints/mandatory-pairs/${constraintId}`,
    { method: 'DELETE' },
  )

  return parseEditResponse(response)
}

export async function addForbiddenPair(
  assignmentId: number,
  participantA: string,
  participantB: string,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/constraints/forbidden-pairs`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ participantA, participantB }),
    },
  )

  return parseEditResponse(response)
}

export async function deleteForbiddenPair(
  assignmentId: number,
  constraintId: number,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/constraints/forbidden-pairs/${constraintId}`,
    { method: 'DELETE' },
  )

  return parseEditResponse(response)
}

export async function addClassificationConstraint(
  assignmentId: number,
  dimensionCode: string,
  ruleType: string,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/constraints/classification`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dimensionCode, ruleType }),
    },
  )

  return parseEditResponse(response)
}

export async function deleteClassificationConstraint(
  assignmentId: number,
  dimensionCode: string,
): Promise<AssignmentDetail | AssignmentEditError> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/constraints/classification/${encodeURIComponent(dimensionCode)}`,
    { method: 'DELETE' },
  )

  return parseEditResponse(response)
}

export function isEditError(
  result: AssignmentDetail | AssignmentEditError,
): result is AssignmentEditError {
  return 'success' in result && result.success === false
}

export async function validateAssignment(
  assignmentId: number,
): Promise<AssignmentDetail | null> {
  const response = await authFetch(`/api/assignments/${assignmentId}/validate`, {
    method: 'POST',
  })

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error('לא ניתן לאמת את החלוקה.')
  }

  return (await response.json()) as AssignmentDetail
}

export function statusLabel(status: string): string {
  if (status === 'PendingValidation') {
    return 'ממתין לאימות'
  }

  if (status === 'Validated') {
    return 'הנתונים תקינים — ניתן לחלוק'
  }

  if (status === 'ValidationFailed') {
    return 'אימות נכשל'
  }

  if (status === 'PendingPlacement') {
    return 'ממתינים לחלוקה'
  }

  return status
}

export function statusBadgeClass(status: string): string {
  if (status === 'Validated') {
    return 'bg-green-100 text-green-800'
  }

  if (status === 'ValidationFailed') {
    return 'bg-red-100 text-red-800'
  }

  return 'bg-amber-100 text-amber-800'
}

export function ruleTypeLabel(ruleType: string): string {
  if (ruleType === 'Balance') {
    return 'איזון'
  }

  if (ruleType === 'Separation') {
    return 'הפרדה'
  }

  return ruleType
}
