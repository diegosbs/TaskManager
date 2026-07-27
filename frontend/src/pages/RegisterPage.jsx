import { useState } from 'react'
import { Alert } from '../components/Alert'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../context/AuthContext'

export function RegisterPage({ onShowLogin }) {
  const { register } = useAuth()
  const [form, setForm] = useState({ name: '', email: '', password: '' })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  function updateField(event) {
    setForm((current) => ({
      ...current,
      [event.target.name]: event.target.value,
    }))
  }

  async function submit(event) {
    event.preventDefault()
    setError('')
    setLoading(true)

    try {
      await register(form)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <AuthLayout
      eyebrow="Get started"
      title="Make progress visible."
      description="Create a private workspace where every task belongs to you and only you can access it."
      footer={
        <p>
          Already registered?{' '}
          <button className="text-button" type="button" onClick={onShowLogin}>
            Sign in
          </button>
        </p>
      }
    >
      <h2>Create account</h2>
      <p className="card-description">You will be signed in after registration.</p>
      <Alert message={error} />

      <form onSubmit={submit}>
        <label htmlFor="register-name">Name</label>
        <input
          id="register-name"
          name="name"
          autoComplete="name"
          value={form.name}
          onChange={updateField}
          maxLength="100"
          required
        />

        <label htmlFor="register-email">Email</label>
        <input
          id="register-email"
          name="email"
          type="email"
          autoComplete="email"
          value={form.email}
          onChange={updateField}
          required
        />

        <label htmlFor="register-password">Password</label>
        <input
          id="register-password"
          name="password"
          type="password"
          autoComplete="new-password"
          value={form.password}
          onChange={updateField}
          minLength="8"
          maxLength="128"
          required
        />
        <span className="field-hint">Use at least 8 characters.</span>

        <button className="button button-primary button-full" disabled={loading}>
          {loading ? 'Creating account…' : 'Create account'}
        </button>
      </form>
    </AuthLayout>
  )
}
