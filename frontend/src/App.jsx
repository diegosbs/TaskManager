import { useState } from 'react'
import { useAuth } from './context/AuthContext'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { TasksPage } from './pages/TasksPage'

export default function App() {
  const { token } = useAuth()
  const [authPage, setAuthPage] = useState('login')

  if (token) {
    return <TasksPage />
  }

  return authPage === 'login' ? (
    <LoginPage onShowRegister={() => setAuthPage('register')} />
  ) : (
    <RegisterPage onShowLogin={() => setAuthPage('login')} />
  )
}
