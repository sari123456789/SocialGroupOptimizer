import { useEffect, useMemo, useState } from 'react'
import type { AssignmentDetail } from '../../services/assignmentDetailService'
import {
  fetchParticipantExplanation,
  type AlternativeGroupEvaluation,
  type AlternativeSwapEvaluation,
  type ParticipantPlacementExplanation,
} from '../../services/explanationService'
import {
  applyManualMove,
  type ManualMoveRequest,
} from '../../services/manualMoveService'
import { getDisplayLabel } from '../../utils/participantDisplay'

interface ParticipantExplanationDialogProps {
  assignmentId: number
  participantId: string
  displayName?: string | null
  participants?: AssignmentDetail['participants']
  onClose: () => void
  onApplied: (detail: AssignmentDetail) => void
}

type SelectedAlternative =
  | { kind: 'transfer'; alternative: AlternativeGroupEvaluation }
  | { kind: 'swap'; alternative: AlternativeSwapEvaluation }

export function ParticipantExplanationDialog({
  assignmentId,
  participantId,
  displayName,
  participants = [],
  onClose,
  onApplied,
}: ParticipantExplanationDialogProps) {
  const [loading, setLoading] = useState(true)
  const [errors, setErrors] = useState<string[] | null>(null)
  const [explanation, setExplanation] =
    useState<ParticipantPlacementExplanation | null>(null)

  useEffect(() => {
    let active = true

    setLoading(true)
    setErrors(null)
    setExplanation(null)

    fetchParticipantExplanation(assignmentId, participantId)
      .then((result) => {
        if (!active) {
          return
        }

        if (result.ok) {
          setExplanation(result.explanation)
        } else {
          setErrors(result.errors)
        }
      })
      .catch(() => {
        if (active) {
          setErrors(['אירעה שגיאה בטעינת ההסבר.'])
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [assignmentId, participantId])

  const labelByParticipantId = useMemo(() => {
    const map = new Map<string, string>()
    for (const participant of participants) {
      map.set(
        participant.participantId,
        getDisplayLabel(participant.displayName, participant.participantId),
      )
    }
    return map
  }, [participants])

  function participantLabel(
    identity: string,
    apiDisplayName?: string | null,
  ): string {
    return getDisplayLabel(
      apiDisplayName ?? labelByParticipantId.get(identity),
      identity,
    )
  }

  const title = displayName
    ? getDisplayLabel(displayName, participantId)
    : participantLabel(
        participantId,
        explanation?.participantDisplayName,
      )

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      role="dialog"
      aria-modal="true"
      onClick={onClose}
    >
      <div
        dir="rtl"
        className="max-h-[85vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex items-start justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-900">הסבר שיבוץ</h2>
            <p className="mt-1 text-sm text-slate-500">{title}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            aria-label="סגירה"
          >
            ✕
          </button>
        </div>

        <div className="px-6 py-5">
          {loading && (
            <div className="h-32 animate-pulse rounded-xl bg-slate-100" />
          )}

          {!loading && errors && (
            <div
              className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
              role="alert"
            >
              <ul className="list-disc space-y-1 pr-5">
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </div>
          )}

          {!loading && explanation && (
            <ExplanationBody
              assignmentId={assignmentId}
              participantId={participantId}
              explanation={explanation}
              participantLabel={participantLabel}
              onApplied={onApplied}
              onClose={onClose}
            />
          )}
        </div>
      </div>
    </div>
  )
}

function ExplanationBody({
  assignmentId,
  participantId,
  explanation,
  participantLabel,
  onApplied,
  onClose,
}: {
  assignmentId: number
  participantId: string
  explanation: ParticipantPlacementExplanation
  participantLabel: (identity: string, apiDisplayName?: string | null) => string
  onApplied: (detail: AssignmentDetail) => void
  onClose: () => void
}) {
  const [selected, setSelected] = useState<SelectedAlternative | null>(null)
  const [overrideChecked, setOverrideChecked] = useState(false)
  const [applyLoading, setApplyLoading] = useState(false)
  const [applyError, setApplyError] = useState<string | null>(null)

  function isSelectableTransfer(alternative: AlternativeGroupEvaluation): boolean {
    return alternative.isLegal || alternative.requiresOverride
  }

  function isSelectableSwap(alternative: AlternativeSwapEvaluation): boolean {
    return alternative.isLegal || alternative.requiresOverride
  }

  function buildRequest(selection: SelectedAlternative): ManualMoveRequest {
    if (selection.kind === 'transfer') {
      return {
        moveType: 'Transfer',
        participantId,
        targetGroupId: selection.alternative.groupId,
      }
    }

    return {
      moveType: 'Swap',
      participantId,
      secondParticipantId: selection.alternative.swapWithParticipantId,
    }
  }

  function selectAlternative(selection: SelectedAlternative) {
    setSelected(selection)
    setOverrideChecked(false)
    setApplyError(null)
  }

  async function handleApply(withOverride: boolean) {
    if (!selected) {
      return
    }

    setApplyLoading(true)
    setApplyError(null)

    try {
      const result = await applyManualMove(
        assignmentId,
        buildRequest(selected),
        withOverride,
      )

      if (result.kind === 'applied') {
        onApplied(result.detail)
        onClose()
        return
      }

      if (result.kind === 'needsOverride') {
        setApplyError('השינוי דורש אישור חריגה מפורש.')
        return
      }

      if (result.kind === 'blocked') {
        setApplyError(
          result.preview.blockingErrors[0] ??
            result.preview.message ??
            'לא ניתן לבצע את השינוי.',
        )
        return
      }

      if (result.kind === 'notFound') {
        setApplyError('החלוקה לא נמצאה.')
        return
      }

      setApplyError(result.message)
    } catch {
      setApplyError('לא ניתן לבצע את השינוי.')
    } finally {
      setApplyLoading(false)
    }
  }

  const bestSwapLabel =
    explanation.bestSwapWithParticipantId != null
      ? participantLabel(
          explanation.bestSwapWithParticipantId,
          explanation.bestSwapWithParticipantDisplayName,
        )
      : null

  return (
    <div className="space-y-5">
      <section className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
        <p className="text-sm text-slate-700">
          המשתתף שובץ ל
          <strong> קבוצה {explanation.currentGroupId}</strong>.
        </p>
        <p className="mt-1 text-sm text-slate-600">
          ציון שיבוץ נוכחי:{' '}
          <strong>{explanation.currentGroupScore.toFixed(1)}</strong>
        </p>
        {explanation.canMove && explanation.bestAlternativeGroupId != null ? (
          <p className="mt-1 text-sm text-emerald-700">
            קיים מעבר חוקי משפר לקבוצה {explanation.bestAlternativeGroupId}
            {explanation.estimatedScoreChange != null && (
              <> (כ-{explanation.estimatedScoreChange.toFixed(1)}+ נקודות)</>
            )}
            .
          </p>
        ) : null}
        {explanation.canSwap && bestSwapLabel != null ? (
          <p className="mt-1 text-sm text-emerald-700">
            קיימת החלפה חוקית משפרת עם {bestSwapLabel}
            {explanation.bestSwapTargetGroupId != null && (
              <> לקבוצה {explanation.bestSwapTargetGroupId}</>
            )}
            {explanation.bestSwapScoreChange != null && (
              <> (כ-{explanation.bestSwapScoreChange.toFixed(1)}+ נקודות)</>
            )}
            .
          </p>
        ) : null}
        {!explanation.canMove && !explanation.canSwap ? (
          <p className="mt-1 text-sm text-slate-600">
            הקבוצה הנוכחית היא ההצבה הטובה ביותר עבור המשתתף.
          </p>
        ) : null}
        <p className="mt-2 text-xs text-slate-500">
          ניתן ללחוץ על חלופה חוקית או כזו שדורשת חריגה כדי לבצע הזזה ידנית.
        </p>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900">
          למה המשתתף כאן
        </h3>
        {explanation.reasons.length > 0 ? (
          <ul className="space-y-1.5 text-sm">
            {explanation.reasons.map((reason, index) => (
              <li
                key={`${reason.code}-${index}`}
                className="flex items-start gap-2 text-slate-700"
              >
                <span className="text-emerald-600">✓</span>
                <span>{reason.message}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-slate-500">אין סיבות חברתיות בולטות.</p>
        )}
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900">
          חלופות העברה
        </h3>
        {explanation.alternatives.length > 0 ? (
          <ul className="space-y-2">
            {explanation.alternatives.map((alternative) => {
              const selectable = isSelectableTransfer(alternative)
              const isSelected =
                selected?.kind === 'transfer' &&
                selected.alternative.groupId === alternative.groupId

              return (
                <li key={alternative.groupId}>
                  <button
                    type="button"
                    disabled={!selectable || applyLoading}
                    onClick={() =>
                      selectable && selectAlternative({ kind: 'transfer', alternative })
                    }
                    className={`w-full rounded-xl border px-4 py-3 text-right text-sm transition ${
                      alternative.isLegal
                        ? 'border-slate-200 bg-white hover:border-indigo-300 hover:bg-indigo-50'
                        : alternative.requiresOverride
                          ? 'border-amber-200 bg-amber-50 hover:border-amber-300'
                          : 'border-red-200 bg-red-50'
                    } ${isSelected ? 'ring-2 ring-indigo-400' : ''} ${
                      selectable
                        ? 'cursor-pointer'
                        : 'cursor-not-allowed opacity-80'
                    } disabled:opacity-60`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-medium text-slate-900">
                        העברה לקבוצה {alternative.groupId}
                      </span>
                      {alternative.isLegal || alternative.requiresOverride ? (
                        <AlternativeScoreDelta
                          participantDelta={alternative.estimatedParticipantScoreDelta}
                          assignmentDelta={alternative.estimatedAssignmentScoreDelta}
                          muted={alternative.requiresOverride}
                        />
                      ) : (
                        <span className="font-medium text-red-700">לא חוקית</span>
                      )}
                    </div>
                    {alternative.reasons.length > 0 && (
                      <ul className="mt-2 space-y-1 text-xs text-slate-600">
                        {alternative.reasons.map((reason, index) => (
                          <li
                            key={`${reason.code}-${index}`}
                            className="flex items-start gap-2"
                          >
                            <span
                              className={
                                alternative.isLegal
                                  ? 'text-slate-400'
                                  : alternative.requiresOverride
                                    ? 'text-amber-500'
                                    : 'text-red-500'
                              }
                            >
                              {alternative.isLegal ? '•' : '✗'}
                            </span>
                            <span>{reason.message}</span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </button>
                  {isSelected && (
                    <ApplyPanel
                      requiresOverride={alternative.requiresOverride}
                      overrideChecked={overrideChecked}
                      applyLoading={applyLoading}
                      applyError={applyError}
                      onOverrideChange={setOverrideChecked}
                      onApply={() =>
                        void handleApply(
                          alternative.requiresOverride ? overrideChecked : false,
                        )
                      }
                    />
                  )}
                </li>
              )
            })}
          </ul>
        ) : (
          <p className="text-sm text-slate-500">אין חלופות העברה.</p>
        )}
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900">
          חלופות החלפה
        </h3>
        {explanation.swapAlternatives.length > 0 ? (
          <ul className="space-y-2">
            {explanation.swapAlternatives.map((swap) => {
              const swapLabel = participantLabel(
                swap.swapWithParticipantId,
                swap.swapWithParticipantDisplayName,
              )
              const selectable = isSelectableSwap(swap)
              const isSelected =
                selected?.kind === 'swap' &&
                selected.alternative.swapWithParticipantId ===
                  swap.swapWithParticipantId

              return (
                <li key={`${swap.targetGroupId}-${swap.swapWithParticipantId}`}>
                  <button
                    type="button"
                    disabled={!selectable || applyLoading}
                    onClick={() =>
                      selectable && selectAlternative({ kind: 'swap', alternative: swap })
                    }
                    className={`w-full rounded-xl border px-4 py-3 text-right text-sm transition ${
                      swap.isLegal
                        ? 'border-slate-200 bg-white hover:border-indigo-300 hover:bg-indigo-50'
                        : swap.requiresOverride
                          ? 'border-amber-200 bg-amber-50 hover:border-amber-300'
                          : 'border-red-200 bg-red-50'
                    } ${isSelected ? 'ring-2 ring-indigo-400' : ''} ${
                      selectable
                        ? 'cursor-pointer'
                        : 'cursor-not-allowed opacity-80'
                    } disabled:opacity-60`}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-medium text-slate-900">
                        החלפה עם {swapLabel} (קבוצה {swap.targetGroupId})
                      </span>
                      {swap.isLegal || swap.requiresOverride ? (
                        <AlternativeScoreDelta
                          participantDelta={swap.estimatedParticipantScoreDelta}
                          assignmentDelta={swap.estimatedAssignmentScoreDelta}
                          muted={swap.requiresOverride}
                        />
                      ) : (
                        <span className="font-medium text-red-700">לא חוקית</span>
                      )}
                    </div>
                    {swap.reasons.length > 0 && (
                      <ul className="mt-2 space-y-1 text-xs text-slate-600">
                        {swap.reasons.map((reason, index) => (
                          <li
                            key={`${reason.code}-${index}`}
                            className="flex items-start gap-2"
                          >
                            <span
                              className={
                                swap.isLegal
                                  ? 'text-slate-400'
                                  : swap.requiresOverride
                                    ? 'text-amber-500'
                                    : 'text-red-500'
                              }
                            >
                              {swap.isLegal ? '•' : '✗'}
                            </span>
                            <span>{reason.message}</span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </button>
                  {isSelected && (
                    <ApplyPanel
                      requiresOverride={swap.requiresOverride}
                      overrideChecked={overrideChecked}
                      applyLoading={applyLoading}
                      applyError={applyError}
                      onOverrideChange={setOverrideChecked}
                      onApply={() =>
                        void handleApply(
                          swap.requiresOverride ? overrideChecked : false,
                        )
                      }
                    />
                  )}
                </li>
              )
            })}
          </ul>
        ) : (
          <p className="text-sm text-slate-500">אין חלופות החלפה.</p>
        )}
      </section>
    </div>
  )
}

function AlternativeScoreDelta({
  participantDelta,
  assignmentDelta,
  muted = false,
}: {
  participantDelta: number
  assignmentDelta: number
  muted?: boolean
}) {
  function formatDelta(value: number): string {
    const prefix = value >= 0 ? '+' : ''
    return `${prefix}${value.toFixed(1)}`
  }

  function deltaClass(value: number): string {
    if (muted) {
      return 'text-amber-700'
    }

    return value >= 0 ? 'text-emerald-700' : 'text-amber-700'
  }

  return (
    <div className="text-left text-xs leading-5">
      <div className={deltaClass(participantDelta)}>
        משתתף {formatDelta(participantDelta)}
      </div>
      <div className={deltaClass(assignmentDelta)}>
        חלוקה {formatDelta(assignmentDelta)}
      </div>
      {muted ? (
        <div className="font-medium text-amber-700">דורש חריגה</div>
      ) : null}
    </div>
  )
}

function ApplyPanel({
  requiresOverride,
  overrideChecked,
  applyLoading,
  applyError,
  onOverrideChange,
  onApply,
}: {
  requiresOverride: boolean
  overrideChecked: boolean
  applyLoading: boolean
  applyError: string | null
  onOverrideChange: (value: boolean) => void
  onApply: () => void
}) {
  return (
    <div className="mt-2 space-y-2 rounded-xl border border-indigo-200 bg-indigo-50 px-4 py-3">
      {requiresOverride ? (
        <label className="flex items-start gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={overrideChecked}
            onChange={(event) => onOverrideChange(event.target.checked)}
            className="mt-1"
          />
          <span>אני מאשר חריגה מאילוצים עסקיים</span>
        </label>
      ) : (
        <p className="text-sm text-slate-700">לבצע את ההזזה הידנית?</p>
      )}

      {applyError && (
        <p
          className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700"
          role="alert"
        >
          {applyError}
        </p>
      )}

      <button
        type="button"
        onClick={onApply}
        disabled={applyLoading || (requiresOverride && !overrideChecked)}
        className="w-full rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
      >
        {applyLoading ? 'שומר…' : 'בצע שינוי'}
      </button>
    </div>
  )
}
