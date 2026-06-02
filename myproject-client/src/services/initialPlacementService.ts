import { authFetch } from './apiClient'

export interface PlacementGroup {
  groupId: number
  participantIds: string[]
}

export interface InitialPlacementResponse {
  status: string
  groups: PlacementGroup[]
  errors: string[]
}

export class PlacementConnectionError extends Error {
  constructor(message = 'לא ניתן להתחבר לשרת') {
    super(message)
    this.name = 'PlacementConnectionError'
  }
}

export async function createInitialPlacement(
  assignmentId: number,
): Promise<InitialPlacementResponse> {
  const response = await authFetch(
    `/api/placement/initial/assignment/${assignmentId}`,
    {
      method: 'POST',
    },
  )

  const data = (await response.json()) as InitialPlacementResponse & {
    errors?: string[]
  }

  if (!response.ok) {
    return {
      status: data.status ?? `HTTP ${response.status}`,
      groups: data.groups ?? [],
      errors:
        data.errors && data.errors.length > 0
          ? data.errors
          : [`השרת החזיר שגיאה (${response.status})`],
    }
  }

  return {
    status: data.status,
    groups: data.groups ?? [],
    errors: data.errors ?? [],
  }
}
