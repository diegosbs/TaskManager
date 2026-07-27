export function AppHeader({ user, onLogout }) {
  return (
    <header className="app-header">
      <div className="header-inner">
        <a className="brand" href="/" aria-label="Task Manager home">
          <span className="brand-mark brand-mark-small" aria-hidden="true">
            TM
          </span>
          <span>Task Manager</span>
        </a>

        <div className="user-menu">
          <div className="user-copy">
            <strong>{user?.name}</strong>
            <span>{user?.email}</span>
          </div>
          <button className="button button-ghost" type="button" onClick={onLogout}>
            Log out
          </button>
        </div>
      </div>
    </header>
  )
}
