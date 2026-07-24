import { useCallback, useEffect, useState, type ReactNode } from 'react'
import type { TokenResponse } from '../../api/auth'
import {
  loadDashboard,
  UnauthorizedError,
  type DashboardData,
  type PendingProvider,
} from '../../api/admin'
import Avatar from '../../components/Avatar'
import Badge from '../../components/Badge'
import {
  CalendarIcon,
  ProvidersIcon,
  RevenueIcon,
  UsersIcon,
} from '../../components/icons'
import { countFmt, fullDate, moneyCompact, timeAgo } from '../../lib/format'
import { bookingStatus } from '../../lib/status'
import ProviderDrawer from './ProviderDrawer'
import './Dashboard.css'

function StatCard({
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
    <div className="stat">
      <span className="stat__icon">{icon}</span>
      <span className="stat__label">{label}</span>
      {loading ? (
        <span className="stat__value skeleton skeleton--value" />
      ) : (
        <span className="stat__value">{value}</span>
      )}
    </div>
  )
}

export default function Dashboard({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const token = session.accessToken
  const [data, setData] = useState<DashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [detailProvider, setDetailProvider] = useState<PendingProvider | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await loadDashboard(token))
    } catch (err) {
      if (err instanceof UnauthorizedError) {
        onLogout()
        return
      }
      setError(err instanceof Error ? err.message : 'Failed to load the dashboard.')
    } finally {
      setLoading(false)
    }
  }, [token, onLogout])

  useEffect(() => {
    void load()
  }, [load])

  const applyDecision = useCallback((id: string, isApproved: boolean) => {
    setData((d) =>
      d
        ? {
            ...d,
            pendingProviders: d.pendingProviders.filter((x) => x.id !== id),
            pendingTotal: Math.max(0, d.pendingTotal - 1),
            activeProviders: isApproved ? d.activeProviders + 1 : d.activeProviders,
          }
        : d,
    )
  }, [])

  if (error) {
    return (
      <div className="panelbox">
        <p>{error}</p>
        <button type="button" className="btn btn--sm" onClick={() => void load()}>
          Retry
        </button>
      </div>
    )
  }

  return (
    <>
      {/* Stat cards */}
      <section className="stats">
        <StatCard
          loading={loading}
          icon={<UsersIcon size={20} />}
          label="Total Users"
          value={data ? countFmt.format(data.totalUsers) : ''}
        />
        <StatCard
          loading={loading}
          icon={<ProvidersIcon size={20} />}
          label="Active Providers"
          value={data ? countFmt.format(data.activeProviders) : ''}
        />
        <StatCard
          loading={loading}
          icon={<CalendarIcon size={20} />}
          label="Bookings Today"
          value={data ? countFmt.format(data.bookingsToday) : ''}
        />
        <StatCard
          loading={loading}
          icon={<RevenueIcon size={20} />}
          label="Revenue MTD"
          value={data ? moneyCompact(data.revenueMtd) : ''}
        />
      </section>

      <div className="grid2">
        {/* Recent bookings */}
        <section className="card">
          <div className="card__head">
            <h2>Recent Bookings</h2>
            <button type="button" className="link">
              View All
            </button>
          </div>
          <div className="tablewrap">
            <table className="table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Customer</th>
                  <th>Service</th>
                  <th>Status</th>
                  <th>Date</th>
                </tr>
              </thead>
              <tbody>
                {loading &&
                  Array.from({ length: 4 }).map((_, i) => (
                    <tr key={i}>
                      {Array.from({ length: 5 }).map((__, j) => (
                        <td key={j}>
                          <span className="skeleton skeleton--line" />
                        </td>
                      ))}
                    </tr>
                  ))}
                {!loading &&
                  data?.recentBookings.map((b, i) => (
                    <tr key={b.id}>
                      <td className="mono">#{b.id.slice(0, 4).toUpperCase()}</td>
                      <td>
                        <div className="cell-user">
                          <Avatar name={b.customerName ?? '—'} tone={i} />
                          <span>{b.customerName ?? '—'}</span>
                        </div>
                      </td>
                      <td>{b.serviceName ?? '—'}</td>
                      <td>
                        <Badge {...bookingStatus(b.status)} />
                      </td>
                      <td className="muted">{fullDate(b.createAt)}</td>
                    </tr>
                  ))}
                {!loading && data?.recentBookings.length === 0 && (
                  <tr>
                    <td colSpan={5} className="empty">
                      No bookings yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>

        {/* Pending approvals */}
        <section className="card">
          <div className="card__head">
            <h2>Pending Approvals</h2>
            {!loading && data && <span className="pill">{data.pendingTotal}</span>}
          </div>

          <div className="approvals">
            {loading &&
              Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="approval">
                  <span className="skeleton skeleton--avatar" />
                  <div style={{ flex: 1 }}>
                    <span className="skeleton skeleton--line" />
                    <span
                      className="skeleton skeleton--line"
                      style={{ width: '60%', marginTop: 6 }}
                    />
                  </div>
                </div>
              ))}

            {!loading &&
              data?.pendingProviders.map((p, i) => (
                <div key={p.id} className="approval">
                  <div className="approval__top">
                    <Avatar name={p.fullName} tone={i + 1} />
                    <div className="approval__meta">
                      <span className="approval__name">{p.fullName}</span>
                      <span className="approval__sub">{p.serviceArea || p.email}</span>
                      <span className="approval__time">
                        Applied {timeAgo(p.createdAt)}
                      </span>
                    </div>
                  </div>
                  <button
                    type="button"
                    className="approval__view"
                    onClick={() => setDetailProvider(p)}
                  >
                    View details
                  </button>
                </div>
              ))}

            {!loading && data?.pendingProviders.length === 0 && (
              <div className="approvals__empty">
                <span className="approvals__empty-badge">✓</span>
                No pending applications. You're all caught up.
              </div>
            )}
          </div>
        </section>
      </div>

      {detailProvider && (
        <ProviderDrawer
          provider={detailProvider}
          token={token}
          onClose={() => setDetailProvider(null)}
          onUnauthorized={onLogout}
          onDecided={(id, isApproved) => {
            applyDecision(id, isApproved)
            setDetailProvider(null)
          }}
        />
      )}
    </>
  )
}
