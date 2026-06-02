import type { AssignmentSummary } from '../../services/assignmentService'
import { AssignmentCard } from './AssignmentCard'

interface AssignmentListProps {
  assignments: AssignmentSummary[]
}

export function AssignmentList({ assignments }: AssignmentListProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {assignments.map((assignment) => (
        <AssignmentCard key={assignment.assignmentId} assignment={assignment} />
      ))}
    </div>
  )
}
