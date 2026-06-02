import { useState } from 'react'
import type { AssignmentDetail } from '../../services/assignmentDetailService'
import { ruleTypeLabel } from '../../services/assignmentDetailService'

interface AssignmentConstraintsTabProps {
  detail: AssignmentDetail
  saving: boolean
  onAddMandatory: (participantA: string, participantB: string) => void
  onDeleteMandatory: (constraintId: number) => void
  onAddForbidden: (participantA: string, participantB: string) => void
  onDeleteForbidden: (constraintId: number) => void
  onAddClassification: (dimensionCode: string, ruleType: string) => void
  onDeleteClassification: (dimensionCode: string) => void
}

export function AssignmentConstraintsTab({
  detail,
  saving,
  onAddMandatory,
  onDeleteMandatory,
  onAddForbidden,
  onDeleteForbidden,
  onAddClassification,
  onDeleteClassification,
}: AssignmentConstraintsTabProps) {
  const [pairA, setPairA] = useState('')
  const [pairB, setPairB] = useState('')
  const [pairMode, setPairMode] = useState<'mandatory' | 'forbidden'>('mandatory')
  const [dimensionCode, setDimensionCode] = useState('')
  const [ruleType, setRuleType] = useState('Balance')

  function handleAddPair(event: React.FormEvent) {
    event.preventDefault()

    if (pairMode === 'mandatory') {
      onAddMandatory(pairA.trim(), pairB.trim())
    } else {
      onAddForbidden(pairA.trim(), pairB.trim())
    }

    setPairA('')
    setPairB('')
  }

  function handleAddClassification(event: React.FormEvent) {
    event.preventDefault()
    onAddClassification(dimensionCode.trim(), ruleType)
    setDimensionCode('')
  }

  return (
    <div className="space-y-8">
      <section>
        <h3 className="font-semibold text-slate-900">זוגות</h3>
        <form className="mt-3 flex flex-wrap items-end gap-2" onSubmit={handleAddPair}>
          <label>
            <span className="text-xs text-slate-500">סוג</span>
            <select
              value={pairMode}
              onChange={(event) =>
                setPairMode(event.target.value as 'mandatory' | 'forbidden')
              }
              className="mt-1 block rounded-lg border border-slate-200 px-3 py-2 text-sm"
            >
              <option value="mandatory">חובה</option>
              <option value="forbidden">איסור</option>
            </select>
          </label>
          <label>
            <span className="text-xs text-slate-500">משתתף א</span>
            <input
              type="text"
              value={pairA}
              onChange={(event) => setPairA(event.target.value)}
              className="mt-1 block rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </label>
          <label>
            <span className="text-xs text-slate-500">משתתף ב</span>
            <input
              type="text"
              value={pairB}
              onChange={(event) => setPairB(event.target.value)}
              className="mt-1 block rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
          </label>
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            הוספה
          </button>
        </form>

        <ConstraintList title="זוגות חובה">
          {detail.mandatoryPairs.map((pair) => (
            <ConstraintRow
              key={pair.constraintId}
              label={`${pair.participantA} + ${pair.participantB}`}
              onDelete={() => onDeleteMandatory(pair.constraintId)}
            />
          ))}
          {detail.mandatoryPairs.length === 0 && (
            <EmptyHint text="אין זוגות חובה." />
          )}
        </ConstraintList>

        <ConstraintList title="זוגות איסור">
          {detail.forbiddenPairs.map((pair) => (
            <ConstraintRow
              key={pair.constraintId}
              label={`${pair.participantA} ≠ ${pair.participantB}`}
              onDelete={() => onDeleteForbidden(pair.constraintId)}
            />
          ))}
          {detail.forbiddenPairs.length === 0 && (
            <EmptyHint text="אין זוגות איסור." />
          )}
        </ConstraintList>
      </section>

      <section>
        <h3 className="font-semibold text-slate-900">אילוצי סיווג</h3>
        <form
          className="mt-3 flex flex-wrap items-end gap-2"
          onSubmit={handleAddClassification}
        >
          <label>
            <span className="text-xs text-slate-500">מימד</span>
            <input
              type="text"
              list="constraint-dimensions"
              value={dimensionCode}
              onChange={(event) => setDimensionCode(event.target.value)}
              className="mt-1 block rounded-lg border border-slate-200 px-3 py-2 text-sm"
              required
            />
            <datalist id="constraint-dimensions">
              {detail.availableDimensions.map((code) => (
                <option key={code} value={code} />
              ))}
            </datalist>
          </label>
          <label>
            <span className="text-xs text-slate-500">סוג</span>
            <select
              value={ruleType}
              onChange={(event) => setRuleType(event.target.value)}
              className="mt-1 block rounded-lg border border-slate-200 px-3 py-2 text-sm"
            >
              <option value="Balance">איזון</option>
              <option value="Separation">הפרדה</option>
            </select>
          </label>
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            הוספה
          </button>
        </form>

        <ConstraintList title="כללי סיווג">
          {detail.classificationConstraints.map((constraint) => (
            <ConstraintRow
              key={constraint.dimensionCode}
              label={`${constraint.dimensionCode} — ${ruleTypeLabel(constraint.ruleType)}`}
              onDelete={() => onDeleteClassification(constraint.dimensionCode)}
            />
          ))}
          {detail.classificationConstraints.length === 0 && (
            <EmptyHint text="אין אילוצי סיווג." />
          )}
        </ConstraintList>
      </section>
    </div>
  )
}

function ConstraintList({
  title,
  children,
}: {
  title: string
  children: React.ReactNode
}) {
  return (
    <div className="mt-4">
      <p className="text-sm font-medium text-slate-700">{title}</p>
      <ul className="mt-2 space-y-1">{children}</ul>
    </div>
  )
}

function ConstraintRow({
  label,
  onDelete,
}: {
  label: string
  onDelete: () => void
}) {
  return (
    <li className="flex items-center justify-between rounded-lg border border-slate-100 bg-slate-50 px-3 py-2 text-sm">
      <span>{label}</span>
      <button
        type="button"
        onClick={onDelete}
        className="text-xs font-medium text-red-600 hover:underline"
      >
        מחיקה
      </button>
    </li>
  )
}

function EmptyHint({ text }: { text: string }) {
  return <li className="text-sm text-slate-500">{text}</li>
}
