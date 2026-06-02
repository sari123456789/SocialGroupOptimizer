import type { InitialPlacementResponse } from '../../services/initialPlacementService'

export function PlacementResult({ result }: { result: InitialPlacementResponse }) {
  return (
    <div className="mt-6 space-y-4">
      <p className="rounded-lg bg-slate-50 px-4 py-3 text-sm">
        <span className="font-medium">סטטוס:</span> {result.status}
      </p>

      {result.groups.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2">
          {result.groups.map((group) => (
            <article
              key={group.groupId}
              className="rounded-xl border border-slate-200 bg-white p-4"
            >
              <h3 className="font-semibold text-slate-900">
                קבוצה {group.groupId}
              </h3>
              <ul className="mt-2 space-y-1 text-sm text-slate-700">
                {group.participantIds.map((participantId) => (
                  <li key={participantId}>{participantId}</li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      )}

      {result.errors.length > 0 && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4" role="alert">
          <h3 className="font-semibold text-red-800">שגיאות</h3>
          <ul className="mt-2 list-disc space-y-1 pr-5 text-sm text-red-700">
            {result.errors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
