import { createContext, useContext, useMemo, useState } from 'react'
import * as authApi from '../api/auth'

const TOKEN_KEY = 'task-manager-token'
const USER_KEY = 'task-manager-user'
const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState(() => {
    const storedUser = localStorage.getItem(USER_KEY)

    try {
      return storedUser ? JSON.parse(storedUser) : null
    } catch {
      localStorage.removeItem(USER_KEY)
      return null
    }
  })

  function saveSession(session) {
    localStorage.setItem(TOKEN_KEY, session.token)
    localStorage.setItem(USER_KEY, JSON.stringify(session.user))
    setToken(session.token)
    setUser(session.user)
  }

  async function login(credentials) {
    const session = await authApi.login(credentials)
    saveSession(session)
  }

  async function register(details) {
    await authApi.register(details)
    await login({ email: details.email, password: details.password })
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    setToken(null)
    setUser(null)
  }

  const value = useMemo(
    () => ({ token, user, login, register, logout }),
    [token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.')
  }

  return context
}
