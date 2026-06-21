import { authFetch } from './apiClient'
import type { AssignmentDetail } from './assignmentDetailService'

export interface BrokenConstraint {
  constraintType: string
  constraintId?: number | null
  message: string
  severity: string
  canOverride: boolean
}

export interface ManualMovePreview {
  isLegal: boolean
  canOverride: boolean
  requiresOverride: boolean
  scoreBefore: number
  scoreAfter: number
  scoreDelta: number
  brokenConstraints: BrokenConstraint[]
  blockingErrors: string[]
  message: string
}

export interface ManualMoveRequest {
  moveType: 'Transfer' | 'Swap'
  participantId: string
  targetGroupId?: number
  secondParticipantId?: string
}

export type ManualMoveApplyResult =
  | { kind: 'applied'; detail: AssignmentDetail }
  | { kind: 'needsOverride'; preview: ManualMovePreview }
  | { kind: 'blocked'; preview: ManualMovePreview }
  | { kind: 'notFound' }
  | { kind: 'error'; message: string }

/**
 * תצוגה מקדימה של מהלך ידני (Read Only). מחזיר null אם החלוקה לא נמצאה.
 */
export async function previewManualMove(
  assignmentId: number,
  request: ManualMoveRequest,
): Promise<ManualMovePreview | null> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/manual-move/preview`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    },
  )

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error('לא ניתן לבדוק את השינוי.')
  }

  return (await response.json()) as ManualMovePreview
}

/**
 * ביצוע מהלך ידני. דורש אישור חריגה מפורש כדי לשבור אילוצים עסקיים.
 */
export async function applyManualMove(
  assignmentId: number,
  request: ManualMoveRequest,
  overrideConfirmed: boolean,
): Promise<ManualMoveApplyResult> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/manual-move/apply`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ move: request, overrideConfirmed }),
    },
  )

  if (response.status === 404) {
    return { kind: 'notFound' }
  }

  if (response.ok) {
    const detail = (await response.json()) as AssignmentDetail
    return { kind: 'applied', detail }
  }

  if (response.status === 409) {
    const preview = (await response.json()) as ManualMovePreview
    return { kind: 'needsOverride', preview }
  }

  if (response.status === 400) {
    const preview = (await response.json()) as ManualMovePreview
    return { kind: 'blocked', preview }
  }

  return { kind: 'error', message: 'לא ניתן לבצע את השינוי.' }
}
