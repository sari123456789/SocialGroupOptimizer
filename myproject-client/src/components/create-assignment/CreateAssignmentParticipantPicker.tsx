import { useCallback, useEffect, useMemo, useState } from 'react'
import { buildClassificationKey } from '../participants/ParticipantsSidebar'
import type { AssignmentSummary } from '../../services/assignmentService'
import {
  fetchAssignmentParticipants,
  ParticipantsConnectionError,
  type AssignmentParticipants,
  type ParticipantListItem,
} from '../../services/participantsService'
import { getDisplayLabel } from '../../utils/participantDisplay'
import { SelectableParticipantRow } from './SelectableParticipantRow'

export interface SelectedParticipantPayload {
  participantId: string
  displayName?: string | null
  classifications: Record<string, string>
}

interface CreateAssignmentParticipantPickerProps {
  assignments: AssignmentSummary[]
  loadingAssignments: boolean
  selectedParticipantIds: Set<string>
  onSelectedParticipantIdsChange: (next: Set<string>) => void
  participantDetailsById: Map<string, SelectedParticipantPayload>
  onParticipantDetailsChange: (next: Map<string, SelectedParticipantPayload>) => void
}

function buildClassificationSelectionKey(assignmentId: number, classificationKey: string): string {
  return `${assignmentId}|${classificationKey}`
}

export function CreateAssignmentParticipantPicker({
  assignments,
  loadingAssignments,
  selectedParticipantIds,
  onSelectedParticipantIdsChange,
  participantDetailsById,
  onParticipantDetailsChange,
}: CreateAssignmentParticipantPickerProps) {
  const [viewAssignmentId, setViewAssignmentId] = useState<number | null>(null)
  const [participantsCache, setParticipantsCache] = useState<
    Record<number, AssignmentParticipants>
  >({})
  const [loadingAssignmentId, setLoadingAssignmentId] = useState<number | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [checkedAssignmentIds, setCheckedAssignmentIds] = useState<Set<number>>(new Set())
  const [checkedClassificationKeys, setCheckedClassificationKeys] = useState<Set<string>>(
    new Set(),
  )
  const [selectedClassificationKey, setSelectedClassificationKey] = useState<string | null>(
    null,
  )
  const [searchQuery, setSearchQuery] = useState('')

  useEffect(() => {
    if (viewAssignmentId !== null) {
      return
    }

    if (assignments.length > 0) {
      setViewAssignmentId(assignments[0].assignmentId)
    }
  }, [assignments, viewAssignmentId])

  const ensureParticipantsLoaded = useCallback(
    async (assignmentId: number): Promise<AssignmentParticipants | null> => {
      const cached = participantsCache[assignmentId]
      if (cached) {
        return cached
      }

      setLoadingAssignmentId(assignmentId)
      setLoadError(null)

      try {
        const data = await fetchAssignmentParticipants(assignmentId)
        setParticipantsCache((current) => ({ ...current, [assignmentId]: data }))
        return data
      } catch (error) {
        if (error instanceof ParticipantsConnectionError) {
          setLoadError(error.message)
        } else {
          setLoadError('לא ניתן לטעון משתתפים.')
        }

        return null
      } finally {
        setLoadingAssignmentId(null)
      }
    },
    [participantsCache],
  )

  useEffect(() => {
    if (viewAssignmentId === null) {
      return
    }

    void ensureParticipantsLoaded(viewAssignmentId)
  }, [viewAssignmentId, ensureParticipantsLoaded])

  const mergeParticipantsIntoSelection = useCallback(
    (participants: ParticipantListItem[]) => {
      const nextIds = new Set(selectedParticipantIds)
      const nextDetails = new Map(participantDetailsById)

      for (const participant of participants) {
        nextIds.add(participant.participantId)
        nextDetails.set(participant.participantId, {
          participantId: participant.participantId,
          displayName: participant.displayName,
          classifications: { ...participant.classifications },
        })
      }

      onSelectedParticipantIdsChange(nextIds)
      onParticipantDetailsChange(nextDetails)
    },
    [
      onParticipantDetailsChange,
      onSelectedParticipantIdsChange,
      participantDetailsById,
      selectedParticipantIds,
    ],
  )

  const removeParticipantsFromSelection = useCallback(
    (participants: ParticipantListItem[]) => {
      const nextIds = new Set(selectedParticipantIds)
      const nextDetails = new Map(participantDetailsById)

      for (const participant of participants) {
        nextIds.delete(participant.participantId)
        nextDetails.delete(participant.participantId)
      }

      onSelectedParticipantIdsChange(nextIds)
      onParticipantDetailsChange(nextDetails)
    },
    [
      onParticipantDetailsChange,
      onSelectedParticipantIdsChange,
      participantDetailsById,
      selectedParticipantIds,
    ],
  )

  const toggleParticipant = useCallback(
    (participantId: string) => {
      const nextIds = new Set(selectedParticipantIds)
      const nextDetails = new Map(participantDetailsById)

      if (nextIds.has(participantId)) {
        nextIds.delete(participantId)
        nextDetails.delete(participantId)
      } else {
        const currentData = viewAssignmentId
          ? participantsCache[viewAssignmentId]
          : null
        const participant = currentData?.participants.find(
          (entry) => entry.participantId === participantId,
        )

        if (participant) {
          nextIds.add(participantId)
          nextDetails.set(participantId, {
            participantId: participant.participantId,
            displayName: participant.displayName,
            classifications: { ...participant.classifications },
          })
        }
      }

      onSelectedParticipantIdsChange(nextIds)
      onParticipantDetailsChange(nextDetails)
    },
    [
      onParticipantDetailsChange,
      onSelectedParticipantIdsChange,
      participantDetailsById,
      participantsCache,
      selectedParticipantIds,
      viewAssignmentId,
    ],
  )

  const toggleAssignmentSelection = useCallback(
    async (assignmentId: number) => {
      const data = await ensureParticipantsLoaded(assignmentId)
      if (!data) {
        return
      }

      const nextChecked = new Set(checkedAssignmentIds)
      if (nextChecked.has(assignmentId)) {
        nextChecked.delete(assignmentId)
        setCheckedAssignmentIds(nextChecked)
        removeParticipantsFromSelection(data.participants)
      } else {
        nextChecked.add(assignmentId)
        setCheckedAssignmentIds(nextChecked)
        mergeParticipantsIntoSelection(data.participants)
      }
    },
    [
      checkedAssignmentIds,
      ensureParticipantsLoaded,
      mergeParticipantsIntoSelection,
      removeParticipantsFromSelection,
    ],
  )

  const toggleClassificationSelection = useCallback(
    async (assignmentId: number, classificationKey: string) => {
      const data = await ensureParticipantsLoaded(assignmentId)
      if (!data) {
        return
      }

      const selectionKey = buildClassificationSelectionKey(assignmentId, classificationKey)
      const [dimensionCode, levelCode] = classificationKey.split('|')
      const matching = data.participants.filter(
        (participant) => participant.classifications[dimensionCode] === levelCode,
      )

      const nextChecked = new Set(checkedClassificationKeys)
      if (nextChecked.has(selectionKey)) {
        nextChecked.delete(selectionKey)
        setCheckedClassificationKeys(nextChecked)
        removeParticipantsFromSelection(matching)
      } else {
        nextChecked.add(selectionKey)
        setCheckedClassificationKeys(nextChecked)
        mergeParticipantsIntoSelection(matching)
      }
    },
    [
      checkedClassificationKeys,
      ensureParticipantsLoaded,
      mergeParticipantsIntoSelection,
      removeParticipantsFromSelection,
    ],
  )

  const currentParticipants = viewAssignmentId
    ? participantsCache[viewAssignmentId]
    : null

  const filteredParticipants = useMemo(() => {
    if (!currentParticipants) {
      return []
    }

    let items = currentParticipants.participants

    if (selectedClassificationKey) {
      const [dimensionCode, levelCode] = selectedClassificationKey.split('|')
      items = items.filter(
        (participant) => participant.classifications[dimensionCode] === levelCode,
      )
    }

    const normalizedQuery = searchQuery.trim().toLowerCase()
    if (!normalizedQuery) {
      return items
    }

    return items.filter((participant) => {
      const label = getDisplayLabel(
        participant.displayName,
        participant.participantId,
      ).toLowerCase()

      return (
        label.includes(normalizedQuery) ||
        participant.participantId.includes(normalizedQuery)
      )
    })
  }, [currentParticipants, searchQuery, selectedClassificationKey])

  const groupsByDimension = useMemo(() => {
    const groups = currentParticipants?.classificationGroups ?? []
    return groups.reduce<Record<string, typeof groups>>((accumulator, group) => {
      if (!accumulator[group.dimensionCode]) {
        accumulator[group.dimensionCode] = []
      }

      accumulator[group.dimensionCode].push(group)
      return accumulator
    }, {})
  }, [currentParticipants])

  const allVisibleSelected =
    filteredParticipants.length > 0 &&
    filteredParticipants.every((participant) =>
      selectedParticipantIds.has(participant.participantId),
    )

  const toggleAllVisible = () => {
    if (allVisibleSelected) {
      removeParticipantsFromSelection(filteredParticipants)
    } else {
      mergeParticipantsIntoSelection(filteredParticipants)
    }
  }

  return (
    <div className="flex h-[28rem] overflow-hidden rounded-2xl border border-slate-200">
      <section className="flex min-w-0 flex-1 flex-col">
        <div className="border-b border-slate-100 px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div>
              <h2 className="text-base font-semibold text-slate-900">
                {currentParticipants?.assignmentName ?? 'בחירת משתתפים'}
              </h2>
              <p className="text-sm text-slate-500">
                {selectedParticipantIds.size} נבחרו · {filteredParticipants.length} מוצגים
              </p>
            </div>
            {filteredParticipants.length > 0 && (
              <button
                type="button"
                onClick={toggleAllVisible}
                className="text-sm font-medium text-indigo-700 hover:underline"
              >
                {allVisibleSelected ? 'ביטול הכל ברשימה' : 'סימון הכל ברשימה'}
              </button>
            )}
          </div>

          <label className="relative mt-3 block">
            <span className="pointer-events-none absolute inset-y-0 start-3 flex items-center text-slate-400">
              ⌕
            </span>
            <input
              type="search"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="חיפוש משתתפים"
              className="w-full rounded-full border border-slate-200 bg-slate-50 py-2 pe-4 ps-10 text-sm outline-none ring-indigo-200 focus:ring-2"
              disabled={viewAssignmentId === null}
            />
          </label>
        </div>

        <div className="flex-1 overflow-y-auto">
          {loadError && (
            <p className="m-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
              {loadError}
            </p>
          )}

          {loadingAssignmentId === viewAssignmentId && (
            <div className="space-y-1 p-2">
              {[1, 2, 3, 4].map((item) => (
                <div key={item} className="h-14 animate-pulse rounded-xl bg-slate-50" />
              ))}
            </div>
          )}

          {!loadError &&
            loadingAssignmentId !== viewAssignmentId &&
            filteredParticipants.map((participant) => (
              <SelectableParticipantRow
                key={participant.participantId}
                participant={participant}
                selected={selectedParticipantIds.has(participant.participantId)}
                onToggle={toggleParticipant}
              />
            ))}
        </div>
      </section>

      <aside className="flex h-full w-72 shrink-0 flex-col border-l border-slate-200 bg-white">
        <div className="border-b border-slate-100 p-3">
          <h3 className="text-sm font-semibold text-slate-900">מקורות משתתפים</h3>
          <p className="mt-1 text-xs text-slate-500">סמני חלוקה שלמה או סיווג</p>
        </div>

        <div className="flex-1 overflow-y-auto p-2">
          <p className="px-2 py-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
            חלוקות
          </p>

          {loadingAssignments && (
            <div className="space-y-2 px-2">
              {[1, 2, 3].map((item) => (
                <div key={item} className="h-10 animate-pulse rounded-xl bg-slate-100" />
              ))}
            </div>
          )}

          {!loadingAssignments &&
            assignments.map((assignment) => {
              const isViewing = assignment.assignmentId === viewAssignmentId
              const isChecked = checkedAssignmentIds.has(assignment.assignmentId)

              return (
                <div
                  key={assignment.assignmentId}
                  className={`mb-1 flex items-center gap-2 rounded-xl px-2 py-1.5 ${
                    isViewing ? 'bg-indigo-50' : 'hover:bg-slate-50'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={isChecked}
                    onChange={() => void toggleAssignmentSelection(assignment.assignmentId)}
                    className="h-4 w-4 shrink-0 rounded border-slate-300 text-indigo-600"
                    aria-label={`בחירת כל המשתתפים מ-${assignment.assignmentName}`}
                  />
                  <button
                    type="button"
                    onClick={() => {
                      setViewAssignmentId(assignment.assignmentId)
                      setSelectedClassificationKey(null)
                      setSearchQuery('')
                    }}
                    className={`flex min-w-0 flex-1 items-center justify-between text-right text-sm ${
                      isViewing ? 'font-medium text-indigo-700' : 'text-slate-700'
                    }`}
                  >
                    <span className="truncate">{assignment.assignmentName}</span>
                    <span className="shrink-0 text-xs text-slate-400">
                      {assignment.participantCount}
                    </span>
                  </button>
                </div>
              )
            })}

          {viewAssignmentId !== null &&
            (currentParticipants?.classificationGroups.length ?? 0) > 0 && (
              <>
                <p className="mt-4 px-2 py-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
                  לפי סיווג
                </p>

                <div className="mb-1 flex items-center gap-2 rounded-xl px-2 py-1.5">
                  <input
                    type="checkbox"
                    checked={selectedClassificationKey === null && allVisibleSelected}
                    onChange={() => {
                      if (currentParticipants) {
                        if (allVisibleSelected) {
                          removeParticipantsFromSelection(currentParticipants.participants)
                        } else {
                          mergeParticipantsIntoSelection(currentParticipants.participants)
                        }
                      }
                    }}
                    className="h-4 w-4 shrink-0 rounded border-slate-300 text-indigo-600"
                  />
                  <button
                    type="button"
                    onClick={() => setSelectedClassificationKey(null)}
                    className={`flex-1 text-right text-sm ${
                      selectedClassificationKey === null
                        ? 'font-medium text-indigo-700'
                        : 'text-slate-700'
                    }`}
                  >
                    כל המשתתפים
                  </button>
                </div>

                {Object.entries(groupsByDimension).map(([dimensionCode, groups]) => (
                  <div key={dimensionCode} className="mb-2">
                    <p className="px-2 py-1 text-xs font-medium text-slate-500">
                      {dimensionCode}
                    </p>
                    {groups.map((group) => {
                      const key = buildClassificationKey(group)
                      const selectionKey = buildClassificationSelectionKey(
                        viewAssignmentId,
                        key,
                      )
                      const isChecked = checkedClassificationKeys.has(selectionKey)
                      const isViewing = selectedClassificationKey === key

                      return (
                        <div
                          key={key}
                          className={`mb-1 flex items-center gap-2 rounded-xl px-2 py-1.5 ${
                            isViewing ? 'bg-indigo-50' : 'hover:bg-slate-50'
                          }`}
                        >
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() =>
                              void toggleClassificationSelection(viewAssignmentId, key)
                            }
                            className="h-4 w-4 shrink-0 rounded border-slate-300 text-indigo-600"
                          />
                          <button
                            type="button"
                            onClick={() => setSelectedClassificationKey(key)}
                            className={`flex min-w-0 flex-1 items-center justify-between text-right text-sm ${
                              isViewing ? 'font-medium text-indigo-700' : 'text-slate-700'
                            }`}
                          >
                            <span className="truncate">{group.levelCode}</span>
                            <span className="shrink-0 text-xs text-slate-400">
                              {group.participantCount}
                            </span>
                          </button>
                        </div>
                      )
                    })}
                  </div>
                ))}
              </>
            )}
        </div>
      </aside>
    </div>
  )
}
