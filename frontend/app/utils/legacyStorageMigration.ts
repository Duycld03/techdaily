import { CLIENT_STORAGE, browserStorage, isCurrentSessionToken, parseStoredJson, profileForToken, readStored, sessionSubject, type StorageAdapter } from './clientStorage'

/** The only Web Storage/cookie legacy boundary. Completed phases never read their sources again. */
export const LEGACY_SESSION = { token: 'techdaily_token', user: 'techdaily_user', refresh: 'techdaily_refresh_token' } as const
export const STORAGE_MIGRATION_LOCK = 'deeppace_storage_migration'
export type MigrationPhase = 'identity' | 'preferences' | 'audio'
export type MigrationResult = 'complete' | 'deferred'
type Progress = Partial<Record<MigrationPhase, true>>

export interface CookieAdapter {
  get(key: string): string | null
  set(key: string, value: string, persistent: boolean): void
  remove(key: string): void
}

export interface MigrationEnvironment {
  local: StorageAdapter | null
  session: StorageAdapter | null
  cookies: CookieAdapter
  exclusive?: <T>(work: () => T | Promise<T>) => Promise<T | undefined>
  diagnostic?: (phase: MigrationPhase, code: 'storage_unavailable' | 'coordination_unavailable') => void
}

const boolean = (value: string) => value === 'true' || value === 'false'
const enumValue = (...values: string[]) => (value: string) => values.includes(value)
const positiveOrder = (value: unknown) => typeof value === 'number' && Number.isSafeInteger(value) && value > 0
const typography = (raw: string) => {
  const value = parseStoredJson(raw) as Record<string, unknown> | null
  return Boolean(value && typeof value === 'object' && !Array.isArray(value)
    && ['sm', 'base', 'lg', 'xl', '2xl'].includes(String(value.fontSize))
    && ['sans', 'serif', 'mono'].includes(String(value.fontFamily))
    && ['normal', 'relaxed', 'loose'].includes(String(value.lineSpacing))
    && ['standard', 'wide', 'full'].includes(String(value.readingWidth)))
}

export const LEGACY_PREFERENCES = [
  ['techdaily_reader_typography', CLIENT_STORAGE.typography, typography],
  ['techdaily_roadmap_view_mode', CLIENT_STORAGE.roadmap, enumValue('timeline', 'mindmap')],
  ['techdaily_graph_view_mode', CLIENT_STORAGE.graph, enumValue('2d', '3d')],
  ['techdaily_graph_legend_collapsed', CLIENT_STORAGE.legend, boolean],
  ['techdaily-auto-advance-cards', CLIENT_STORAGE.autoAdvanceCards, boolean],
  ['techdaily_reader_audio_auto_advance', CLIENT_STORAGE.audioAutoAdvance, boolean],
] as const

export function legacyBookRecord(key: string): { destination: string; valid: (value: string) => boolean } | null {
  const match = /^techdaily_(bookmark|completed)_([a-zA-Z0-9-]{1,128})$/.exec(key)
  if (!match) return null
  return {
    destination: `deeppace_${match[1]}_${match[2]}`,
    valid: match[1] === 'bookmark'
      ? raw => /^\d+$/.test(raw) && positiveOrder(Number(raw))
      : raw => { const value = parseStoredJson(raw); return Array.isArray(value) && value.every(positiveOrder) },
  }
}

function progress(storage: StorageAdapter): Progress {
  const value = parseStoredJson(storage.getItem(CLIENT_STORAGE.migration)) as Progress | null
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {}
}

export function migrationPhaseComplete(storage: StorageAdapter | null, phase: MigrationPhase): boolean {
  try { return storage !== null && progress(storage)[phase] === true } catch { return false }
}

export function completeMigrationPhase(storage: StorageAdapter, phase: MigrationPhase): void {
  const next = { ...progress(storage), [phase]: true }
  storage.setItem(CLIENT_STORAGE.migration, JSON.stringify(next))
  if (!migrationPhaseComplete(storage, phase)) throw new Error('Migration checkpoint unavailable')
}

/** Source removal follows destination readback. A retry only cleans up an already canonical destination. */
export function transferStorageRecord(storage: StorageAdapter, source: string, destination: string, valid: (value: string) => boolean): void {
  const value = storage.getItem(source)
  if (value === null) return
  if (storage.getItem(destination) === null && value.length <= 65_536 && valid(value)) {
    storage.setItem(destination, value)
    if (storage.getItem(destination) !== value) throw new Error('Migration write unavailable')
  }
  storage.removeItem(source)
}

function migratePreferences(storage: StorageAdapter): void {
  if (migrationPhaseComplete(storage, 'preferences')) return
  for (const [source, destination, valid] of LEGACY_PREFERENCES) transferStorageRecord(storage, source, destination, valid)
  // Snapshot before removals so enumeration cannot skip shifted keys.
  const keys = Array.from({ length: storage.length }, (_, index) => storage.key(index)).filter((key): key is string => key !== null)
  for (const key of keys) {
    const record = legacyBookRecord(key)
    if (record) transferStorageRecord(storage, key, record.destination, record.valid)
  }
  completeMigrationPhase(storage, 'preferences')
}

export function hasLegacySessionCookies(get: (key: string) => unknown): boolean {
  return Boolean(get(LEGACY_SESSION.token) || get(LEGACY_SESSION.user))
}

function identityEpoch(env: MigrationEnvironment): string | null {
  return env.cookies.get(CLIENT_STORAGE.identityRetired) ?? readStored(env.local, CLIENT_STORAGE.identityRetired)
}

/** Retire before waiting for a lock. A cookie backs up the tombstone when Web Storage is denied. */
export function retireLegacyIdentity(env: MigrationEnvironment): string {
  const epoch = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`
  try { env.cookies.set(CLIENT_STORAGE.identityRetired, epoch, true) } catch { /* Storage can be denied independently. */ }
  try { env.local?.setItem(CLIENT_STORAGE.identityRetired, epoch) } catch { /* The cookie remains a second fence. */ }
  return epoch
}

export function clearLegacyCredentials(env: MigrationEnvironment): void {
  for (const storage of [env.local, env.session]) {
    for (const key of Object.values(LEGACY_SESSION)) storage?.removeItem(key)
    storage?.removeItem('deeppace_refresh_token')
  }
  env.cookies.remove(LEGACY_SESSION.token)
  env.cookies.remove(LEGACY_SESSION.user)
}

function migrateIdentity(env: MigrationEnvironment, initialEpoch: string | null): void {
  if (!env.local || !env.session) throw new Error('Migration storage unavailable')
  const persistentDone = migrationPhaseComplete(env.local, 'identity')
  const sessionDone = migrationPhaseComplete(env.session, 'identity')
  if (persistentDone && sessionDone) return
  const canonical = [env.cookies.get(CLIENT_STORAGE.token), env.cookies.get(CLIENT_STORAGE.user),
    env.local.getItem(CLIENT_STORAGE.token), env.local.getItem(CLIENT_STORAGE.user),
    env.session.getItem(CLIENT_STORAGE.token), env.session.getItem(CLIENT_STORAGE.user)].some(value => value !== null)
  if (!canonical && !identityEpoch(env) && initialEpoch === identityEpoch(env)) {
    const candidates = [
      { token: persistentDone ? null : env.local.getItem(LEGACY_SESSION.token), user: persistentDone ? null : env.local.getItem(LEGACY_SESSION.user), persistent: true },
      { token: sessionDone ? null : env.session.getItem(LEGACY_SESSION.token), user: sessionDone ? null : env.session.getItem(LEGACY_SESSION.user), persistent: false },
      { token: persistentDone ? null : env.cookies.get(LEGACY_SESSION.token), user: persistentDone ? null : env.cookies.get(LEGACY_SESSION.user), persistent: false },
    ].filter(candidate => isCurrentSessionToken(candidate.token))
    if (new Set(candidates.map(candidate => sessionSubject(candidate.token))).size > 1) throw new Error('Ambiguous migration identity')
    const selected = candidates[0]
    if (selected && initialEpoch === identityEpoch(env)) {
      const profile = profileForToken(selected.token, parseStoredJson(selected.user))
      if (!profile) throw new Error('Migration profile unavailable')
      const target = selected.persistent ? env.local : env.session
      const user = JSON.stringify(profile)
      // The lock and epoch fence cover this synchronous identity commit as a pair.
      try {
        target.setItem(CLIENT_STORAGE.token, selected.token!)
        target.setItem(CLIENT_STORAGE.user, user)
        if (target.getItem(CLIENT_STORAGE.token) !== selected.token || target.getItem(CLIENT_STORAGE.user) !== user) throw new Error('Migration write unavailable')
        if (initialEpoch !== identityEpoch(env)) throw new Error('Migration identity retired')
        env.cookies.set(CLIENT_STORAGE.token, selected.token!, selected.persistent)
        env.cookies.set(CLIENT_STORAGE.user, user, selected.persistent)
        if (env.cookies.get(CLIENT_STORAGE.token) !== selected.token || env.cookies.get(CLIENT_STORAGE.user) !== user) throw new Error('Migration cookie unavailable')
      } catch (error) {
        // Roll back only this uncommitted pair; valid sources remain available for a later retry.
        if (target.getItem(CLIENT_STORAGE.token) === selected.token) {
          target.removeItem(CLIENT_STORAGE.token)
          if (target.getItem(CLIENT_STORAGE.user) === user) target.removeItem(CLIENT_STORAGE.user)
        }
        if (env.cookies.get(CLIENT_STORAGE.token) === selected.token) {
          env.cookies.remove(CLIENT_STORAGE.token)
          if (env.cookies.get(CLIENT_STORAGE.user) === user) env.cookies.remove(CLIENT_STORAGE.user)
        }
        throw error
      }
    }
  }
  clearLegacyCredentials(env)
  completeMigrationPhase(env.local, 'identity')
  completeMigrationPhase(env.session, 'identity')
}

export async function migrateLegacyStorage(env: MigrationEnvironment): Promise<MigrationResult> {
  if (!env.exclusive) { env.diagnostic?.('identity', 'coordination_unavailable'); return 'deferred' }
  const epoch = identityEpoch(env)
  try {
    const result = await env.exclusive(() => {
      let completed = true
      try { migrateIdentity(env, epoch) } catch { completed = false; env.diagnostic?.('identity', 'storage_unavailable') }
      try {
        if (!env.local) throw new Error('Migration storage unavailable')
        migratePreferences(env.local)
      } catch { completed = false; env.diagnostic?.('preferences', 'storage_unavailable') }
      return completed ? 'complete' as const : 'deferred' as const
    })
    return result ?? 'deferred'
  } catch { return 'deferred' }
}

export function browserMigrationEnvironment(): MigrationEnvironment {
  return {
    local: browserStorage('localStorage'), session: browserStorage('sessionStorage'),
    cookies: {
      get(key) {
        const value = document.cookie.split('; ').find(entry => entry.startsWith(`${key}=`))?.slice(key.length + 1)
        try { return value === undefined ? null : decodeURIComponent(value) } catch { return null }
      },
      set(key, value, persistent) { document.cookie = `${key}=${encodeURIComponent(value)}; Path=/; SameSite=Lax${persistent ? '; Max-Age=2592000' : ''}${location.protocol === 'https:' ? '; Secure' : ''}` },
      remove(key) { document.cookie = `${key}=; Max-Age=0; Path=/; SameSite=Lax` },
    },
    exclusive: typeof navigator !== 'undefined' && navigator.locks?.request
      ? <T>(work: () => T | Promise<T>) => (navigator.locks.request(STORAGE_MIGRATION_LOCK, { ifAvailable: true }, lock => (lock ? work() : undefined)) as Promise<T | undefined>)
      : undefined,
  }
}

/** Normal auth writes still work without Web Locks; legacy transfer safely defers there. */
export async function commitSessionChange(env: MigrationEnvironment, epoch: string, commit: () => void): Promise<void> {
  const guarded = () => { if (identityEpoch(env) === epoch) commit() }
  if (typeof navigator !== 'undefined' && navigator.locks?.request) {
    await navigator.locks.request(STORAGE_MIGRATION_LOCK, guarded)
  } else guarded()
}
