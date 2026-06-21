import { PlacementResult } from '../placement/PlacementResult'
import type { AssignmentDetail } from '../../services/assignmentDetailService'
import {
  statusBadgeClass,
  statusLabel,
} from '../../services/assignmentDetailService'

interface AssignmentOverviewTabProps {
  detail: AssignmentDetail
  editing: boolean
  validating: boolean
  assignmentName: string
  minGroups: string
  maxGroups: string
  minGroupSize: string
  maxGroupSize: string
  saving: boolean
  onStartEdit: () => void
  onCancelEdit: () => void
  onSave: () => void
  onValidate: () => void
  onAssignmentNameChange: (value: string) => void
  onMinGroupsChange: (value: string) => void
  onMaxGroupsChange: (value: string) => void
  onMinGroupSizeChange: (value: string) => void
  onMaxGroupSizeChange: (value: string) => void
  onExplainParticipant?: (participantId: string) => void
  onManualMove?: () => void
}

export function AssignmentOverviewTab({
  detail,
  editing,
  validating,
  assignmentName,
  minGroups,
  maxGroups,
  minGroupSize,
  maxGroupSize,
  saving,
  onStartEdit,
  onCancelEdit,
  onSave,
  onValidate,
  onAssignmentNameChange,
  onMinGroupsChange,
  onMaxGroupsChange,
  onMinGroupSizeChange,
  onMaxGroupSizeChange,
  onExplainParticipant,
  onManualMove,
}: AssignmentOverviewTabProps) {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3">
        <span
          className={`rounded-full px-3 py-1 text-sm font-medium ${statusBadgeClass(detail.status)}`}
        >
          {statusLabel(detail.status)}
        </span>
        <span className="text-sm text-slate-500">
          {detail.participantCount} משתתפים
        </span>
      </div>

      <section className="rounded-xl border border-slate-200 bg-slate-50 p-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-semibold text-slate-900">אימות חלוקה ראשונית</h2>
            <p className="mt-1 text-sm text-slate-600">
              בדיקה אוטומטית האם הנתונים והאילוצים מאפשרים חלוקה חוקית.
              אילוצי סיווג אינם חובה — ניתן לאמת גם רק עם אילוצי קבוצות וזוגות.
              אילוצי סיווג (אופציונלי) מוסיפים בטאב &quot;אילוצים&quot;.
            </p>
          </div>
          <button
            type="button"
            onClick={onValidate}
            disabled={validating || saving}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            {validating ? 'מאמת...' : 'אימות מחדש'}
          </button>
        </div>

        {detail.status === 'PendingValidation' && (
          <p className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            טרם בוצע אימות. לחצי &quot;אימות מחדש&quot; לבדיקת החלוקה.
          </p>
        )}

        {detail.status === 'Validated' && detail.placementGroups.length > 0 && (
          <div className="mt-4">
            {onManualMove && (
              <div className="mb-3 flex justify-end">
                <button
                  type="button"
                  onClick={onManualMove}
                  className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
                >
                  שינוי ידני
                </button>
              </div>
            )}
            {detail.placementScore != null && (
              <p className="mb-3 text-sm text-slate-700">
                ציון חלוקה:{' '}
                <strong>{detail.placementScore.toFixed(1)}%</strong>
                {detail.initialPlacementScore != null && (
                  <span className="text-slate-600">
                    {' '}
                    (לפני שיפור: {detail.initialPlacementScore.toFixed(1)}%, אחרי שיפור: {detail.placementScore.toFixed(1)}%)
                  </span>
                )}
              </p>
            )}
            <PlacementResult
              result={{
                status: detail.lastPlacementStatus ?? 'Success',
                groups: detail.placementGroups,
                errors: [],
              }}
              participants={detail.participants}
              onExplain={onExplainParticipant}
            />
          </div>
        )}

        {detail.validationErrors.length > 0 && (
          <div className="mt-4 rounded-xl border border-red-200 bg-red-50 p-4" role="alert">
            <h3 className="font-semibold text-red-800">בעיות באימות</h3>
            <ul className="mt-2 list-disc space-y-1 pr-5 text-sm text-red-700">
              {detail.validationErrors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          </div>
        )}
      </section>

      {!editing && (
        <div className="grid gap-4 sm:grid-cols-2">
          <InfoCard label="שם חלוקה" value={detail.assignmentName} />
          <InfoCard
            label="מספר קבוצות"
            value={`${detail.settings.minGroups} – ${detail.settings.maxGroups}`}
          />
          <InfoCard
            label="גודל קבוצה"
            value={`${detail.settings.minGroupSize} – ${detail.settings.maxGroupSize}`}
          />
          <InfoCard
            label="זוגות חובה"
            value={String(detail.mandatoryPairs.length)}
          />
          <InfoCard
            label="זוגות איסור"
            value={String(detail.forbiddenPairs.length)}
          />
          <InfoCard
            label="אילוצי סיווג"
            value={String(detail.classificationConstraints.length)}
          />
        </div>
      )}

      {editing && (
        <form
          className="grid gap-4 sm:grid-cols-2"
          onSubmit={(event) => {
            event.preventDefault()
            onSave()
          }}
        >
          <label className="block sm:col-span-2">
            <span className="text-sm font-medium text-slate-700">שם חלוקה</span>
            <input
              type="text"
              value={assignmentName}
              onChange={(event) => onAssignmentNameChange(event.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </label>
          <NumberField label="קבוצות מינימום" value={minGroups} onChange={onMinGroupsChange} />
          <NumberField label="קבוצות מקסימום" value={maxGroups} onChange={onMaxGroupsChange} />
          <NumberField label="גודל מינימום" value={minGroupSize} onChange={onMinGroupSizeChange} />
          <NumberField label="גודל מקסימום" value={maxGroupSize} onChange={onMaxGroupSizeChange} />
          <div className="flex gap-2 sm:col-span-2">
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
            >
              {saving ? 'שומר...' : 'שמירה'}
            </button>
            <button
              type="button"
              onClick={onCancelEdit}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm text-slate-700"
            >
              ביטול
            </button>
          </div>
        </form>
      )}

      {!editing && (
        <button
          type="button"
          onClick={onStartEdit}
          className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          עריכת הגדרות
        </button>
      )}
    </div>
  )
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3">
      <p className="text-xs text-slate-500">{label}</p>
      <p className="mt-1 font-medium text-slate-900">{value}</p>
    </div>
  )
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string
  value: string
  onChange: (value: string) => void
}) {
  return (
    <label className="block">
      <span className="text-sm font-medium text-slate-700">{label}</span>
      <input
        type="number"
        min={1}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
        required
      />
    </label>
  )
}
