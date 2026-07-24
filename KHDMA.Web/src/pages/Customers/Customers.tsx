import type { TokenResponse } from '../../api/auth'
import { getCustomers } from '../../api/admin'
import Avatar from '../../components/Avatar'
import Badge from '../../components/Badge'
import Pagination from '../../components/Pagination'
import { SearchIcon } from '../../components/icons'
import { fullDate } from '../../lib/format'
import { userStatus } from '../../lib/status'
import { usePagedList } from '../../lib/usePagedList'

export default function Customers({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const { page, setPage, search, setSearch, data, loading, error, pageSize, reload } =
    usePagedList(getCustomers, session.accessToken, onLogout)

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Customers</h1>
          <p>
            {data ? `${data.totalCount} registered customers` : 'Registered customers'}
          </p>
        </div>
        <div className="page-search">
          <SearchIcon size={18} />
          <input
            type="search"
            placeholder="Search customers by name…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {error ? (
        <div className="panelbox">
          <p>{error}</p>
          <button type="button" className="btn btn--sm" onClick={() => void reload()}>
            Retry
          </button>
        </div>
      ) : (
        <section className="card">
          <div className="tablewrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Customer</th>
                  <th>Phone</th>
                  <th>Status</th>
                  <th>Joined</th>
                </tr>
              </thead>
              <tbody>
                {loading &&
                  Array.from({ length: pageSize }).map((_, i) => (
                    <tr key={i}>
                      {Array.from({ length: 4 }).map((__, j) => (
                        <td key={j}>
                          <span className="skeleton skeleton--line" />
                        </td>
                      ))}
                    </tr>
                  ))}

                {!loading &&
                  data?.data.map((c, i) => (
                    <tr key={c.id}>
                      <td>
                        <div className="cell-user">
                          <Avatar name={c.fullName} tone={i} />
                          <div>
                            <div className="cell-user__name">{c.fullName}</div>
                            <div className="cell-user__sub">{c.email}</div>
                          </div>
                        </div>
                      </td>
                      <td className="muted">{c.phone || '—'}</td>
                      <td>
                        <Badge {...userStatus(c.status)} />
                      </td>
                      <td className="muted">{fullDate(c.createdAt)}</td>
                    </tr>
                  ))}

                {!loading && data?.data.length === 0 && (
                  <tr>
                    <td colSpan={4} className="empty">
                      No customers found.
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
        </section>
      )}
    </>
  )
}
