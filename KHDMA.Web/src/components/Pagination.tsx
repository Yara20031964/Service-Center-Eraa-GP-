// Page numbers with leading/trailing ellipsis.
function pageList(current: number, total: number): (number | '…')[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)
  const out: (number | '…')[] = [1]
  const start = Math.max(2, current - 1)
  const end = Math.min(total - 1, current + 1)
  if (start > 2) out.push('…')
  for (let p = start; p <= end; p++) out.push(p)
  if (end < total - 1) out.push('…')
  out.push(total)
  return out
}

export default function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPage,
}: {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPage: (p: number) => void
}) {
  if (totalCount === 0) return null
  const from = (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="pagination">
      <span className="pagination__info">
        Showing <strong>{from}</strong>–<strong>{to}</strong> of{' '}
        <strong>{totalCount}</strong>
      </span>
      <div className="pagination__controls">
        <button
          type="button"
          className="pagebtn"
          disabled={page <= 1}
          onClick={() => onPage(page - 1)}
        >
          Prev
        </button>
        {pageList(page, totalPages).map((p, i) =>
          p === '…' ? (
            <span key={`d${i}`} className="pagebtn pagebtn--dots">
              …
            </span>
          ) : (
            <button
              key={p}
              type="button"
              className={`pagebtn${p === page ? ' pagebtn--active' : ''}`}
              onClick={() => onPage(p)}
            >
              {p}
            </button>
          ),
        )}
        <button
          type="button"
          className="pagebtn"
          disabled={page >= totalPages}
          onClick={() => onPage(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}
