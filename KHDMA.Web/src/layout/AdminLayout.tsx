import { useState, type ReactNode } from 'react'
import type { TokenResponse } from '../api/auth'
import Avatar from '../components/Avatar'
import {
  CalendarIcon,
  CardIcon,
  GridIcon,
  LogoMark,
  LogoutIcon,
  ProvidersIcon,
  StarIcon,
  UsersIcon,
  WrenchIcon,
} from '../components/icons'
import './AdminLayout.css'

export type PageId =
  | 'dashboard'
  | 'customers'
  | 'providers'
  | 'catalog'
  | 'payments'
  | 'reviews'
  | 'bookings'

const NAV: { icon: ReactNode; label: string; page?: PageId }[] = [
  { icon: <GridIcon size={19} />, label: 'Dashboard', page: 'dashboard' },
  { icon: <UsersIcon size={19} />, label: 'Customers', page: 'customers' },
  { icon: <ProvidersIcon size={19} />, label: 'Providers', page: 'providers' },
  { icon: <WrenchIcon size={19} />, label: 'Catalog', page: 'catalog' },
  { icon: <CalendarIcon size={19} />, label: 'Bookings', page: 'bookings' },
  { icon: <CardIcon size={19} />, label: 'Payments', page: 'payments' },
  { icon: <StarIcon size={19} />, label: 'Reviews', page: 'reviews' },
]

const COLLAPSE_KEY = 'khdma.sidebar.collapsed'

export default function AdminLayout({
  session,
  onLogout,
  page,
  onNavigate,
  children,
}: {
  session: TokenResponse
  onLogout: () => void
  page: PageId
  onNavigate: (p: PageId) => void
  children: ReactNode
}) {
  const [collapsed, setCollapsed] = useState<boolean>(
    () => localStorage.getItem(COLLAPSE_KEY) === '1',
  )

  function setAndStore(v: boolean) {
    setCollapsed(v)
    localStorage.setItem(COLLAPSE_KEY, v ? '1' : '0')
  }

  return (
    <div className={`admin${collapsed ? ' admin--collapsed' : ''}`}>
      {/* ---------- Sidebar ---------- */}
      <aside className="side">
        <div className="side__top">
          <button
            type="button"
            className="side__brand"
            onClick={() => collapsed && setAndStore(false)}
            title={collapsed ? 'Expand sidebar' : undefined}
            aria-label={collapsed ? 'Expand sidebar' : 'KHDMA Admin'}
          >
            <span className="side__mark">
              <LogoMark size={18} />
            </span>
            {!collapsed && (
              <span className="side__name">
                KHDMA<span>Admin</span>
              </span>
            )}
          </button>
          {!collapsed && (
            <button
              type="button"
              className="side__collapse"
              onClick={() => setAndStore(true)}
              aria-label="Collapse sidebar"
              title="Collapse sidebar"
            >
              ✕
            </button>
          )}
        </div>

        <nav className="side__nav">
          {NAV.map((item) => {
            const active = item.page === page
            const navigable = !!item.page
            return (
              <button
                key={item.label}
                type="button"
                className={`nav-item${active ? ' nav-item--active' : ''}${
                  navigable ? '' : ' nav-item--soon'
                }`}
                aria-current={active ? 'page' : undefined}
                title={collapsed ? item.label : navigable ? undefined : 'Coming soon'}
                onClick={() => item.page && onNavigate(item.page)}
              >
                <span className="nav-item__icon">{item.icon}</span>
                {!collapsed && <span className="nav-item__label">{item.label}</span>}
              </button>
            )
          })}
        </nav>

        <button
          type="button"
          className="side__logout"
          onClick={onLogout}
          title={collapsed ? 'Sign out' : undefined}
          aria-label="Sign out"
        >
          <span className="nav-item__icon">
            <LogoutIcon size={19} />
          </span>
          {!collapsed && <span className="nav-item__label">Sign out</span>}
        </button>
      </aside>

      {/* ---------- Main column ---------- */}
      <div className="main">
        <header className="topbar">
          <div className="topbar__right">
            <div className="user">
              <div className="user__meta">
                <span className="user__name">{session.userName}</span>
                <span className="user__role">{session.role}</span>
              </div>
              <Avatar name={session.userName} />
            </div>
          </div>
        </header>

        <main className="content">{children}</main>
      </div>
    </div>
  )
}
