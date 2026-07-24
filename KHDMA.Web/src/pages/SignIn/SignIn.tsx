import { useState } from 'react'
import { adminLogin, saveSession, type TokenResponse } from '../../api/auth'
import {
  AlertIcon,
  CheckIcon,
  EyeIcon,
  LockIcon,
  LogoMark,
  MailIcon,
  Spinner,
} from '../../components/icons'
import './SignIn.css'

const FEATURES = [
  'Real-time booking dispatch & live monitoring',
  'Provider verification, moderation & payouts',
  'Revenue, commissions & operations analytics',
]

export default function SignIn({
  onSuccess,
}: {
  onSuccess: (token: TokenResponse) => void
}) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [remember, setRemember] = useState(true)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit() {
    setError(null)
    setLoading(true)
    try {
      const token = await adminLogin(email.trim(), password)
      if (remember) saveSession(token)
      onSuccess(token)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setLoading(false)
    }
  }

  const year = new Date().getFullYear()

  return (
    <div className="auth">
      {/* ---------- Brand / marketing panel ---------- */}
      <aside className="brand">
        <div className="brand__grid" aria-hidden="true" />
        <div className="brand__glow" aria-hidden="true" />

        <div className="brand__content">
          <div className="brand__logo">
            <span className="brand__logo-mark">
              <LogoMark />
            </span>
            <span className="brand__logo-text">
              KHDMA<span>Admin</span>
            </span>
          </div>

          <div className="brand__mid">
            <h1 className="brand__headline">
              Run your service marketplace from a single console.
            </h1>
            <p className="brand__lead">
              Dispatch jobs, vet providers, track every payment and keep customers
              happy — all in one place.
            </p>
            <ul className="brand__features">
              {FEATURES.map((f) => (
                <li key={f}>
                  <span className="brand__check">
                    <CheckIcon />
                  </span>
                  {f}
                </li>
              ))}
            </ul>
          </div>

          <div className="brand__foot">
            <div className="brand__avatars" aria-hidden="true">
              <span style={{ background: '#f6c453', color: '#7a4d00' }}>MH</span>
              <span style={{ background: '#8ecae6', color: '#023047' }}>KA</span>
              <span style={{ background: '#ffb4a2', color: '#6b1414' }}>SM</span>
              <span className="brand__avatars-more">+200</span>
            </div>
            <p>Powering the KHDMA operations team every day.</p>
          </div>
        </div>
      </aside>

      {/* ---------- Sign-in panel ---------- */}
      <main className="panel">
        <div className="panel__inner">
          <div className="panel__brand">
            <span className="panel__brand-mark">
              <LogoMark size={18} />
            </span>
            <span className="panel__brand-text">
              KHDMA<span>Admin</span>
            </span>
          </div>

          <header className="panel__head">
            <span className="panel__eyebrow">Admin Console</span>
            <h2>Welcome back</h2>
            <p>Sign in to your account to continue.</p>
          </header>

          {error && (
            <div className="alert" role="alert">
              <AlertIcon />
              <span>{error}</span>
            </div>
          )}

          <form
            onSubmit={(e) => {
              e.preventDefault()
              void handleSubmit()
            }}
            noValidate
          >
            <div className="field">
              <label htmlFor="email">Email address</label>
              <div className="control">
                <span className="control__icon">
                  <MailIcon />
                </span>
                <input
                  id="email"
                  type="email"
                  autoComplete="username"
                  placeholder="you@khdma.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={loading}
                  required
                />
              </div>
            </div>

            <div className="field">
              <label htmlFor="password">Password</label>
              <div className="control">
                <span className="control__icon">
                  <LockIcon />
                </span>
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={loading}
                  required
                />
                <button
                  type="button"
                  className="control__toggle"
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  tabIndex={-1}
                >
                  <EyeIcon off={showPassword} />
                </button>
              </div>
            </div>

            <label className="checkbox">
              <input
                type="checkbox"
                checked={remember}
                onChange={(e) => setRemember(e.target.checked)}
              />
              <span className="checkbox__box" aria-hidden="true">
                <CheckIcon />
              </span>
              <span>Keep me signed in</span>
            </label>

            <button type="submit" className="btn" disabled={loading}>
              {loading ? (
                <>
                  <Spinner /> Signing in…
                </>
              ) : (
                'Sign in'
              )}
            </button>
          </form>

          <p className="panel__help">
            Need access? <a href="#support">Contact your administrator</a>
          </p>
        </div>

        <footer className="panel__footer">
          <span>© {year} KHDMA · Admin Console</span>
          <span className="panel__version">v1.0</span>
        </footer>
      </main>
    </div>
  )
}
