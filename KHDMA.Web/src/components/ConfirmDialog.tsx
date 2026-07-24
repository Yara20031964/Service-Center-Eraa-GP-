import { useEffect } from 'react'
import { Spinner } from './icons'

// A modal confirmation dialog. The parent owns `busy`/`error` so the dialog can
// stay open and show a server message (e.g. a 409) when the action fails.
export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Delete',
  busy = false,
  error,
  onConfirm,
  onCancel,
}: {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  busy?: boolean
  error?: string | null
  onConfirm: () => void
  onCancel: () => void
}) {
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !busy) onCancel()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, busy, onCancel])

  if (!open) return null

  return (
    <div className="modal-root" role="dialog" aria-modal="true" aria-label={title}>
      <div className="modal-overlay" onClick={busy ? undefined : onCancel} />
      <div className="modal">
        <h3 className="modal__title">{title}</h3>
        <p className="modal__msg">{message}</p>
        {error && <div className="inline-error">{error}</div>}
        <div className="modal__actions">
          <button
            type="button"
            className="linkbtn"
            onClick={onCancel}
            disabled={busy}
          >
            Cancel
          </button>
          <button
            type="button"
            className="btn btn--sm modal__confirm"
            onClick={onConfirm}
            disabled={busy}
          >
            {busy ? <Spinner /> : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
