// Admin Reviews (moderation) API client.
import { UnauthorizedError, type PagedResponse } from './admin'

export interface ReviewItem {
  id: string
  bookingId: string
  customerName: string
  providerName: string
  rating: number
  comment: string | null
  punctualityRating: number | null
  workQualityRating: number | null
  cleanlinesRating: number | null
  createAt: string
  isHidden: boolean
  // Present only once the backend adds them to ReviewDto; optional so the page
  // renders a provider reply automatically if/when it's returned.
  providerReply?: string | null
  providerReplyAt?: string | null
}

interface Wrapped<T> {
  success: boolean
  message?: string
  data: T
}

async function parse<T>(res: Response): Promise<T> {
  if (res.status === 401 || res.status === 403) throw new UnauthorizedError()
  const text = await res.text()
  const body = text ? JSON.parse(text) : {}
  if (!res.ok) throw new Error(body?.message || `Request failed (${res.status}).`)
  return body as T
}

async function authGet<T>(path: string, token: string): Promise<T> {
  let res: Response
  try {
    res = await fetch(path, { headers: { Authorization: `Bearer ${token}` } })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  return parse<T>(res)
}

export interface ReviewQuery {
  page: number
  pageSize: number
  minRating?: number
  maxRating?: number
}

export function getReviews(
  token: string,
  q: ReviewQuery,
): Promise<PagedResponse<ReviewItem>> {
  const params = new URLSearchParams({
    page: String(q.page),
    pageSize: String(q.pageSize),
  })
  if (q.minRating != null) params.set('minRating', String(q.minRating))
  if (q.maxRating != null) params.set('maxRating', String(q.maxRating))
  return authGet<PagedResponse<ReviewItem>>(`/api/admin/reviews?${params}`, token)
}

export async function getReviewDetails(
  token: string,
  id: string,
): Promise<ReviewItem> {
  const r = await authGet<Wrapped<ReviewItem>>(`/api/admin/reviews/${id}`, token)
  return r.data
}

/** Moderate a review: hide/unhide and/or soft-delete. */
export async function updateReviewStatus(
  token: string,
  id: string,
  opts: { isDeleted: boolean; isHidden: boolean },
): Promise<void> {
  const params = new URLSearchParams({
    isDeleted: String(opts.isDeleted),
    isHidden: String(opts.isHidden),
  })
  let res: Response
  try {
    res = await fetch(`/api/admin/reviews/${id}/status?${params}`, {
      method: 'PUT',
      headers: { Authorization: `Bearer ${token}` },
      // Empty body guarantees a Content-Length header (the host returns 411
      // "Length Required" for a body-less PUT).
      body: '',
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  await parse<unknown>(res)
}
