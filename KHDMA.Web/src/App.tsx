import { useCallback, useState } from 'react'
import { clearSession, getSession, type TokenResponse } from './api/auth'
import AdminLayout, { type PageId } from './layout/AdminLayout'
import SignIn from './pages/SignIn/SignIn'
import Dashboard from './pages/Dashboard/Dashboard'
import Customers from './pages/Customers/Customers'
import Providers from './pages/Providers/Providers'
import Catalog from './pages/Catalog/Catalog'
import Payments from './pages/Payments/Payments'

// App shell: SignIn when signed out; otherwise the admin console with
// state-based navigation between pages.
export default function App() {
  const [session, setSession] = useState<TokenResponse | null>(() => getSession())
  const [page, setPage] = useState<PageId>('dashboard')

  const handleLogout = useCallback(() => {
    clearSession()
    setSession(null)
    setPage('dashboard')
  }, [])

  if (!session) {
    return <SignIn onSuccess={setSession} />
  }

  return (
    <AdminLayout
      session={session}
      onLogout={handleLogout}
      page={page}
      onNavigate={setPage}
    >
      {page === 'dashboard' && <Dashboard session={session} onLogout={handleLogout} />}
      {page === 'customers' && <Customers session={session} onLogout={handleLogout} />}
      {page === 'providers' && <Providers session={session} onLogout={handleLogout} />}
      {page === 'catalog' && <Catalog session={session} onLogout={handleLogout} />}
      {page === 'payments' && <Payments session={session} onLogout={handleLogout} />}
    </AdminLayout>
  )
}
