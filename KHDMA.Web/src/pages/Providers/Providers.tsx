import type { TokenResponse } from '../../api/auth'
import { getProviders } from '../../api/admin'
import Avatar from '../../components/Avatar'
import Badge from '../../components/Badge'
import Pagination from '../../components/Pagination'
import { SearchIcon } from '../../components/icons'
import { moneyFull } from '../../lib/format'
import { availability, providerState } from '../../lib/status'
import { usePagedList } from '../../lib/usePagedList'

export default function Providers({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const { page, setPage, search, setSearch, data, loading, error, pageSize, reload } =
    usePagedList(getProviders, session.accessToken, onLogout)

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Providers</h1>
          <p>{data ? `${data.totalCount} active providers` : 'Active providers'}</p>
        </div>
        <div className="page-search">
          <SearchIcon size={18} />
          <input
            type="search"
            placeholder="Search providers…"
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
                  <th>Provider</th>
                  <th>Service area</th>
                  <th>Rate</th>
                  <th>Availability</th>
                  <th>Rating</th>
                  <th>State</th>
                </tr>
              </thead>
              <tbody>
                {loading &&
                  Array.from({ length: pageSize }).map((_, i) => (
                    <tr key={i}>
                      {Array.from({ length: 6 }).map((__, j) => (
                        <td key={j}>
                          <span className="skeleton skeleton--line" />
                        </td>
                      ))}
                    </tr>
                  ))}

                {!loading &&
                  data?.data.map((p, i) => (
                    <tr key={p.id}>
                      <td>
                        <div className="cell-user">
                          <Avatar name={p.fullName} tone={i + 1} />
                          <div>
                            <div className="cell-user__name">{p.fullName}</div>
                            <div className="cell-user__sub">{p.email}</div>
                          </div>
                        </div>
                      </td>
                      <td className="muted">{p.serviceArea || '—'}</td>
                      <td className="muted">{moneyFull(p.hourlyRate)}/hr</td>
                      <td>
                        <Badge {...availability(p.availabilityStatus)} />
                      </td>
                      <td className="muted">
                        {p.reviewCount > 0
                          ? `★ ${p.rating.toFixed(1)} (${p.reviewCount})`
                          : '—'}
                      </td>
                      <td>
                        <Badge {...providerState(p.providerState)} />
                      </td>
                    </tr>
                  ))}

                {!loading && data?.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="empty">
                      No providers found.
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
