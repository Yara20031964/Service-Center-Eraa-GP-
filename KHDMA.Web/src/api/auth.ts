// Auth API client for the KHDMA Admin Portal.
//
// All calls go through the relative `/api` path. In development Vite proxies
// this to the PRODUCTION API (https://khdma.runasp.net — see vite.config.ts);
// in production the SPA is served from the same origin as the API.

/** Shape of `TokenResponseDto` returned by the API. */
export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  role: string
  userName: string
}

/** Shape of `AuthResponseDto` returned by the API (camelCase JSON). */
export interface AuthResponse {
  token: TokenResponse | null
  isSuccess: boolean
  errorMessage: string | null
}

const TOKEN_KEY = 'khdma.admin.session'

/**
 * Log in as an administrator via POST /api/auth/login/admin.
 * Throws an Error with a friendly message on failure.
 */
export async function adminLogin(
  email: string,
  password: string,
): Promise<TokenResponse> {
  let res: Response
  try {
    res = await fetch('/api/auth/login/admin', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }

  let data: AuthResponse | null = null
  try {
    data = (await res.json()) as AuthResponse
  } catch {
    // Non-JSON response (e.g. rate-limit or gateway error).
  }

  if (res.status === 429) {
    throw new Error('Too many attempts. Please wait a moment and try again.')
  }

  if (!res.ok || !data?.isSuccess || !data.token) {
    throw new Error(
      data?.errorMessage || 'Invalid email or password. Please try again.',
    )
  }

  return data.token
}

/** Persist the admin session (used by authenticated pages later). */
export function saveSession(token: TokenResponse): void {
  localStorage.setItem(TOKEN_KEY, JSON.stringify(token))
}

/** Read the stored admin session, if any. */
export function getSession(): TokenResponse | null {
  const raw = localStorage.getItem(TOKEN_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as TokenResponse
  } catch {
    return null
  }
}

/** Clear the stored admin session. */
export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY)
}
