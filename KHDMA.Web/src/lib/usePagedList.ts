import { useCallback, useEffect, useState } from 'react'
import { UnauthorizedError, type PagedResponse } from '../api/admin'

interface ListOpts {
  search: string
  page: number
  pageSize: number
}

/**
 * Drives a searchable, paginated list backed by an admin endpoint.
 * `fetchFn` and `token` must be stable references.
 */
export function usePagedList<T>(
  fetchFn: (token: string, opts: ListOpts) => Promise<PagedResponse<T>>,
  token: string,
  onUnauthorized: () => void,
  pageSize = 10,
) {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [debounced, setDebounced] = useState('')
  const [data, setData] = useState<PagedResponse<T> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Debounce the search box; a new query always returns to page 1.
  useEffect(() => {
    const t = setTimeout(() => {
      setDebounced(search)
      setPage(1)
    }, 350)
    return () => clearTimeout(t)
  }, [search])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await fetchFn(token, { search: debounced, page, pageSize }))
    } catch (err) {
      if (err instanceof UnauthorizedError) {
        onUnauthorized()
        return
      }
      setError(err instanceof Error ? err.message : 'Failed to load.')
    } finally {
      setLoading(false)
    }
  }, [fetchFn, token, onUnauthorized, debounced, page, pageSize])

  useEffect(() => {
    void load()
  }, [load])

  return {
    page,
    setPage,
    search,
    setSearch,
    data,
    loading,
    error,
    pageSize,
    reload: load,
  }
}
