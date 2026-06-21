import { useMemo, useState } from 'react'
import type { AssignmentDetail } from '../../services/assignmentDetailService'
import {
  applyManualMove,
  previewManualMove,
  type ManualMovePreview,
  type ManualMoveRequest,
} from '../../services/manualMoveService'
import { getParticipantSelectLabel } from '../../utils/participantDisplay'

interface ManualMoveDialogProps {
  detail: AssignmentDetail
  onClose: () => void
  onApplied: (detail: AssignmentDetail) => void
}

type MoveType = 'Transfer' | 'Swap'

export function ManualMoveDialog({
  detail,
  onClose,
  onApplied,
}: ManualMoveDialogProps) {
  const [moveType, setMoveType] = useState<MoveType>('Transfer')
  const [participantId, setParticipantId] = useState('')
  const [targetGroupId, setTargetGroupId] = useState('')
  const [secondParticipantId, setSecondParticipantId] = useState('')

  const [preview, setPreview] = useState<ManualMovePreview | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)
  const [applyLoading, setApplyLoading] = useState(false)
  const [overrideChecked, setOverrideChecked] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // מיפוי משתתף → קבוצה נוכחית, ורשימת משתתפים עם תווית.
  const groupByParticipant = useMemo(() => {
    const map = new Map<string, number>()
    for (const group of detail.placementGroups) {
      for (const id of group.participantIds) {
        map.set(id, group.groupId)
      }
    }
    return map
  }, [detail.placementGroups])

  const participantMetaById = useMemo(() => {
    const map = new Map<string, { displayName?: string | null }>()
    for (const participant of detail.participants) {
      map.set(participant.participantId, {
        displayName: participant.displayName,
      })
    }
    return map
  }, [detail.participants])

  const allParticipantIds = useMemo(
    () => detail.placementGroups.flatMap((group) => group.participantIds),
    [detail.placementGroups],
  )

  const groupIds = useMemo(
    () => detail.placementGroups.map((group) => group.groupId),
    [detail.placementGroups],
  )

  function participantLabel(id: string): string {
    const meta = participantMetaById.get(id)
    return getParticipantSelectLabel(
      meta?.displayName,
      id,
      groupByParticipant.get(id),
    )
  }

  function resetPreview() {
    setPreview(null)
    setOverrideChecked(false)
    setError(null)
  }

  function buildRequest(): ManualMoveRequest | null {
    if (!participantId) {
      setError('יש לבחור משתתף.')
      return null
    }

    if (moveType === 'Transfer') {
      if (!targetGroupId) {
        setError('יש לבחור קבוצת יעד.')
        return null
      }
      return {
        moveType: 'Transfer',
        participantId,
        targetGroupId: Number(targetGroupId),
      }
    }

    if (!secondParticipantId) {
      setError('יש לבחור משתתף שני להחלפה.')
      return null
    }
    return {
      moveType: 'Swap',
      participantId,
      secondParticipantId,
    }
  }

  async function handlePreview() {
    const request = buildRequest()
    if (!request) {
      return
    }

    setPreviewLoading(true)
    setError(null)
    setOverrideChecked(false)

    try {
      const result = await previewManualMove(detail.assignmentId, request)
      if (result === null) {
        setError('החלוקה לא נמצאה.')
        setPreview(null)
        return
      }
      setPreview(result)
    } catch {
      setError('לא ניתן לבדוק את השינוי.')
      setPreview(null)
    } finally {
      setPreviewLoading(false)
    }
  }

  async function handleApply(withOverride: boolean) {
    const request = buildRequest()
    if (!request) {
      return
    }

    setApplyLoading(true)
    setError(null)

    try {
      const result = await applyManualMove(
        detail.assignmentId,
        request,
        withOverride,
      )

      if (result.kind === 'applied') {
        onApplied(result.detail)
        onClose()
        return
      }

      if (result.kind === 'needsOverride' || result.kind === 'blocked') {
        setPreview(result.preview)
        if (result.kind === 'needsOverride') {
          setError('השינוי דורש אישור חריגה מפורש.')
        }
        return
      }

      if (result.kind === 'notFound') {
        setError('החלוקה לא נמצאה.')
        return
      }

      setError(result.message)
    } catch {
      setError('לא ניתן לבצע את השינוי.')
    } finally {
      setApplyLoading(false)
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      role="dialog"
      aria-modal="true"
      onClick={onClose}
    >
      <div
        dir="rtl"
        className="max-h-[88vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex items-start justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-900">שינוי ידני</h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            aria-label="סגירה"
          >
            ✕
          </button>
        </div>

        <div className="space-y-4 px-6 py-5">
          <div className="flex gap-2">
            <TypeButton
              active={moveType === 'Transfer'}
              label="העברה"
              onClick={() => {
                setMoveType('Transfer')
                resetPreview()
              }}
            />
            <TypeButton
              active={moveType === 'Swap'}
              label="החלפה"
              onClick={() => {
                setMoveType('Swap')
                resetPreview()
              }}
            />
          </div>

          <label className="block">
            <span className="text-sm font-medium text-slate-700">משתתף</span>
            <select
              value={participantId}
              onChange={(event) => {
                setParticipantId(event.target.value)
                resetPreview()
              }}
              className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            >
              <option value="">בחר משתתף…</option>
              {allParticipantIds.map((id) => (
                <option key={id} value={id}>
                  {participantLabel(id)}
                </option>
              ))}
            </select>
          </label>

          {moveType === 'Transfer' && (
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                קבוצת יעד
              </span>
              <select
                value={targetGroupId}
                onChange={(event) => {
                  setTargetGroupId(event.target.value)
                  resetPreview()
                }}
                className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              >
                <option value="">בחר קבוצה…</option>
                {groupIds
                  .filter(
                    (groupId) =>
                      groupId !== groupByParticipant.get(participantId),
                  )
                  .map((groupId) => (
                    <option key={groupId} value={groupId}>
                      קבוצה {groupId}
                    </option>
                  ))}
              </select>
            </label>
          )}

          {moveType === 'Swap' && (
            <label className="block">
              <span className="text-sm font-medium text-slate-700">
                משתתף להחלפה
              </span>
              <select
                value={secondParticipantId}
                onChange={(event) => {
                  setSecondParticipantId(event.target.value)
                  resetPreview()
                }}
                className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
              >
                <option value="">בחר משתתף…</option>
                {allParticipantIds
                  .filter((id) => id !== participantId)
                  .map((id) => (
                    <option key={id} value={id}>
                      {participantLabel(id)}
                    </option>
                  ))}
              </select>
            </label>
          )}

          <button
            type="button"
            onClick={() => void handlePreview()}
            disabled={previewLoading || applyLoading}
            className="w-full rounded-lg border border-indigo-200 px-4 py-2 text-sm font-medium text-indigo-700 hover:bg-indigo-50 disabled:opacity-60"
          >
            {previewLoading ? 'בודק…' : 'בדיקת שינוי'}
          </button>

          {error && (
            <p
              className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700"
              role="alert"
            >
              {error}
            </p>
          )}

          {preview && (
            <PreviewPanel
              preview={preview}
              overrideChecked={overrideChecked}
              applyLoading={applyLoading}
              onOverrideChange={setOverrideChecked}
              onApply={handleApply}
              onCancel={onClose}
            />
          )}
        </div>
      </div>
    </div>
  )
}

function PreviewPanel({
  preview,
  overrideChecked,
  applyLoading,
  onOverrideChange,
  onApply,
  onCancel,
}: {
  preview: ManualMovePreview
  overrideChecked: boolean
  applyLoading: boolean
  onOverrideChange: (value: boolean) => void
  onApply: (withOverride: boolean) => void
  onCancel: () => void
}) {
  const scoreLine = (
    <p className="text-sm text-slate-600">
      ציון לפני: <strong>{preview.scoreBefore.toFixed(1)}</strong> · אחרי:{' '}
      <strong>{preview.scoreAfter.toFixed(1)}</strong> (
      {preview.scoreDelta >= 0 ? '+' : ''}
      {preview.scoreDelta.toFixed(1)})
    </p>
  )

  if (preview.isLegal) {
    return (
      <div className="space-y-3 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3">
        <p className="text-sm font-semibold text-emerald-800">השינוי תקין</p>
        {scoreLine}
        <button
          type="button"
          onClick={() => onApply(false)}
          disabled={applyLoading}
          className="w-full rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {applyLoading ? 'שומר…' : 'אשר שינוי'}
        </button>
      </div>
    )
  }

  if (preview.requiresOverride && preview.canOverride) {
    return (
      <div className="space-y-3 rounded-xl border border-amber-300 bg-amber-50 px-4 py-3">
        <p className="text-sm font-semibold text-amber-900">
          השינוי מפר אילוצים
        </p>
        {scoreLine}
        <ul className="list-disc space-y-1 pr-5 text-sm text-amber-900">
          {preview.brokenConstraints.map((broken, index) => (
            <li key={`${broken.constraintType}-${index}`}>{broken.message}</li>
          ))}
        </ul>
        <label className="flex items-start gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={overrideChecked}
            onChange={(event) => onOverrideChange(event.target.checked)}
            className="mt-0.5"
          />
          <span>אני מאשר את השינוי למרות שבירת האילוצים.</span>
        </label>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={onCancel}
            className="flex-1 rounded-lg border border-slate-200 px-4 py-2 text-sm text-slate-700"
          >
            ביטול
          </button>
          <button
            type="button"
            onClick={() => onApply(true)}
            disabled={!overrideChecked || applyLoading}
            className="flex-1 rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            {applyLoading ? 'שומר…' : 'אשר למרות שבירת אילוצים'}
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-2 rounded-xl border border-red-200 bg-red-50 px-4 py-3">
      <p className="text-sm font-semibold text-red-800">
        לא ניתן לבצע שינוי זה
      </p>
      <ul className="list-disc space-y-1 pr-5 text-sm text-red-700">
        {preview.blockingErrors.map((message) => (
          <li key={message}>{message}</li>
        ))}
      </ul>
    </div>
  )
}

function TypeButton({
  active,
  label,
  onClick,
}: {
  active: boolean
  label: string
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex-1 rounded-lg border px-4 py-2 text-sm font-medium transition ${
        active
          ? 'border-indigo-600 bg-indigo-50 text-indigo-700'
          : 'border-slate-200 text-slate-600 hover:bg-slate-50'
      }`}
    >
      {label}
    </button>
  )
}
