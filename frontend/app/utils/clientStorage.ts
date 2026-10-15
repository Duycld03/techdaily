/** Canonical application-owned storage. Standard library and refresh-cookie keys are unchanged. */
export const CLIENT_STORAGE = {
  token: 'deeppace_token',
  user: 'deeppace_user',
  typography: 'deeppace_reader_typography',
  roadmap: 'deeppace_roadmap_view_mode',
  graph: 'deeppace_graph_view_mode',
  legend: 'deeppace_graph_legend_collapsed',
  autoAdvanceCards: 'deeppace_auto_advance_cards',
  audioAutoAdvance: 'deeppace_reader_audio_auto_advance',
  migration: 'deeppace_storage_migration_v1',
  identityRetired: 'deeppace_identity_retired_v1',
  audioDatabase: 'deeppace-audio',
} as const

export interface StorageAdapter {
  getItem(key: string): string | null
  setItem(key: string, value: string): void
  removeItem(key: string): void
  readonly length: number
  key(index: number): string | null
}

export interface RoleDescriptor {
  id: string
  key: string
  nameEn: string
  nameVi: string
  isProtected: boolean
}

export interface SessionProfile {
  id: string
  email: string
  name: string
  preferredLocale: string
  avatarUrl?: string
  role?: RoleDescriptor
}

export function decodeSessionToken(token: string | null): Record<string, unknown> | null {
  if (!token) return null
  try {
    const parts = token.split('.')
    if (parts.length !== 3 || !parts[1]) return null
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0))
    const payload: unknown = JSON.parse(new TextDecoder().decode(bytes))
    return payload && typeof payload === 'object' && !Array.isArray(payload) ? payload as Record<string, unknown> : null
  } catch { return null }
}

export function sessionSubject(token: string | null): string | null {
  const claims = decodeSessionToken(token)
  const subject = claims?.nameid ?? claims?.sub ?? claims?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
  return typeof subject === 'string' && subject.length > 0 ? subject : null
}

export function isCurrentSessionToken(token: string | null, now = Date.now()): boolean {
  const claims = decodeSessionToken(token)
  return Boolean(claims && typeof claims.exp === 'number' && Number.isFinite(claims.exp) && claims.exp * 1000 > now && sessionSubject(token))
}

export function profileForToken(token: string | null, candidate: unknown): SessionProfile | null {
  const id = sessionSubject(token)
  if (!id) return null
  if (candidate && typeof candidate === 'object' && (candidate as SessionProfile).id === id) return candidate as SessionProfile
  const claims = decodeSessionToken(token)!
  const email = typeof claims.email === 'string' ? claims.email : ''
  return { id, email, name: String(claims.unique_name ?? claims.name ?? email.split('@')[0] ?? 'User'), preferredLocale: 'en' }
}

export function parseStoredJson(value: string | null): unknown {
  try { return value === null ? null : JSON.parse(value) } catch { return null }
}

export function browserStorage(scope: 'localStorage' | 'sessionStorage'): StorageAdapter | null {
  try { return typeof window === 'undefined' ? null : window[scope] } catch { return null }
}

export function readStored(storage: StorageAdapter | null, key: string): string | null {
  try { return storage?.getItem(key) ?? null } catch { return null }
}

export function readCanonicalSession(cookieToken: string | null, cookieUser: unknown) {
  const local = browserStorage('localStorage')
  const session = browserStorage('sessionStorage')
  const localToken = readStored(local, CLIENT_STORAGE.token)
  const sessionToken = readStored(session, CLIENT_STORAGE.token)
  const token = cookieToken ?? localToken ?? sessionToken
  const profile = cookieToken !== null ? cookieUser : parseStoredJson(readStored(localToken !== null ? local : session, CLIENT_STORAGE.user))
  return { token, user: profileForToken(token, profile), persistent: token !== null && localToken === token }
}
