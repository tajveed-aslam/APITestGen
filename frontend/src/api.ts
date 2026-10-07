export type InputType = 'openApi' | 'sampleResponse'
export type TestCategory = 'positive' | 'negative'

export interface TestCase {
  id: string
  title: string
  category: TestCategory
  method: string
  path: string
  description: string
  headers: Record<string, string>
  queryParams: Record<string, string>
  body: unknown
  expectedStatus: number
  assertions: string[]
}

export interface GenerationSummary {
  id: string
  title: string
  inputType: InputType
  positiveCount: number
  negativeCount: number
  createdAt: string
}

export interface Generation {
  id: string
  title: string
  inputType: InputType
  input: string
  baseUrl: string | null
  testCases: TestCase[]
  postmanCollection: unknown
  pytestCode: string
  model: string
  durationMs: number
  createdAt: string
}

export interface CreateGenerationRequest {
  inputType: InputType
  input: string
  baseUrl?: string
  title?: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  email: string
  isGuest: boolean
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

let authToken: string | null = null
let onUnauthorized: (() => void) | null = null

export function configureAuth(token: string | null, unauthorizedHandler: (() => void) | null) {
  authToken = token
  onUnauthorized = unauthorizedHandler
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function request<T>(path: string, init: RequestInit & { json?: unknown } = {}): Promise<T> {
  const { json, ...rest } = init
  const headers = new Headers(rest.headers)
  if (authToken) headers.set('Authorization', `Bearer ${authToken}`)
  if (json !== undefined) headers.set('Content-Type', 'application/json')

  let response: Response
  try {
    response = await fetch(API_BASE + path, {
      ...rest,
      headers,
      body: json !== undefined ? JSON.stringify(json) : rest.body,
    })
  } catch {
    throw new ApiError(0, "Can't reach the server. Check your connection and try again.")
  }

  if (response.status === 401 && authToken) onUnauthorized?.()

  if (!response.ok) {
    throw new ApiError(response.status, await readError(response))
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails
    const fieldErrors = problem.errors ? Object.values(problem.errors).flat() : []
    if (fieldErrors.length > 0) return fieldErrors.join(' ')
    if (problem.detail) return problem.detail
    if (problem.title) return problem.title
  } catch {
    // Non-JSON error body; fall through.
  }
  return `Request failed (${response.status}).`
}

export const api = {
  health: () => request<{ status: string }>('/api/health'),

  register: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/register', { method: 'POST', json: { email, password } }),
  login: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/login', { method: 'POST', json: { email, password } }),
  guest: () => request<AuthResponse>('/api/auth/guest', { method: 'POST' }),

  listGenerations: () => request<GenerationSummary[]>('/api/generations'),
  getGeneration: (id: string) => request<Generation>(`/api/generations/${id}`),
  createGeneration: (body: CreateGenerationRequest) =>
    request<Generation>('/api/generations', { method: 'POST', json: body }),
  deleteGeneration: (id: string) => request<void>(`/api/generations/${id}`, { method: 'DELETE' }),
}
