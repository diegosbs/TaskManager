export function AuthLayout({ eyebrow, title, description, children, footer }) {
  return (
    <main className="auth-shell">
      <section className="auth-intro" aria-labelledby="auth-heading">
        <div className="brand-mark" aria-hidden="true">
          TM
        </div>
        <p className="eyebrow">{eyebrow}</p>
        <h1 id="auth-heading">{title}</h1>
        <p className="auth-description">{description}</p>
        <div className="intro-note">
          <span className="intro-note-dot" />
          Clean Architecture · Secure JWT · SQLite
        </div>
      </section>

      <section className="auth-card">
        {children}
        <div className="auth-footer">{footer}</div>
      </section>
    </main>
  )
}
