import { useCallback, useEffect, useState } from 'react'
import type { TokenResponse } from '../../api/auth'
import { UnauthorizedError, type PagedResponse } from '../../api/admin'
import { getBookings, type BookingListItem } from '../../api/bookings'
import Avatar from '../../components/Avatar'
import Badge from '../../components/Badge'
import Pagination from '../../components/Pagination'
import { fullDate, moneyFull } from '../../lib/format'
import { bookingStatus } from '../../lib/status'
import BookingDrawer from './BookingDrawer'
import './Bookings.css'

const PAGE_SIZE = 10

// Statuses offered in the filter (value = BookingStatus int).
const STATUS_OPTIONS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]

const shortId = (id: string) => id.slice(0, 4).toUpperCase()

export default function Bookings({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const token = session.accessToken
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<number | ''>('')
  const [date, setDate] = useState('')
  const [data, setData] = useState<PagedResponse<BookingListItem> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    let fromDate: string | undefined
    let toDate: string | undefined
    if (date) {
      fromDate = date
      const next = new Date(date)
      next.setDate(next.getDate() + 1)
      toDate = next.toISOString().slice(0, 10)
    }
    try {
      const res = await getBookings(token, {
        page,
        pageSize: PAGE_SIZE,
        status: status === '' ? undefined : status,
        fromDate,
        toDate,
      })
      setData(res)
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setError(err instanceof Error ? err.message : 'Failed to load bookings.')
    } finally {
      setLoading(false)
    }
  }, [token, page, status, date, onLogout])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <>
      <div className="page-head">
        <div>
          <h1>All Bookings</h1>
          <p>{data ? `${data.totalCount} bookings` : 'Every booking on the platform'}</p>
        </div>
        <div className="bk-filters">
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value === '' ? '' : Number(e.target.value))
              setPage(1)
            }}
          >
            <option value="">All statuses</option>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {bookingStatus(s).label}
              </option>
            ))}
          </select>
          <input
            type="date"
            value={date}
            onChange={(e) => {
              setDate(e.target.value)
              setPage(1)
            }}
          />
          {date && (
            <button type="button" className="bk-clear" onClick={() => setDate('')}>
              Clear
            </button>
          )}
        </div>
      </div>

      {error ? (
        <div className="panelbox">
          <p>{error}</p>
          <button className="btn btn--sm" onClick={() => void load()}>
            Retry
          </button>
        </div>
      ) : (
        <section className="card">
          <div className="tablewrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Booking</th>
                  <th>Customer</th>
                  <th>Provider</th>
                  <th>Service</th>
                  <th>Status</th>
                  <th>Amount</th>
                  <th>Date</th>
                </tr>
              </thead>
              <tbody>
                {loading &&
                  Array.from({ length: PAGE_SIZE }).map((_, i) => (
                    <tr key={i}>
                      {Array.from({ length: 7 }).map((__, j) => (
                        <td key={j}>
                          <span className="skeleton skeleton--line" />
                        </td>
                      ))}
                    </tr>
                  ))}

                {!loading &&
                  data?.data.map((b, i) => (
                    <tr
                      key={b.id}
                      className="row-click"
                      onClick={() => setSelectedId(b.id)}
                    >
                      <td className="mono">#BK-{shortId(b.id)}</td>
                      <td>
                        <div className="cell-user">
                          <Avatar name={b.customerName} tone={i} />
                          <span className="cell-user__name">{b.customerName}</span>
                        </div>
                      </td>
                      <td className="muted">{b.providerName || '—'}</td>
                      <td className="muted">{b.serviceName || '—'}</td>
                      <td>
                        <Badge {...bookingStatus(b.status)} />
                      </td>
                      <td className="mono">{moneyFull(b.totalPrice)}</td>
                      <td className="muted">{fullDate(b.createAt)}</td>
                    </tr>
                  ))}

                {!loading && data?.data.length === 0 && (
                  <tr>
                    <td colSpan={7} className="empty">
                      No bookings found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          {data && (
            <Pagination
              page={page}
              pageSize={PAGE_SIZE}
              totalCount={data.totalCount}
              totalPages={data.totalPages}
              onPage={setPage}
            />
          )}
        </section>
      )}

      {selectedId && (
        <BookingDrawer
          bookingId={selectedId}
          token={token}
          onClose={() => setSelectedId(null)}
          onUnauthorized={onLogout}
          onCancelled={() => {
            setSelectedId(null)
            void load()
          }}
        />
      )}
    </>
  )
}
