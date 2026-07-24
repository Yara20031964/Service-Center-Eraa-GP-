import { useCallback, useEffect, useState } from 'react'
import type { TokenResponse } from '../../api/auth'
import { UnauthorizedError, type PagedResponse } from '../../api/admin'
import {
  getReviews,
  updateReviewStatus,
  type ReviewItem,
} from '../../api/reviews'
import Avatar from '../../components/Avatar'
import Badge from '../../components/Badge'
import ConfirmDialog from '../../components/ConfirmDialog'
import Pagination from '../../components/Pagination'
import { Spinner } from '../../components/icons'
import { fullDate, fullDateTime } from '../../lib/format'
import './Reviews.css'

const PAGE_SIZE = 10

function Stars({ value }: { value: number }) {
  return (
    <span className="stars" aria-label={`${value} out of 5`}>
      {[1, 2, 3, 4, 5].map((n) => (
        <svg key={n} viewBox="0 0 24 24" width="15" height="15" aria-hidden="true">
          <path
            d="m12 3.5 2.6 5.3 5.9.8-4.3 4.1 1 5.8L12 17l-5.2 2.8 1-5.8-4.3-4.1 5.9-.8z"
            fill={n <= value ? '#f5a623' : 'none'}
            stroke={n <= value ? '#f5a623' : '#cbd5e1'}
            strokeWidth="1.4"
            strokeLinejoin="round"
          />
        </svg>
      ))}
    </span>
  )
}

function statusBadge(isHidden: boolean) {
  return isHidden
    ? ({ label: 'Hidden', tone: 'gray' } as const)
    : ({ label: 'Active', tone: 'green' } as const)
}

const shortId = (id: string) => id.slice(0, 4).toUpperCase()

export default function Reviews({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const token = session.accessToken
  const [page, setPage] = useState(1)
  const [rating, setRating] = useState(0) // 0 = all
  const [data, setData] = useState<PagedResponse<ReviewItem> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [selected, setSelected] = useState<ReviewItem | null>(null)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [confirmDel, setConfirmDel] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await getReviews(token, {
        page,
        pageSize: PAGE_SIZE,
        minRating: rating || undefined,
        maxRating: rating || undefined,
      })
      setData(res)
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setError(err instanceof Error ? err.message : 'Failed to load reviews.')
    } finally {
      setLoading(false)
    }
  }, [token, page, rating, onLogout])

  useEffect(() => {
    void load()
  }, [load])

  function open(r: ReviewItem) {
    setActionError(null)
    setSelected(r)
  }

  function patchLocal(id: string, patch: Partial<ReviewItem>) {
    setData((d) =>
      d ? { ...d, data: d.data.map((x) => (x.id === id ? { ...x, ...patch } : x)) } : d,
    )
    setSelected((s) => (s && s.id === id ? { ...s, ...patch } : s))
  }

  async function toggleHidden(r: ReviewItem) {
    setBusy(true)
    setActionError(null)
    try {
      await updateReviewStatus(token, r.id, {
        isDeleted: false,
        isHidden: !r.isHidden,
      })
      patchLocal(r.id, { isHidden: !r.isHidden })
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setActionError(err instanceof Error ? err.message : 'Action failed.')
    } finally {
      setBusy(false)
    }
  }

  async function remove(r: ReviewItem) {
    setBusy(true)
    setActionError(null)
    try {
      await updateReviewStatus(token, r.id, { isDeleted: true, isHidden: false })
      setConfirmDel(false)
      setSelected(null)
      await load()
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setActionError(err instanceof Error ? err.message : 'Delete failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Reviews</h1>
          <p>{data ? `${data.totalCount} reviews` : 'Customer reviews & moderation'}</p>
        </div>
        <label className="rating-filter">
          <span>Rating</span>
          <select
            value={rating}
            onChange={(e) => {
              setRating(Number(e.target.value))
              setPage(1)
            }}
          >
            <option value={0}>All ratings</option>
            {[5, 4, 3, 2, 1].map((n) => (
              <option key={n} value={n}>
                {n} star{n > 1 ? 's' : ''}
              </option>
            ))}
          </select>
        </label>
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
                  <th>Reviewer</th>
                  <th>Provider</th>
                  <th>Rating</th>
                  <th>Review</th>
                  <th>Status</th>
                  <th>Date</th>
                </tr>
              </thead>
              <tbody>
                {loading &&
                  Array.from({ length: PAGE_SIZE }).map((_, i) => (
                    <tr key={i}>
                      {Array.from({ length: 6 }).map((__, j) => (
                        <td key={j}>
                          <span className="skeleton skeleton--line" />
                        </td>
                      ))}
                    </tr>
                  ))}

                {!loading &&
                  data?.data.map((r, i) => (
                    <tr key={r.id} className="row-click" onClick={() => open(r)}>
                      <td>
                        <div className="cell-user">
                          <Avatar name={r.customerName} tone={i} />
                          <span className="cell-user__name">{r.customerName}</span>
                        </div>
                      </td>
                      <td className="muted">{r.providerName}</td>
                      <td>
                        <Stars value={r.rating} />
                      </td>
                      <td className="review-cell">{r.comment || '—'}</td>
                      <td>
                        <Badge {...statusBadge(r.isHidden)} />
                      </td>
                      <td className="muted">{fullDate(r.createAt)}</td>
                    </tr>
                  ))}

                {!loading && data?.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="empty">
                      No reviews found.
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

      {/* -------- Detail modal -------- */}
      {selected && (
        <div className="modal-root" role="dialog" aria-modal="true" aria-label="Review">
          <div
            className="modal-overlay"
            onClick={busy ? undefined : () => setSelected(null)}
          />
          <div className="review-modal">
            <button
              type="button"
              className="review-modal__close"
              onClick={() => setSelected(null)}
              aria-label="Close"
            >
              ✕
            </button>

            <div className="review-modal__head">
              <Avatar name={selected.customerName} className="avatar--lg" />
              <div>
                <h3>{selected.customerName}</h3>
                <p>
                  Review ID #REV-{shortId(selected.id)} · {fullDate(selected.createAt)}
                </p>
              </div>
            </div>

            <div className="review-modal__rating">
              <Stars value={selected.rating} />
              <Badge {...statusBadge(selected.isHidden)} />
            </div>

            <p className="review-modal__comment">
              {selected.comment ? `“${selected.comment}”` : 'No comment left.'}
            </p>

            {(selected.punctualityRating ||
              selected.workQualityRating ||
              selected.cleanlinesRating) && (
              <div className="subratings">
                {selected.punctualityRating != null && (
                  <span>Punctuality: <strong>{selected.punctualityRating}/5</strong></span>
                )}
                {selected.workQualityRating != null && (
                  <span>Work quality: <strong>{selected.workQualityRating}/5</strong></span>
                )}
                {selected.cleanlinesRating != null && (
                  <span>Cleanliness: <strong>{selected.cleanlinesRating}/5</strong></span>
                )}
              </div>
            )}

            {selected.providerReply && (
              <div className="review-reply">
                <div className="review-reply__head">
                  <span>Provider reply</span>
                  {selected.providerReplyAt && (
                    <span>{fullDate(selected.providerReplyAt)}</span>
                  )}
                </div>
                <p>“{selected.providerReply}”</p>
              </div>
            )}

            <div className="review-facts">
              <div>
                <span className="review-facts__label">Service Provider</span>
                <span className="review-facts__value">{selected.providerName}</span>
              </div>
              <div>
                <span className="review-facts__label">Booking Reference</span>
                <span className="review-facts__value">
                  #BK-{shortId(selected.bookingId)}
                </span>
              </div>
              <div>
                <span className="review-facts__label">Rating</span>
                <span className="review-facts__value">{selected.rating} / 5</span>
              </div>
              <div>
                <span className="review-facts__label">Submitted</span>
                <span className="review-facts__value">
                  {fullDateTime(selected.createAt)}
                </span>
              </div>
            </div>

            {actionError && <div className="inline-error">{actionError}</div>}

            <div className="review-modal__actions">
              <button
                type="button"
                className="linkbtn"
                onClick={() => setSelected(null)}
                disabled={busy}
              >
                Cancel
              </button>
              <button
                type="button"
                className={selected.isHidden ? 'btn-approve' : 'btn-neutral'}
                onClick={() => void toggleHidden(selected)}
                disabled={busy}
              >
                {busy ? <Spinner /> : selected.isHidden ? 'Restore review' : 'Hide review'}
              </button>
              <button
                type="button"
                className="btn-danger"
                onClick={() => {
                  setActionError(null)
                  setConfirmDel(true)
                }}
                disabled={busy}
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={confirmDel}
        title="Delete review"
        message={`Remove this review by “${selected?.customerName}”? It will no longer be visible.`}
        confirmLabel="Delete"
        busy={busy}
        error={actionError}
        onConfirm={() => selected && void remove(selected)}
        onCancel={() => setConfirmDel(false)}
      />
    </>
  )
}
