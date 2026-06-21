import { authFetch, SessionExpiredError } from './apiClient'

export interface AssignmentSummary {
  assignmentId: number
  assignmentName: string
  participantCount: number
}

export interface AssignmentImportResponse {
  success: boolean
  assignmentId?: number
  assignmentName?: string
  participantCount: number
  mandatoryPairCount: number
  forbiddenPairCount: number
  classificationRuleCount: number
  errors: string[]
}

export class AssignmentsConnectionError extends Error {
  constructor(message = 'לא ניתן להתחבר לשרת') {
    super(message)
    this.name = 'AssignmentsConnectionError'
  }
}

export async function fetchAssignments(): Promise<AssignmentSummary[]> {
  let response: Response

  try {
    response = await authFetch('/api/assignments')
  } catch (error) {
    if (error instanceof Error && error.message === 'NETWORK_ERROR') {
      throw new AssignmentsConnectionError(
        'לא ניתן להתחבר לשרת. ודאי שה-API רץ בפרופיל https (localhost:7044) וש-Vite רץ (npm run dev).',
      )
    }

    throw new AssignmentsConnectionError()
  }

  if (!response.ok) {
    throw new AssignmentsConnectionError(
      `השרת החזיר שגיאה (${response.status}).`,
    )
  }

  return (await response.json()) as AssignmentSummary[]
}

export async function downloadImportTemplate(): Promise<void> {
  const response = await authFetch('/api/assignments/template')

  if (!response.ok) {
    throw new AssignmentsConnectionError('לא ניתן להוריד את התבנית.')
  }

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = 'participants-template.xlsx'
  link.click()
  URL.revokeObjectURL(url)
}

export async function importAssignmentFromExcel(
  file: File,
  options?: {
    assignmentName?: string
    groupCount?: number
    minGroupSize?: number
    maxGroupSize?: number
  },
): Promise<AssignmentImportResponse> {
  const formData = new FormData()
  formData.append('file', file)

  if (options?.assignmentName) {
    formData.append('assignmentName', options.assignmentName)
  }

  if (options?.groupCount !== undefined) {
    formData.append('groupCount', String(options.groupCount))
  }

  if (options?.minGroupSize !== undefined) {
    formData.append('minGroupSize', String(options.minGroupSize))
  }

  if (options?.maxGroupSize !== undefined) {
    formData.append('maxGroupSize', String(options.maxGroupSize))
  }

  let response: Response

  try {
    response = await authFetch('/api/assignments/import', {
      method: 'POST',
      body: formData,
    })
  } catch (error) {
    if (error instanceof SessionExpiredError) {
      return {
        success: false,
        participantCount: 0,
        mandatoryPairCount: 0,
        forbiddenPairCount: 0,
        classificationRuleCount: 0,
        errors: ['נדרשת התחברות מחדש.'],
      }
    }

    if (error instanceof Error && error.message === 'NETWORK_ERROR') {
      return {
        success: false,
        participantCount: 0,
        mandatoryPairCount: 0,
        forbiddenPairCount: 0,
        classificationRuleCount: 0,
        errors: ['לא ניתן להתחבר לשרת. ודאי שה-API רץ בפרופיל https (localhost:7044).'],
      }
    }

    throw error
  }

  let data: AssignmentImportResponse
  try {
    data = (await response.json()) as AssignmentImportResponse
  } catch {
    return {
      success: false,
      participantCount: 0,
      mandatoryPairCount: 0,
      forbiddenPairCount: 0,
      classificationRuleCount: 0,
      errors: [`השרת החזיר שגיאה (${response.status})`],
    }
  }

  if (!response.ok) {
    return {
      success: false,
      participantCount: data.participantCount ?? 0,
      mandatoryPairCount: data.mandatoryPairCount ?? 0,
      forbiddenPairCount: data.forbiddenPairCount ?? 0,
      classificationRuleCount: data.classificationRuleCount ?? 0,
      errors:
        data.errors?.length > 0
          ? data.errors
          : [`השרת החזיר שגיאה (${response.status})`],
    }
  }

  return { ...data, success: data.success ?? true }
}

export interface CreateFromParticipantsPayload {
  assignmentName: string
  groupCount: number
  minGroupSize: number
  maxGroupSize: number
  participants: Array<{
    participantId: string
    displayName?: string | null
    classifications: Record<string, string>
    preferences?: string[]
  }>
}

export async function createAssignmentFromParticipants(
  payload: CreateFromParticipantsPayload,
): Promise<AssignmentImportResponse> {
  let response: Response

  try {
    response = await authFetch('/api/assignments/create-from-participants', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })
  } catch (error) {
    if (error instanceof Error && error.message === 'NETWORK_ERROR') {
      return {
        success: false,
        participantCount: 0,
        mandatoryPairCount: 0,
        forbiddenPairCount: 0,
        classificationRuleCount: 0,
        errors: ['לא ניתן להתחבר לשרת. ודאי שה-API רץ.'],
      }
    }

    throw error
  }

  let data: AssignmentImportResponse
  try {
    data = (await response.json()) as AssignmentImportResponse
  } catch {
    return {
      success: false,
      participantCount: 0,
      mandatoryPairCount: 0,
      forbiddenPairCount: 0,
      classificationRuleCount: 0,
      errors: [`השרת החזיר שגיאה (${response.status})`],
    }
  }

  if (!response.ok) {
    return {
      success: false,
      participantCount: data.participantCount ?? 0,
      mandatoryPairCount: data.mandatoryPairCount ?? 0,
      forbiddenPairCount: data.forbiddenPairCount ?? 0,
      classificationRuleCount: data.classificationRuleCount ?? 0,
      errors:
        data.errors?.length > 0
          ? data.errors
          : [`השרת החזיר שגיאה (${response.status})`],
    }
  }

  return { ...data, success: data.success ?? true }
}
