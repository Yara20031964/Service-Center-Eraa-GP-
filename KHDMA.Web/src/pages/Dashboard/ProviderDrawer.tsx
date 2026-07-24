import { useEffect, useState, type ReactNode } from 'react'
import {
  decideProvider,
  getProviderDetails,
  UnauthorizedError,
  type PendingProvider,
  type ProviderDetail,
} from '../../api/admin'
import { Spinner } from '../../components/icons'

const AVAILABILITY: Record<number, string> = {
  0: 'Online',
  1: 'Offline',
  2: 'Busy',
}

function initials(name: string): string {
  const p = name.trim().split(/\s+/)
  return ((p[0]?.[0] ?? '') + (p[1]?.[0] ?? '')).toUpperCase() || '?'
}

function money(n: number): string {
  return `EGP ${new Intl.NumberFormat('en-US').format(n)}`
}

function fullDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function Row({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="drow">
      <span className="drow__label">{label}</span>
      <span className="drow__value">{value}</span>
    </div>
  )
}

export default function ProviderDrawer({
  provider,
  token,
  onClose,
  onDecided,
  onUnauthorized,
}: {
  provider: PendingProvider
  token: string
  onClose: () => void
  onDecided: (id: string, isApproved: boolean) => void
  onUnauthorized: () => void
}) {
  const [detail, setDetail] = useState<ProviderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState<null | 'approve' | 'reject'>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  // Fetch fresh detail; fall back to the list row if the call fails.
  useEffect(() => {
    let live = true
    setLoading(true)
    getProviderDetails(token, provider.id)
      .then((d) => live && setDetail(d))
      .catch((err) => {
        if (err instanceof UnauthorizedError) onUnauthorized()
      })
      .finally(() => live && setLoading(false))
    return () => {
      live = false
    }
  }, [token, provider.id, onUnauthorized])

  // Close on Escape.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  async function act(isApproved: boolean) {
    setBusy(isApproved ? 'approve' : 'reject')
    setActionError(null)
    try {
      await decideProvider(
        token,
        provider.id,
        isApproved,
        isApproved ? undefined : reason.trim() || undefined,
      )
      onDecided(provider.id, isApproved)
    } catch (err) {
      if (err instanceof UnauthorizedError) {
        onUnauthorized()
        return
      }
      setActionError(
        err instanceof Error ? err.message : 'Action failed. Please try again.',
      )
    } finally {
      setBusy(null)
    }
  }

  const serviceArea = detail?.serviceArea ?? provider.serviceArea
  const hourlyRate = detail?.hourlyRate ?? provider.hourlyRate
  const phone = detail?.phone ?? provider.phone

  return (
    <div className="drawer-root" role="dialog" aria-modal="true" aria-label="Provider application">
      <div className="drawer-overlay" onClick={onClose} />
      <aside className="drawer">
        <header className="drawer__head">
          <div className="drawer__id">
            <span className="avatar avatar--lg">{initials(provider.fullName)}</span>
            <div>
              <h3>{provider.fullName}</h3>
              <p>{provider.email}</p>
            </div>
          </div>
          <button
            type="button"
            className="drawer__close"
            onClick={onClose}
            aria-label="Close"
          >
            ✕
          </button>
        </header>

        <div className="drawer__body">
          <span className="drawer__tag">Pending application</span>

          {loading ? (
            <div className="drawer__loading">
              <Spinner /> Loading details…
            </div>
          ) : (
            <div className="drows">
              <Row label="Phone" value={phone || '—'} />
              <Row label="Service area" value={serviceArea || '—'} />
              <Row label="Hourly rate" value={money(hourlyRate)} />
              <Row
                label="Availability"
                value={
                  detail ? (AVAILABILITY[detail.availabilityStatus] ?? '—') : '—'
                }
              />
              <Row
                label="Rating"
                value={
                  detail && detail.reviewCount > 0
                    ? `${detail.rating.toFixed(1)} (${detail.reviewCount})`
                    : 'No reviews yet'
                }
              />
              <Row label="Applied" value={fullDateTime(provider.createdAt)} />
            </div>
          )}

          <label className="drawer__reason">
            <span>Reason for rejection (optional)</span>
            <textarea
              rows={3}
              placeholder="Add a note the team can see…"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              disabled={busy !== null}
            />
          </label>

          {actionError && <div className="inline-error">{actionError}</div>}
        </div>

        <footer className="drawer__foot">
          <button
            type="button"
            className="btn-reject"
            disabled={busy !== null}
            onClick={() => void act(false)}
          >
            {busy === 'reject' ? <Spinner /> : 'Reject'}
          </button>
          <button
            type="button"
            className="btn-approve"
            disabled={busy !== null}
            onClick={() => void act(true)}
          >
            {busy === 'approve' ? <Spinner /> : 'Approve'}
          </button>
        </footer>
      </aside>
    </div>
  )
}
