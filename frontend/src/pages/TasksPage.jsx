import { useCallback, useEffect, useMemo, useState } from 'react'
import * as taskApi from '../api/tasks'
import { Alert } from '../components/Alert'
import { AppHeader } from '../components/AppHeader'
import { TaskCard } from '../components/TaskCard'
import { TaskForm } from '../components/TaskForm'
import { useAuth } from '../context/AuthContext'

const filters = ['All', 'Pending', 'InProgress', 'Completed']

export function TasksPage() {
  const { token, user, logout } = useAuth()
  const [tasks, setTasks] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [deletingId, setDeletingId] = useState(null)
  const [filter, setFilter] = useState('All')
  const [formOpen, setFormOpen] = useState(false)
  const [editingTask, setEditingTask] = useState(null)

  const handleRequestError = useCallback(
    (requestError, setMessage) => {
      if (requestError.status === 401) {
        logout()
        return
      }

      setMessage(requestError.message)
    },
    [logout],
  )

  const loadTasks = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      setTasks(await taskApi.getTasks(token))
    } catch (requestError) {
      handleRequestError(requestError, setError)
    } finally {
      setLoading(false)
    }
  }, [token, handleRequestError])

  useEffect(() => {
    loadTasks()
  }, [loadTasks])

  const visibleTasks = useMemo(
    () =>
      filter === 'All' ? tasks : tasks.filter((task) => task.status === filter),
    [filter, tasks],
  )

  function openCreateForm() {
    setEditingTask(null)
    setFormError('')
    setFormOpen(true)
  }

  function openEditForm(task) {
    setEditingTask(task)
    setFormError('')
    setFormOpen(true)
  }

  async function saveTask(values) {
    setSaving(true)
    setFormError('')

    try {
      if (editingTask) {
        await taskApi.updateTask(token, editingTask.id, values)
      } else {
        await taskApi.createTask(token, values)
      }

      setFormOpen(false)
      setEditingTask(null)
      await loadTasks()
    } catch (requestError) {
      handleRequestError(requestError, setFormError)
    } finally {
      setSaving(false)
    }
  }

  async function deleteTask(task) {
    if (!window.confirm(`Delete “${task.title}”? This cannot be undone.`)) {
      return
    }

    setDeletingId(task.id)
    setError('')

    try {
      await taskApi.deleteTask(token, task.id)
      setTasks((current) => current.filter((item) => item.id !== task.id))
    } catch (requestError) {
      handleRequestError(requestError, setError)
    } finally {
      setDeletingId(null)
    }
  }

  const completedCount = tasks.filter((task) => task.status === 'Completed').length

  return (
    <div className="app-shell">
      <AppHeader user={user} onLogout={logout} />

      <main className="page">
        <section className="page-heading">
          <div>
            <p className="eyebrow">Your workspace</p>
            <h1>Good work starts with a clear list.</h1>
            <p>
              {tasks.length
                ? `${completedCount} of ${tasks.length} tasks completed.`
                : 'Create your first task to get moving.'}
            </p>
          </div>
          <button className="button button-primary" type="button" onClick={openCreateForm}>
            <span aria-hidden="true">＋</span> New task
          </button>
        </section>

        <Alert message={error} />

        <section className="task-toolbar" aria-label="Task filters">
          {filters.map((item) => (
            <button
              className={filter === item ? 'filter active' : 'filter'}
              type="button"
              key={item}
              aria-pressed={filter === item}
              onClick={() => setFilter(item)}
            >
              {item === 'InProgress' ? 'In progress' : item}
            </button>
          ))}
        </section>

        {loading ? (
          <div className="state-panel" role="status">
            <span className="spinner" />
            Loading your tasks…
          </div>
        ) : visibleTasks.length ? (
          <section className="task-grid" aria-label="Tasks">
            {visibleTasks.map((task) => (
              <TaskCard
                key={task.id}
                task={task}
                onEdit={openEditForm}
                onDelete={deleteTask}
                deleting={deletingId === task.id}
              />
            ))}
          </section>
        ) : (
          <section className="state-panel">
            <div className="empty-icon" aria-hidden="true">
              ✓
            </div>
            <h2>No tasks here</h2>
            <p>
              {filter === 'All'
                ? 'Create a task and turn your plans into progress.'
                : 'There are no tasks with this status.'}
            </p>
            {filter === 'All' && (
              <button className="button button-secondary" onClick={openCreateForm}>
                Create your first task
              </button>
            )}
          </section>
        )}
      </main>

      {formOpen && (
        <TaskForm
          task={editingTask}
          saving={saving}
          error={formError}
          onSave={saveTask}
          onCancel={() => setFormOpen(false)}
        />
      )}
    </div>
  )
}
