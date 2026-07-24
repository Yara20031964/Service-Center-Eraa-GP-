// Admin dashboard API client. All calls go through the relative `/api` path,
// which Vite proxies to production (see vite.config.ts), authenticated with the
// admin access token.

/** Thrown when the API rejects the token (expired / invalid). */
export class UnauthorizedError extends Error {
  constructor() {
    super('Your session has expired. Please sign in again.')
    this.name = 'UnauthorizedError'
  }
}

export interface PagedResponse<T> {
  success: boolean
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

interface ApiResponse<T> {
  success: boolean
  data: T
}

const qs = (o: Record<string, string | number>) =>
  Object.entries(o)
    .filter(([, v]) => v !== '' && v !== undefined && v !== null)
    .map(([k, v]) => `${k}=${encodeURIComponent(v)}`)
    .join('&')

async function authGet<T>(path: string, token: string): Promise<T> {
  let res: Response
  try {
    res = await fetch(path, { headers: { Authorization: `Bearer ${token}` } })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  if (res.status === 401 || res.status === 403) throw new UnauthorizedError()
  if (!res.ok) throw new Error(`Request failed (${res.status}).`)
  return (await res.json()) as T
}

/** POST/PUT with the admin token; surfaces the server's message on failure. */
async function authSend(
  path: string,
  token: string,
  method: 'POST' | 'PUT',
  body?: unknown,
): Promise<void> {
  let res: Response
  try {
    res = await fetch(path, {
      method,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  if (res.status === 401 || res.status === 403) throw new UnauthorizedError()
  if (!res.ok) {
    let msg = `Request failed (${res.status}).`
    try {
      const j = (await res.json()) as { message?: string }
      if (j?.message) msg = j.message
    } catch {
      /* no JSON body */
    }
    throw new Error(msg)
  }
}

/* ---------- Response row types ---------- */

export interface BookingRow {
  id: string
  customerName: string | null
  providerName: string | null
  serviceName: string | null
  status: number
  totalPrice: number
  scheduledTime: string | null
  createAt: string
}

export interface PendingProvider {
  id: string
  fullName: string
  email: string
  phone: string | null
  serviceArea: string | null
  hourlyRate: number
  createdAt: string
}

export interface CustomerListItem {
  id: string
  fullName: string
  email: string
  phone: string | null
  profilePhotoUrl: string | null
  status: number
  createdAt: string
  isDeleted: boolean
}

export interface ProviderListItem {
  id: string
  fullName: string
  email: string
  phone: string | null
  profilePhotoUrl: string | null
  status: number
  providerState: number
  availabilityStatus: number
  serviceArea: string | null
  hourlyRate: number
  rating: number
  reviewCount: number
  publicRating: number
  createdAt: string
}

export interface ProviderDetail {
  id: string
  fullName: string
  email: string
  phone: string | null
  profilePhotoUrl: string | null
  status: number
  providerState: number
  availabilityStatus: number
  serviceArea: string | null
  hourlyRate: number
  rating: number
  reviewCount: number
  createdAt: string
}

export interface DashboardData {
  totalUsers: number
  activeProviders: number
  bookingsToday: number
  revenueMtd: number
  recentBookings: BookingRow[]
  pendingProviders: PendingProvider[]
  /** True total of pending applications (may exceed the loaded list). */
  pendingTotal: number
}

/* ---------- Helpers ---------- */

function ymd(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`
}

/** Load everything the dashboard needs in parallel. */
export async function loadDashboard(token: string): Promise<DashboardData> {
  const today = new Date()
  const tomorrow = new Date(today)
  tomorrow.setDate(today.getDate() + 1)
  const monthStart = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-01`

  const [customers, providers, bookingsToday, revenue, recent, pending] =
    await Promise.all([
      authGet<PagedResponse<unknown>>(
        '/api/admin/users/customers?page=1&pageSize=1',
        token,
      ),
      authGet<PagedResponse<unknown>>(
        '/api/admin/users/providers?page=1&pageSize=1',
        token,
      ),
      authGet<PagedResponse<unknown>>(
        `/api/admin/bookings?fromDate=${ymd(today)}&toDate=${ymd(tomorrow)}&page=1&pageSize=1`,
        token,
      ),
      authGet<ApiResponse<{ totalRevenue: number }>>(
        `/api/admin/reports/revenue?period=monthly&dateFrom=${monthStart}`,
        token,
      ),
      authGet<PagedResponse<BookingRow>>(
        '/api/admin/bookings?page=1&pageSize=5',
        token,
      ),
      authGet<PagedResponse<PendingProvider>>(
        '/api/admin/users/providers/pending?page=1&pageSize=20',
        token,
      ),
    ])

  return {
    // "Total Users" = customers + providers.
    totalUsers: customers.totalCount + providers.totalCount,
    activeProviders: providers.totalCount,
    bookingsToday: bookingsToday.totalCount,
    revenueMtd: revenue.data?.totalRevenue ?? 0,
    recentBookings: recent.data ?? [],
    pendingProviders: pending.data ?? [],
    pendingTotal: pending.totalCount,
  }
}

/** Paginated list of customers (optionally filtered by search). */
export function getCustomers(
  token: string,
  opts: { search?: string; page: number; pageSize: number },
): Promise<PagedResponse<CustomerListItem>> {
  const query = qs({ search: opts.search ?? '', page: opts.page, pageSize: opts.pageSize })
  return authGet<PagedResponse<CustomerListItem>>(
    `/api/admin/users/customers?${query}`,
    token,
  )
}

/** Paginated list of (active) providers. */
export function getProviders(
  token: string,
  opts: { search?: string; page: number; pageSize: number },
): Promise<PagedResponse<ProviderListItem>> {
  const query = qs({ search: opts.search ?? '', page: opts.page, pageSize: opts.pageSize })
  return authGet<PagedResponse<ProviderListItem>>(
    `/api/admin/users/providers?${query}`,
    token,
  )
}

/** Fetch a single provider's full detail record. */
export async function getProviderDetails(
  token: string,
  id: string,
): Promise<ProviderDetail> {
  const res = await authGet<ApiResponse<ProviderDetail>>(
    `/api/admin/users/providers/${id}`,
    token,
  )
  return res.data
}

/* ---------- Payments ---------- */

/** A row in the Transactions table (mirrors PaymentDto; enums arrive as ints). */
export interface PaymentRow {
  id: string
  bookingId: string
  amount: number
  commissionAmount: number
  providerEarning: number
  /** PaymentStatus enum: 0 Pending, 1 Paid, 2 Failed, 3 Refunded. */
  paymentStatus: number
  transactionReference: string | null
  paidAt: string | null
  providerName: string | null
  customerName: string | null
}

/** A row in the Provider Payouts table (mirrors PayoutDto). */
export interface PayoutRow {
  id: string
  providerId: string
  providerName: string | null
  amount: number
  status: string
  createdAt: string
}

/** The two summary figures we can source today (from the dashboard summary). */
export interface PaymentsSummary {
  totalRevenue: number
  pendingPayouts: number
}

/** Paginated transactions. `search` is accepted for usePagedList but unused. */
export function getPayments(
  token: string,
  opts: { search?: string; page: number; pageSize: number },
): Promise<PagedResponse<PaymentRow>> {
  const query = qs({ page: opts.page, pageSize: opts.pageSize })
  return authGet<PagedResponse<PaymentRow>>(`/api/admin/payments?${query}`, token)
}

/** Paginated provider payouts. `search` is accepted for usePagedList but unused. */
export function getPayouts(
  token: string,
  opts: { search?: string; page: number; pageSize: number },
): Promise<PagedResponse<PayoutRow>> {
  const query = qs({ page: opts.page, pageSize: opts.pageSize })
  return authGet<PagedResponse<PayoutRow>>(`/api/admin/payouts?${query}`, token)
}

/** Total revenue + pending payouts, read from the dashboard summary endpoint. */
export async function loadPaymentsSummary(token: string): Promise<PaymentsSummary> {
  const res = await authGet<
    ApiResponse<{ revenue: { allTime: number; pendingPayouts: number } }>
  >('/api/admin/dashboard/summary', token)
  return {
    totalRevenue: res.data?.revenue?.allTime ?? 0,
    pendingPayouts: res.data?.revenue?.pendingPayouts ?? 0,
  }
}

/** Issue a (full or partial) refund against a payment. */
export function issueRefund(
  token: string,
  body: { paymentId: string; reason: string; refundAmount: number },
): Promise<void> {
  return authSend('/api/admin/payments/refund', token, 'POST', body)
}

/** Approve a pending provider payout. */
export function approvePayout(token: string, id: string): Promise<void> {
  return authSend(`/api/admin/payouts/${id}/approve`, token, 'PUT')
}

/** Approve or reject a pending provider application. */
export async function decideProvider(
  token: string,
  id: string,
  isApproved: boolean,
  reason?: string,
): Promise<void> {
  let res: Response
  try {
    res = await fetch(`/api/admin/users/providers/${id}/approve-reject`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ isApproved, reason }),
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  if (res.status === 401 || res.status === 403) throw new UnauthorizedError()
  if (!res.ok) throw new Error(`Request failed (${res.status}).`)
}
