// Admin Bookings API client.
import { UnauthorizedError, type PagedResponse } from './admin'

export interface BookingListItem {
  id: string
  customerName: string
  providerName: string | null
  serviceName: string | null
  status: number
  totalPrice: number
  scheduledTime: string | null
  createAt: string
}

export interface PaymentDetails {
  id: string
  bookingId: string
  amount: number
  commissionAmount: number
  providerEarning: number
  paymentStatus: number
  transactionReference: string | null
  paidAt: string | null
}

export interface BookingDetail {
  id: string
  customerId: string
  customerName: string
  providerId: string | null
  providerName: string | null
  serviceId: string
  serviceName: string | null
  bookingType: number
  status: number
  scheduledTime: string | null
  address: string | null
  totalPrice: number
  notes: string | null
  cancelReason: string | null
  createAt: string
  paymentDetails: PaymentDetails | null
  reviewDetails: { rating: number; comment: string | null } | null
}

export interface ChatMessage {
  messageId: string
  senderName: string
  messageText: string
  messageType: string
  sentAt: string
  attachmentUrl: string | null
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

export interface BookingQuery {
  page: number
  pageSize: number
  status?: number
  fromDate?: string
  toDate?: string
}

export function getBookings(
  token: string,
  q: BookingQuery,
): Promise<PagedResponse<BookingListItem>> {
  const p = new URLSearchParams({ page: String(q.page), pageSize: String(q.pageSize) })
  if (q.status != null) p.set('status', String(q.status))
  if (q.fromDate) p.set('fromDate', q.fromDate)
  if (q.toDate) p.set('toDate', q.toDate)
  return authGet<PagedResponse<BookingListItem>>(`/api/admin/bookings?${p}`, token)
}

export async function getBookingDetails(
  token: string,
  id: string,
): Promise<BookingDetail> {
  const r = await authGet<Wrapped<BookingDetail>>(`/api/admin/bookings/${id}`, token)
  return r.data
}

export async function getTranscript(
  token: string,
  id: string,
): Promise<ChatMessage[]> {
  const r = await authGet<PagedResponse<ChatMessage>>(
    `/api/admin/bookings/${id}/transcript?page=1&pageSize=200`,
    token,
  )
  return r.data ?? []
}

/** Force-cancel a booking. The endpoint binds a raw JSON string reason. */
export async function cancelBooking(
  token: string,
  id: string,
  reason: string,
): Promise<void> {
  let res: Response
  try {
    res = await fetch(`/api/admin/bookings/${id}/cancel`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(reason),
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  await parse<unknown>(res)
}
