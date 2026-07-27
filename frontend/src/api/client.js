const API_URL = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '')

export class ApiError extends Error {
  constructor(message, status, errors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

export async function apiRequest(path, { token, body, ...options } = {}) {
  const headers = new Headers(options.headers)

  if (body !== undefined) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    const problem = await readJson(response)
    const validationMessage = Object.values(problem?.errors || {})
      .flat()
      .join(' ')

    throw new ApiError(
      validationMessage ||
        problem?.detail ||
        problem?.title ||
        'The request could not be completed.',
      response.status,
      problem?.errors,
    )
  }

  if (response.status === 204) {
    return null
  }

  return readJson(response)
}

async function readJson(response) {
  const text = await response.text()
  if (!text) {
    return null
  }

  try {
    return JSON.parse(text)
  } catch {
    return { detail: text }
  }
}
