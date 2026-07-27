import { apiRequest } from './client'

export function getTasks(token) {
  return apiRequest('/api/tasks', { token })
}

export function createTask(token, task) {
  return apiRequest('/api/tasks', {
    method: 'POST',
    token,
    body: task,
  })
}

export function updateTask(token, id, task) {
  return apiRequest(`/api/tasks/${id}`, {
    method: 'PUT',
    token,
    body: task,
  })
}

export function deleteTask(token, id) {
  return apiRequest(`/api/tasks/${id}`, {
    method: 'DELETE',
    token,
  })
}
