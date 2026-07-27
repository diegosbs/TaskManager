const statusLabels = {
  Pending: 'Pending',
  InProgress: 'In progress',
  Completed: 'Completed',
}

export function TaskCard({ task, onEdit, onDelete, deleting }) {
  const dueDate = new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(`${task.dueDate}T00:00:00`))

  return (
    <article className="task-card">
      <div className="task-card-top">
        <span className={`status status-${task.status.toLowerCase()}`}>
          {statusLabels[task.status]}
        </span>
        <span className="due-date">Due {dueDate}</span>
      </div>

      <h3>{task.title}</h3>
      <p>{task.description || 'No description provided.'}</p>

      <div className="task-actions">
        <button
          className="button button-secondary"
          type="button"
          onClick={() => onEdit(task)}
        >
          Edit
        </button>
        <button
          className="button button-danger"
          type="button"
          disabled={deleting}
          onClick={() => onDelete(task)}
        >
          {deleting ? 'Deleting…' : 'Delete'}
        </button>
      </div>
    </article>
  )
}
