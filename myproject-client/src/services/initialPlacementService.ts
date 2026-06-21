export interface PlacementGroup {
  groupId: number
  participantIds: string[]
}

export interface InitialPlacementResponse {
  status: string
  groups: PlacementGroup[]
  errors: string[]
}
