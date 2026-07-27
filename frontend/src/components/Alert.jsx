export function Alert({ message }) {
  if (!message) {
    return null
  }

  return (
    <div className="alert" role="alert">
      {message}
    </div>
  )
}
