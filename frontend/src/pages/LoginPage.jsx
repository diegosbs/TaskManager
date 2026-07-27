import { useState } from 'react'
import { Alert } from '../components/Alert'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../context/AuthContext'

export function LoginPage({ onShowRegister }) {
  const { login } = useAuth()
  const [form, setForm] = useState({
    email: 'demo@taskmanager.local',
    password: 'Demo123!',
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault()
    setError('')
    setLoading(true)

    try {
      await login(form)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <AuthLayout
      eyebrow="Welcome back"
      title="Bring clarity to your day."
      description="A small, focused workspace for planning work, tracking progress, and finishing what matters."
      footer={
        <p>
          New here?{' '}
          <button className="text-button" type="button" onClick={onShowRegister}>
            Create an account
          </button>
        </p>
      }
    >
      <h2>Sign in</h2>
      <p className="card-description">Use the demo account or your own credentials.</p>
      <Alert message={error} />

      <form onSubmit={submit}>
        <label htmlFor="login-email">Email</label>
        <input
          id="login-email"
          type="email"
          autoComplete="email"
          value={form.email}
          onChange={(event) => setForm({ ...form, email: event.target.value })}
          required
        />

        <label htmlFor="login-password">Password</label>
        <input
          id="login-password"
          type="password"
          autoComplete="current-password"
          value={form.password}
          onChange={(event) => setForm({ ...form, password: event.target.value })}
          required
        />

        <button className="button button-primary button-full" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <div className="demo-box">
        <strong>Demo access</strong>
        <span>demo@taskmanager.local</span>
        <span>Demo123!</span>
      </div>
    </AuthLayout>
  )
}
