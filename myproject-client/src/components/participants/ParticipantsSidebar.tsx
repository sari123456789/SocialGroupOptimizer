import { Link } from 'react-router-dom'
import type { AssignmentSummary } from '../../services/assignmentService'
import type { ClassificationGroup } from '../../services/participantsService'

interface ParticipantsSidebarProps {
  assignments: AssignmentSummary[]
  selectedAssignmentId: number | null
  onSelectAssignment: (assignmentId: number) => void
  classificationGroups: ClassificationGroup[]
  selectedClassificationKey: string | null
  onSelectClassification: (key: string | null) => void
  loadingAssignments: boolean
}

function buildClassificationKey(group: ClassificationGroup): string {
  return `${group.dimensionCode}|${group.levelCode}`
}

export function ParticipantsSidebar({
  assignments,
  selectedAssignmentId,
  onSelectAssignment,
  classificationGroups,
  selectedClassificationKey,
  onSelectClassification,
  loadingAssignments,
}: ParticipantsSidebarProps) {
  const groupsByDimension = classificationGroups.reduce<
    Record<string, ClassificationGroup[]>
  >((accumulator, group) => {
    if (!accumulator[group.dimensionCode]) {
      accumulator[group.dimensionCode] = []
    }

    accumulator[group.dimensionCode].push(group)
    return accumulator
  }, {})

  return (
    <aside className="flex h-full w-72 shrink-0 flex-col border-l border-slate-200 bg-white">
      <div className="border-b border-slate-100 p-4">
        <h2 className="text-lg font-semibold text-slate-900">משתתפים</h2>
        <Link
          to="/assignments/new"
          className="mt-3 inline-flex w-full items-center justify-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100"
        >
          <span className="text-lg leading-none">+</span>
          חלוקה חדשה
        </Link>
      </div>

      <div className="flex-1 overflow-y-auto p-2">
        <p className="px-3 py-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
          חלוקות
        </p>

        {loadingAssignments && (
          <div className="space-y-2 px-2">
            {[1, 2, 3].map((item) => (
              <div key={item} className="h-10 animate-pulse rounded-xl bg-slate-100" />
            ))}
          </div>
        )}

        {!loadingAssignments && assignments.length === 0 && (
          <p className="px-3 py-2 text-sm text-slate-500">אין חלוקות עדיין.</p>
        )}

        {!loadingAssignments &&
          assignments.map((assignment) => {
            const isSelected = assignment.assignmentId === selectedAssignmentId

            return (
              <button
                key={assignment.assignmentId}
                type="button"
                onClick={() => onSelectAssignment(assignment.assignmentId)}
                className={`mb-1 flex w-full items-center justify-between rounded-xl px-3 py-2 text-right text-sm transition ${
                  isSelected
                    ? 'bg-indigo-50 font-medium text-indigo-700'
                    : 'text-slate-700 hover:bg-slate-50'
                }`}
              >
                <span className="truncate">{assignment.assignmentName}</span>
                <span className="shrink-0 text-xs text-slate-400">
                  {assignment.participantCount}
                </span>
              </button>
            )
          })}

        {selectedAssignmentId !== null && classificationGroups.length > 0 && (
          <>
            <p className="mt-4 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
              לפי סיווג
            </p>

            <button
              type="button"
              onClick={() => onSelectClassification(null)}
              className={`mb-1 flex w-full rounded-xl px-3 py-2 text-right text-sm transition ${
                selectedClassificationKey === null
                  ? 'bg-indigo-50 font-medium text-indigo-700'
                  : 'text-slate-700 hover:bg-slate-50'
              }`}
            >
              כל המשתתפים
            </button>

            {Object.entries(groupsByDimension).map(([dimensionCode, groups]) => (
              <div key={dimensionCode} className="mb-2">
                <p className="px-3 py-1 text-xs font-medium text-slate-500">
                  {dimensionCode}
                </p>
                {groups.map((group) => {
                  const key = buildClassificationKey(group)
                  const isSelected = selectedClassificationKey === key

                  return (
                    <button
                      key={key}
                      type="button"
                      onClick={() => onSelectClassification(key)}
                      className={`mb-1 flex w-full items-center justify-between rounded-xl px-3 py-2 text-right text-sm transition ${
                        isSelected
                          ? 'bg-indigo-50 font-medium text-indigo-700'
                          : 'text-slate-700 hover:bg-slate-50'
                      }`}
                    >
                      <span className="truncate">{group.levelCode}</span>
                      <span className="shrink-0 text-xs text-slate-400">
                        {group.participantCount}
                      </span>
                    </button>
                  )
                })}
              </div>
            ))}
          </>
        )}
      </div>
    </aside>
  )
}

export { buildClassificationKey }
