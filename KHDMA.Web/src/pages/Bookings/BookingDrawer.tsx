import { useEffect, useState } from 'react'
import { UnauthorizedError } from '../../api/admin'
import {
  cancelBooking,
  getBookingDetails,
  getTranscript,
  type BookingDetail,
  type ChatMessage,
} from '../../api/bookings'
import Badge from '../../components/Badge'
import { Spinner } from '../../components/icons'
import { fullDateTime, moneyFull } from '../../lib/format'
import { bookingStatus, paymentStatus } from '../../lib/status'

const CANCELLABLE = new Set([0, 1, 2, 3, 4, 5])
const shortId = (id: string) => id.slice(0, 4).toUpperCase()

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="bk-row">
      <span className="bk-row__label">{label}</span>
      <span className="bk-row__value">{value}</span>
    </div>
  )
}

export default function BookingDrawer({
  bookingId,
  token,
  onClose,
  onCancelled,
  onUnauthorized,
}: {
  bookingId: string
  token: string
  onClose: () => void
  onCancelled: () => void
  onUnauthorized: () => void
}) {
  const [detail, setDetail] = useState<BookingDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [chat, setChat] = useState<ChatMessage[] | null>(null)
  const [chatLoading, setChatLoading] = useState(false)

  const [cancelMode, setCancelMode] = useState(false)
  const [reason, setReason] = useState('')
  const [cancelBusy, setCancelBusy] = useState(false)
  const [cancelError, setCancelError] = useState<string | null>(null)

  useEffect(() => {
    let live = true
    setLoading(true)
    getBookingDetails(token, bookingId)
      .then((d) => live && setDetail(d))
      .catch((err) => {
        if (err instanceof UnauthorizedError) onUnauthorized()
        else if (live) setError(err instanceof Error ? err.message : 'Failed to load.')
      })
      .finally(() => live && setLoading(false))
    return () => {
      live = false
    }
  }, [token, bookingId, onUnauthorized])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && !cancelBusy && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose, cancelBusy])

  async function loadChat() {
    setChatLoading(true)
    try {
      setChat(await getTranscript(token, bookingId))
    } catch (err) {
      if (err instanceof UnauthorizedError) onUnauthorized()
      else setChat([])
    } finally {
      setChatLoading(false)
    }
  }

  async function doCancel() {
    setCancelBusy(true)
    setCancelError(null)
    try {
      await cancelBooking(token, bookingId, reason.trim() || 'Cancelled by admin')
      onCancelled()
    } catch (err) {
      if (err instanceof UnauthorizedError) onUnauthorized()
      else setCancelError(err instanceof Error ? err.message : 'Cancel failed.')
    } finally {
      setCancelBusy(false)
    }
  }

  const pay = detail?.paymentDetails
  const canCancel = detail && CANCELLABLE.has(detail.status)

  return (
    <div className="bk-drawer-root" role="dialog" aria-modal="true" aria-label="Booking">
      <div className="bk-overlay" onClick={cancelBusy ? undefined : onClose} />
      <aside className="bk-drawer">
        <header className="bk-drawer__head">
          <div>
            <h3>Booking #BK-{shortId(bookingId)}</h3>
            {detail && <p>Created {fullDateTime(detail.createAt)}</p>}
          </div>
          <button
            type="button"
            className="bk-drawer__close"
            onClick={onClose}
            aria-label="Close"
          >
            ✕
          </button>
        </header>

        <div className="bk-drawer__body">
          {loading && (
            <div className="bk-loading">
              <Spinner /> Loading…
            </div>
          )}
          {error && <div className="inline-error">{error}</div>}

          {detail && (
            <>
              <div className="bk-status">
                <Badge {...bookingStatus(detail.status)} />
                {detail.scheduledTime && (
                  <span className="bk-status__sched">
                    Scheduled · {fullDateTime(detail.scheduledTime)}
                  </span>
                )}
              </div>

              <section className="bk-section">
                <h4>Details</h4>
                <Row label="Customer" value={detail.customerName} />
                <Row label="Provider" value={detail.providerName || 'Not assigned'} />
                <Row label="Service" value={detail.serviceName || '—'} />
                <Row label="Type" value={detail.bookingType === 1 ? 'Scheduled' : 'Immediate'} />
                <Row label="Address" value={detail.address || '—'} />
                {detail.cancelReason && (
                  <Row label="Cancel reason" value={detail.cancelReason} />
                )}
              </section>

              <section className="bk-section bk-pay">
                <h4>Payment info</h4>
                {pay ? (
                  <>
                    <div className="bk-pay__row">
                      <span>Total</span>
                      <strong>{moneyFull(pay.amount)}</strong>
                    </div>
                    <div className="bk-pay__row bk-pay__muted">
                      <span>Commission</span>
                      <span>{moneyFull(pay.commissionAmount)}</span>
                    </div>
                    <div className="bk-pay__row bk-pay__muted">
                      <span>Provider earning</span>
                      <span>{moneyFull(pay.providerEarning)}</span>
                    </div>
                    <div className="bk-pay__foot">
                      <Badge {...paymentStatus(pay.paymentStatus)} />
                      {pay.paidAt && <span>Paid {fullDateTime(pay.paidAt)}</span>}
                    </div>
                  </>
                ) : (
                  <p className="bk-empty">No payment recorded for this booking.</p>
                )}
              </section>

              <section className="bk-section">
                <h4>Chat transcript</h4>
                {chat === null ? (
                  <button
                    type="button"
                    className="bk-linkbtn"
                    onClick={() => void loadChat()}
                    disabled={chatLoading}
                  >
                    {chatLoading ? <Spinner /> : 'View chat between customer & provider'}
                  </button>
                ) : chat.length === 0 ? (
                  <p className="bk-empty">No messages for this booking.</p>
                ) : (
                  <div className="bk-chat">
                    {chat.map((m) => (
                      <div key={m.messageId} className="bk-msg">
                        <div className="bk-msg__top">
                          <span className="bk-msg__name">{m.senderName}</span>
                          <span className="bk-msg__time">{fullDateTime(m.sentAt)}</span>
                        </div>
                        <p>{m.messageText}</p>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            </>
          )}
        </div>

        {detail && (
          <footer className="bk-drawer__foot">
            {!canCancel ? (
              <p className="bk-foot-note">
                This booking is <strong>{bookingStatus(detail.status).label}</strong> and
                can’t be cancelled.
              </p>
            ) : cancelMode ? (
              <div className="bk-cancel">
                <textarea
                  rows={2}
                  placeholder="Reason for cancellation…"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  disabled={cancelBusy}
                />
                {cancelError && <div className="inline-error">{cancelError}</div>}
                <div className="bk-cancel__actions">
                  <button
                    type="button"
                    className="bk-linkbtn"
                    onClick={() => setCancelMode(false)}
                    disabled={cancelBusy}
                  >
                    Back
                  </button>
                  <button
                    type="button"
                    className="bk-danger"
                    onClick={() => void doCancel()}
                    disabled={cancelBusy}
                  >
                    {cancelBusy ? <Spinner /> : 'Confirm cancellation'}
                  </button>
                </div>
              </div>
            ) : (
              <>
                <button
                  type="button"
                  className="bk-danger bk-danger--full"
                  onClick={() => {
                    setCancelError(null)
                    setCancelMode(true)
                  }}
                >
                  Force cancel booking
                </button>
                <p className="bk-foot-note">
                  This immediately stops the service and triggers the refund policy.
                </p>
              </>
            )}
          </footer>
        )}
      </aside>
    </div>
  )
}
