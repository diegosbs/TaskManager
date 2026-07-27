import { useEffect, useState } from 'react'
import { Alert } from './Alert'

const emptyTask = {
  title: '',
  description: '',
  status: 'Pending',
  dueDate: '',
}

export function TaskForm({ task, saving, error, onSave, onCancel }) {
  const [form, setForm] = useState(emptyTask)

  useEffect(() => {
    setForm(
      task
        ? {
            title: task.title,
            description: task.description || '',
            status: task.status,
            dueDate: task.dueDate,
          }
        : emptyTask,
    )
  }, [task])

  function updateField(event) {
    setForm((current) => ({
      ...current,
      [event.target.name]: event.target.value,
    }))
  }

  function submit(event) {
    event.preventDefault()
    onSave(form)
  }

  return (
    <div className="form-overlay" role="presentation">
      <section
        className="task-form-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="task-form-title"
      >
        <div className="form-heading">
          <div>
            <p className="eyebrow">{task ? 'Update task' : 'New task'}</p>
            <h2 id="task-form-title">
              {task ? 'Edit your task' : 'What needs to be done?'}
            </h2>
          </div>
          <button
            className="icon-button"
            type="button"
            aria-label="Close task form"
            onClick={onCancel}
          >
            ×
          </button>
        </div>

        <Alert message={error} />

        <form onSubmit={submit}>
          <label htmlFor="task-title">Title</label>
          <input
            id="task-title"
            name="title"
            value={form.title}
            onChange={updateField}
            maxLength="100"
            required
            autoFocus
          />
          <span className="field-hint">{form.title.length}/100</span>

          <label htmlFor="task-description">Description</label>
          <textarea
            id="task-description"
            name="description"
            value={form.description}
            onChange={updateField}
            maxLength="500"
            rows="5"
          />
          <span className="field-hint">{form.description.length}/500</span>

          <div className="form-grid">
            <div>
              <label htmlFor="task-status">Status</label>
              <select
                id="task-status"
                name="status"
                value={form.status}
                onChange={updateField}
              >
                <option value="Pending">Pending</option>
                <option value="InProgress">In progress</option>
                <option value="Completed">Completed</option>
              </select>
            </div>
            <div>
              <label htmlFor="task-due-date">Due date</label>
              <input
                id="task-due-date"
                name="dueDate"
                type="date"
                value={form.dueDate}
                onChange={updateField}
                required
              />
            </div>
          </div>

          <div className="form-actions">
            <button
              className="button button-secondary"
              type="button"
              onClick={onCancel}
            >
              Cancel
            </button>
            <button className="button button-primary" type="submit" disabled={saving}>
              {saving ? 'Saving…' : task ? 'Save changes' : 'Create task'}
            </button>
          </div>
        </form>
      </section>
    </div>
  )
}
