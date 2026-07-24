import {
  useCallback,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import type { TokenResponse } from '../../api/auth'
import {
  getPayments,
  issueRefund,
  loadPaymentsSummary,
  UnauthorizedError,
  type PaymentRow,
  type PaymentsSummary,
} from '../../api/admin'
import Avatar from '../../components/Avatar'
import Badge from '../../components/Badge'
import Pagination from '../../components/Pagination'
import { CardIcon, RevenueIcon, Spinner } from '../../components/icons'
import { fullDate, moneyCompact, moneyFull } from '../../lib/format'
import { paymentStatus } from '../../lib/status'
import { usePagedList } from '../../lib/usePagedList'
import './Payments.css'

// PaymentStatus.Paid — the only state a refund can be issued against.
const PAID = 1

export default function Payments({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const token = session.accessToken

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Payments</h1>
          <p>Transactions and refunds</p>
        </div>
      </div>

      <Summary token={token} onLogout={onLogout} />

      <Transactions token={token} onLogout={onLogout} />
    </>
  )
}

/* ---------- Summary cards (Total Revenue + Pending Payouts) ---------- */

function SummaryCard({
  icon,
  label,
  value,
  loading,
}: {
  icon: ReactNode
  label: string
  value: string
  loading: boolean
}) {
  return (
    <div className="pay-stat">
      <span className="pay-stat__icon">{icon}</span>
      <span className="pay-stat__label">{label}</span>
      {loading ? (
        <span className="pay-stat__value skeleton skeleton--value" />
      ) : (
        <span className="pay-stat__value">{value}</span>
      )}
    </div>
  )
}

function Summary({ token, onLogout }: { token: string; onLogout: () => void }) {
  const [data, setData] = useState<PaymentsSummary | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let alive = true
    loadPaymentsSummary(token)
      .then((s) => alive && setData(s))
      .catch((err) => {
        if (err instanceof UnauthorizedError) onLogout()
        // A summary failure shouldn't blank the whole page; cards just stay at —.
        if (alive) setData({ totalRevenue: 0, pendingPayouts: 0 })
      })
      .finally(() => alive && setLoading(false))
    return () => {
      alive = false
    }
  }, [token, onLogout])

  return (
    <section className="pay-stats">
      <SummaryCard
        loading={loading}
        icon={<RevenueIcon size={20} />}
        label="Total Revenue"
        value={data ? moneyCompact(data.totalRevenue) : ''}
      />
      <SummaryCard
        loading={loading}
        icon={<CardIcon size={20} />}
        label="Pending Payouts"
        value={data ? moneyFull(data.pendingPayouts) : ''}
      />
    </section>
  )
}

/* ---------- Transactions ---------- */

function txnRef(p: PaymentRow): string {
  return p.transactionReference || `#${p.id.slice(0, 8).toUpperCase()}`
}

function Transactions({
  token,
  onLogout,
}: {
  token: string
  onLogout: () => void
}) {
  const { page, setPage, data, loading, error, pageSize, reload } = usePagedList(
    getPayments,
    token,
    onLogout,
  )
  const [refunding, setRefunding] = useState<PaymentRow | null>(null)

  if (error) {
    return (
      <div className="panelbox">
        <p>{error}</p>
        <button type="button" className="btn btn--sm" onClick={() => void reload()}>
          Retry
        </button>
      </div>
    )
  }

  return (
    <section className="card">
      <div className="tablewrap">
        <table className="table">
          <thead>
            <tr>
              <th>Txn ID</th>
              <th>Booking</th>
              <th>Customer</th>
              <th>Provider</th>
              <th>Amount</th>
              <th>Net Payout</th>
              <th>Status</th>
              <th>Date</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {loading &&
              Array.from({ length: pageSize }).map((_, i) => (
                <tr key={i}>
                  {Array.from({ length: 9 }).map((__, j) => (
                    <td key={j}>
                      <span className="skeleton skeleton--line" />
                    </td>
                  ))}
                </tr>
              ))}

            {!loading &&
              data?.data.map((p, i) => (
                <tr key={p.id}>
                  <td className="mono">{txnRef(p)}</td>
                  <td className="mono">#{p.bookingId.slice(0, 8).toUpperCase()}</td>
                  <td>
                    <div className="cell-user">
                      <Avatar name={p.customerName ?? '—'} tone={i} />
                      <span>{p.customerName ?? '—'}</span>
                    </div>
                  </td>
                  <td className="muted">{p.providerName || '—'}</td>
                  <td>
                    <div className="pay-amount__main">{moneyFull(p.amount)}</div>
                    <div className="pay-amount__sub">
                      Comm. {moneyFull(p.commissionAmount)}
                    </div>
                  </td>
                  <td className="muted">{moneyFull(p.providerEarning)}</td>
                  <td>
                    <Badge {...paymentStatus(p.paymentStatus)} />
                  </td>
                  <td className="muted">{p.paidAt ? fullDate(p.paidAt) : '—'}</td>
                  <td>
                    <button
                      type="button"
                      className="pay-act pay-act--refund"
                      disabled={p.paymentStatus !== PAID}
                      onClick={() => setRefunding(p)}
                    >
                      Refund
                    </button>
                  </td>
                </tr>
              ))}

            {!loading && data?.data.length === 0 && (
              <tr>
                <td colSpan={9} className="empty">
                  No transactions found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {data && (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={data.totalCount}
          totalPages={data.totalPages}
          onPage={setPage}
        />
      )}

      {refunding && (
        <RefundModal
          payment={refunding}
          token={token}
          onClose={() => setRefunding(null)}
          onUnauthorized={onLogout}
          onDone={() => {
            setRefunding(null)
            void reload()
          }}
        />
      )}
    </section>
  )
}

/* ---------- Refund modal ---------- */

function RefundModal({
  payment,
  token,
  onClose,
  onUnauthorized,
  onDone,
}: {
  payment: PaymentRow
  token: string
  onClose: () => void
  onUnauthorized: () => void
  onDone: () => void
}) {
  const [amount, setAmount] = useState(String(payment.amount))
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !busy) onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [busy, onClose])

  const submit = useCallback(async () => {
    const refundAmount = Number(amount)
    if (!Number.isFinite(refundAmount) || refundAmount <= 0) {
      setError('Enter a refund amount greater than zero.')
      return
    }
    if (refundAmount > payment.amount) {
      setError(`Refund cannot exceed the charge (${moneyFull(payment.amount)}).`)
      return
    }
    setBusy(true)
    setError(null)
    try {
      await issueRefund(token, {
        paymentId: payment.id,
        reason: reason.trim(),
        refundAmount,
      })
      onDone()
    } catch (err) {
      if (err instanceof UnauthorizedError) {
        onUnauthorized()
        return
      }
      setError(err instanceof Error ? err.message : 'Refund failed.')
    } finally {
      setBusy(false)
    }
  }, [amount, reason, payment, token, onDone, onUnauthorized])

  return (
    <div className="modal-root" role="dialog" aria-modal="true" aria-label="Issue refund">
      <div className="modal-overlay" onClick={busy ? undefined : onClose} />
      <div className="modal">
        <h3 className="modal__title">Issue refund</h3>
        <p className="modal__msg">
          Refunding {txnRef(payment)}
          {payment.customerName ? ` — ${payment.customerName}` : ''}. This cancels the
          related booking.
        </p>

        <div className="pay-field">
          <label htmlFor="refund-amount">Amount (max {moneyFull(payment.amount)})</label>
          <input
            id="refund-amount"
            type="number"
            min="0"
            step="0.01"
            value={amount}
            disabled={busy}
            onChange={(e) => setAmount(e.target.value)}
          />
        </div>

        <div className="pay-field">
          <label htmlFor="refund-reason">Reason</label>
          <textarea
            id="refund-reason"
            value={reason}
            disabled={busy}
            placeholder="Why is this being refunded?"
            onChange={(e) => setReason(e.target.value)}
          />
        </div>

        {error && <div className="inline-error">{error}</div>}

        <div className="modal__actions">
          <button type="button" className="pay-act" onClick={onClose} disabled={busy}>
            Cancel
          </button>
          <button
            type="button"
            className="btn btn--sm modal__confirm"
            onClick={() => void submit()}
            disabled={busy}
          >
            {busy ? <Spinner /> : 'Refund'}
          </button>
        </div>
      </div>
    </div>
  )
}
