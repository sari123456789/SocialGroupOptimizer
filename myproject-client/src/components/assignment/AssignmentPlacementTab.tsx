import { PlacementResult } from '../placement/PlacementResult'
import type { InitialPlacementResponse } from '../../services/initialPlacementService'

interface AssignmentPlacementTabProps {
  participantCount: number
  running: boolean
  error: string | null
  result: InitialPlacementResponse | null
  onRun: () => void
}

export function AssignmentPlacementTab({
  participantCount,
  running,
  error,
  result,
  onRun,
}: AssignmentPlacementTabProps) {
  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-600">
        לאחר שכל המשתתפים והאילוצים מוכנים, ניתן להריץ חלוקה ראשונית.
        כרגע רשומים {participantCount} משתתפים.
      </p>

      <button
        type="button"
        onClick={onRun}
        disabled={running || participantCount === 0}
        className="rounded-xl bg-indigo-600 px-5 py-3 font-medium text-white disabled:opacity-60"
      >
        {running ? 'מריץ חלוקה...' : 'הרצת חלוקה ראשונית'}
      </button>

      {error && (
        <p className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
          {error}
        </p>
      )}

      {result && <PlacementResult result={result} />}
    </div>
  )
}
