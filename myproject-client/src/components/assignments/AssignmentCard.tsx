import { Link } from 'react-router-dom'
import type { AssignmentSummary } from '../../services/assignmentService'

interface AssignmentCardProps {
  assignment: AssignmentSummary
}

export function AssignmentCard({ assignment }: AssignmentCardProps) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md">
      <h2 className="text-lg font-semibold text-slate-900">
        {assignment.assignmentName}
      </h2>
      <p className="mt-2 text-sm text-slate-600">
        {assignment.participantCount} משתתפים
      </p>
      <Link
        to={`/assignments/${assignment.assignmentId}`}
        className="mt-4 inline-flex rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-indigo-700"
      >
        פתיחה
      </Link>
    </article>
  )
}
