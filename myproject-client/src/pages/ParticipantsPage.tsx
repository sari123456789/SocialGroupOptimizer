import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ParticipantRow } from '../components/participants/ParticipantRow'
import { ParticipantsSidebar } from '../components/participants/ParticipantsSidebar'
import { AppShell } from '../components/layout/AppShell'
import {
  AssignmentsConnectionError,
  fetchAssignments,
  type AssignmentSummary,
} from '../services/assignmentService'
import {
  fetchAssignmentParticipants,
  ParticipantsConnectionError,
  type AssignmentParticipants,
} from '../services/participantsService'
import { getDisplayLabel } from '../utils/participantDisplay'

export function ParticipantsPage() {
  const [assignments, setAssignments] = useState<AssignmentSummary[]>([])
  const [loadingAssignments, setLoadingAssignments] = useState(true)
  const [assignmentsError, setAssignmentsError] = useState<string | null>(null)

  const [selectedAssignmentId, setSelectedAssignmentId] = useState<number | null>(
    null,
  )
  const [participantsData, setParticipantsData] =
    useState<AssignmentParticipants | null>(null)
  const [loadingParticipants, setLoadingParticipants] = useState(false)
  const [participantsError, setParticipantsError] = useState<string | null>(null)

  const [selectedClassificationKey, setSelectedClassificationKey] = useState<
    string | null
  >(null)
  const [searchQuery, setSearchQuery] = useState('')

  const loadAssignments = useCallback(async () => {
    setLoadingAssignments(true)
    setAssignmentsError(null)

    try {
      const list = await fetchAssignments()
      setAssignments(list)
      setSelectedAssignmentId((current) => {
        if (current !== null && list.some((item) => item.assignmentId === current)) {
          return current
        }

        return list.length > 0 ? list[0].assignmentId : null
      })
    } catch (error) {
      if (error instanceof AssignmentsConnectionError) {
        setAssignmentsError(error.message)
      } else {
        setAssignmentsError('לא ניתן לטעון חלוקות.')
      }
    } finally {
      setLoadingAssignments(false)
    }
  }, [])

  useEffect(() => {
    void loadAssignments()
  }, [loadAssignments])

  useEffect(() => {
    if (selectedAssignmentId === null) {
      setParticipantsData(null)
      return
    }

    async function loadParticipants() {
      setLoadingParticipants(true)
      setParticipantsError(null)
      setSelectedClassificationKey(null)
      setSearchQuery('')

      try {
        const data = await fetchAssignmentParticipants(selectedAssignmentId!)
        setParticipantsData(data)
      } catch (error) {
        if (error instanceof ParticipantsConnectionError) {
          setParticipantsError(error.message)
        } else {
          setParticipantsError('לא ניתן לטעון משתתפים.')
        }
        setParticipantsData(null)
      } finally {
        setLoadingParticipants(false)
      }
    }

    void loadParticipants()
  }, [selectedAssignmentId])

  const filteredParticipants = useMemo(() => {
    if (!participantsData) {
      return []
    }

    let items = participantsData.participants

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
  }, [participantsData, selectedClassificationKey, searchQuery])

  return (
    <AppShell wide>
      <div className="flex h-[calc(100vh-4.5rem)] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <section className="flex min-w-0 flex-1 flex-col">
          <div className="border-b border-slate-100 px-4 py-4 sm:px-6">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h1 className="text-xl font-semibold text-slate-900">
                  {participantsData?.assignmentName ?? 'משתתפים'}
                </h1>
                <p className="mt-1 text-sm text-slate-500">
                  {filteredParticipants.length} משתתפים
                </p>
              </div>
              {selectedAssignmentId !== null && (
                <Link
                  to={`/assignments/${selectedAssignmentId}`}
                  className="text-sm font-medium text-indigo-700 hover:underline"
                >
                  פרטי חלוקה
                </Link>
              )}
            </div>

            <label className="relative mt-4 block">
              <span className="pointer-events-none absolute inset-y-0 start-3 flex items-center text-slate-400">
                ⌕
              </span>
              <input
                type="search"
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                placeholder="חיפוש משתתפים"
                className="w-full rounded-full border border-slate-200 bg-slate-50 py-2.5 pe-4 ps-10 text-sm outline-none ring-indigo-200 focus:ring-2"
                disabled={selectedAssignmentId === null}
              />
            </label>
          </div>

          <div className="flex-1 overflow-y-auto">
            {assignmentsError && (
              <p className="m-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
                {assignmentsError}
              </p>
            )}

            {participantsError && (
              <p className="m-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
                {participantsError}
              </p>
            )}

            {loadingParticipants && (
              <div className="space-y-1 p-2">
                {[1, 2, 3, 4, 5].map((item) => (
                  <div key={item} className="h-16 animate-pulse rounded-xl bg-slate-50" />
                ))}
              </div>
            )}

            {!loadingParticipants &&
              selectedAssignmentId !== null &&
              filteredParticipants.length === 0 && (
                <div className="flex h-full items-center justify-center p-8 text-center text-slate-500">
                  לא נמצאו משתתפים.
                </div>
              )}

            {!loadingParticipants &&
              filteredParticipants.map((participant) => (
                <ParticipantRow key={participant.participantId} participant={participant} />
              ))}
          </div>
        </section>

        <ParticipantsSidebar
          assignments={assignments}
          selectedAssignmentId={selectedAssignmentId}
          onSelectAssignment={setSelectedAssignmentId}
          classificationGroups={participantsData?.classificationGroups ?? []}
          selectedClassificationKey={selectedClassificationKey}
          onSelectClassification={setSelectedClassificationKey}
          loadingAssignments={loadingAssignments}
        />
      </div>
    </AppShell>
  )
}
