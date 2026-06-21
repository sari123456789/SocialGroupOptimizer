import { authFetch } from './apiClient'

export interface ExplanationReason {
  code: string
  message: string
  weight: number
}

export interface AlternativeGroupEvaluation {
  groupId: number
  isLegal: boolean
  requiresOverride: boolean
  estimatedParticipantScoreDelta: number
  estimatedAssignmentScoreDelta: number
  reasons: ExplanationReason[]
}

export interface AlternativeSwapEvaluation {
  targetGroupId: number
  swapWithParticipantId: string
  swapWithParticipantDisplayName?: string | null
  isLegal: boolean
  requiresOverride: boolean
  estimatedParticipantScoreDelta: number
  estimatedAssignmentScoreDelta: number
  reasons: ExplanationReason[]
}

export interface ParticipantPlacementExplanation {
  participantId: string
  participantDisplayName?: string | null
  currentGroupId: number
  currentGroupScore: number
  canMove: boolean
  bestAlternativeGroupId?: number | null
  estimatedScoreChange?: number | null
  canSwap: boolean
  bestSwapWithParticipantId?: string | null
  bestSwapWithParticipantDisplayName?: string | null
  bestSwapTargetGroupId?: number | null
  bestSwapScoreChange?: number | null
  reasons: ExplanationReason[]
  alternatives: AlternativeGroupEvaluation[]
  swapAlternatives: AlternativeSwapEvaluation[]
}

export interface ExplanationError {
  errors: string[]
}

export type ExplanationResult =
  | { ok: true; explanation: ParticipantPlacementExplanation }
  | { ok: false; errors: string[] }

/**
 * שולף הסבר שיבוץ עבור משתתף יחיד (Read Only).
 */
export async function fetchParticipantExplanation(
  assignmentId: number,
  participantId: string,
): Promise<ExplanationResult> {
  const response = await authFetch(
    `/api/assignments/${assignmentId}/participants/${encodeURIComponent(
      participantId,
    )}/explanation`,
  )

  if (response.status === 404) {
    return { ok: false, errors: ['החלוקה או המשתתף לא נמצאו.'] }
  }

  if (!response.ok) {
    let errors = ['לא ניתן לטעון את ההסבר.']
    try {
      const data = (await response.json()) as ExplanationError
      if (Array.isArray(data.errors) && data.errors.length > 0) {
        errors = data.errors
      }
    } catch {
      // נשארת הודעת ברירת המחדל.
    }

    return { ok: false, errors }
  }

  const explanation = (await response.json()) as ParticipantPlacementExplanation
  return { ok: true, explanation }
}
