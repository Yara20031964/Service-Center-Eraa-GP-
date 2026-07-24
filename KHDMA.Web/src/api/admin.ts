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
