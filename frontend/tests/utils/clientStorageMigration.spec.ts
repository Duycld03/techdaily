import { describe, it, expect, vi } from 'vitest'
import { CLIENT_STORAGE, isCurrentSessionToken, profileForToken, type StorageAdapter } from '~/utils/clientStorage'
import { LEGACY_SESSION, completeMigrationPhase, hasLegacySessionCookies, legacyBookRecord, migrateLegacyStorage, retireLegacyIdentity, transferStorageRecord, type MigrationEnvironment } from '~/utils/legacyStorageMigration'

function storage(): StorageAdapter {
  const data = new Map<string, string>()
  return { get length() { return data.size }, key: index => [...data.keys()][index] ?? null,
    getItem: key => data.get(key) ?? null, setItem: (key, value) => { data.set(key, value) }, removeItem: key => { data.delete(key) } }
}
function token(id = 'account-a', seconds = 3600) {
  return `e30.${btoa(JSON.stringify({ sub: id, email: `${id}@example.test`, exp: Math.floor(Date.now() / 1000) + seconds }))}.signature`
}
function environment(): MigrationEnvironment {
  const cookies = new Map<string, string>()
  return { local: storage(), session: storage(), cookies: { get: key => cookies.get(key) ?? null,
    set: (key, value) => { cookies.set(key, value) }, remove: key => { cookies.delete(key) } }, exclusive: async work => work() }
}
function legacySession(env: MigrationEnvironment, scope: 'local' | 'session' = 'local', id = 'account-a') {
  env[scope]!.setItem(LEGACY_SESSION.token, token(id))
  env[scope]!.setItem(LEGACY_SESSION.user, JSON.stringify({ id, email: `${id}@example.test`, name: id, preferredLocale: 'vi' }))
}

describe('Canonical storage registry and validators', () => {
  it('validates expiry/identity without trusting decoded claims as authorization', () => {
    expect(isCurrentSessionToken(token())).toBe(true)
    expect(isCurrentSessionToken(token('account-a', -10))).toBe(false)
    expect(isCurrentSessionToken('malformed')).toBe(false)
    expect(isCurrentSessionToken(`e30.${btoa('{"sub":"a"}')}.signature`)).toBe(false)
    expect(profileForToken(token(), { id: 'other' })?.id).toBe('account-a')
  })
  it('recognizes only supported per-book keys and positive slice orders', () => {
    const bookmark = legacyBookRecord('techdaily_bookmark_book-1')!
    expect(bookmark.destination).toBe('deeppace_bookmark_book-1')
    expect(bookmark.valid('3')).toBe(true)
    for (const invalid of ['0', '-1', 'NaN', '3abc', '1.5']) expect(bookmark.valid(invalid)).toBe(false)
    expect(legacyBookRecord('techdaily_completed_book-1')!.valid('[1,3]')).toBe(true)
    expect(legacyBookRecord('techdaily_completed_book-1')!.valid('["1"]')).toBe(false)
    expect(legacyBookRecord('unrelated_bookmark_book-1')).toBeNull()
  })
  it('transfers supported preferences including explicit false without touching unrelated state', async () => {
    const env = environment()
    env.local!.setItem('techdaily-auto-advance-cards', 'false')
    env.local!.setItem('techdaily_graph_legend_collapsed', 'false')
    env.local!.setItem('techdaily_graph_view_mode', '3d')
    env.local!.setItem('techdaily_roadmap_view_mode', 'mindmap')
    env.local!.setItem('techdaily_reader_typography', JSON.stringify({ fontSize: 'lg', fontFamily: 'serif', lineSpacing: 'loose', readingWidth: 'wide' }))
    env.local!.setItem('techdaily_bookmark_book-1', '7')
    env.local!.setItem('techdaily_completed_book-1', '[1,3]')
    env.local!.setItem('third_party', 'keep')
    expect(await migrateLegacyStorage(env)).toBe('complete')
    expect(env.local!.getItem(CLIENT_STORAGE.autoAdvanceCards)).toBe('false')
    expect(env.local!.getItem(CLIENT_STORAGE.legend)).toBe('false')
    expect(env.local!.getItem(CLIENT_STORAGE.graph)).toBe('3d')
    expect(env.local!.getItem(CLIENT_STORAGE.roadmap)).toBe('mindmap')
    expect(env.local!.getItem('deeppace_bookmark_book-1')).toBe('7')
    expect(env.local!.getItem('deeppace_completed_book-1')).toBe('[1,3]')
    expect(env.local!.getItem('third_party')).toBe('keep')
  })
  it('discards malformed known preferences without treating false as missing', async () => {
    const env = environment()
    for (const key of ['techdaily_roadmap_view_mode', 'techdaily_reader_typography', 'techdaily_completed_book-1']) env.local!.setItem(key, '{broken')
    await migrateLegacyStorage(env)
    expect(env.local!.getItem(CLIENT_STORAGE.roadmap)).toBeNull()
    expect(env.local!.getItem(CLIENT_STORAGE.typography)).toBeNull()
    expect(env.local!.getItem('deeppace_completed_book-1')).toBeNull()
  })
})

describe('Resumable storage transfer', () => {
  it('preserves canonical data even when it is invalid', async () => {
    const env = environment()
    env.local!.setItem('techdaily_graph_view_mode', '3d')
    env.local!.setItem(CLIENT_STORAGE.graph, 'invalid-current')
    await migrateLegacyStorage(env)
    expect(env.local!.getItem(CLIENT_STORAGE.graph)).toBe('invalid-current')
    expect(env.local!.getItem('techdaily_graph_view_mode')).toBeNull()
  })
  it('retains the source on destination failure and retries without overwriting', () => {
    const target = storage()
    target.setItem('old', 'false')
    const write = vi.spyOn(target, 'setItem').mockImplementationOnce(() => { throw new Error('quota') })
    expect(() => transferStorageRecord(target, 'old', 'new', () => true)).toThrow()
    expect(target.getItem('old')).toBe('false')
    write.mockRestore()
    transferStorageRecord(target, 'old', 'new', () => true)
    expect(target.getItem('new')).toBe('false')
    expect(target.getItem('old')).toBeNull()
  })
  it('resumes a failed source cleanup after a verified canonical copy', () => {
    const target = storage()
    target.setItem('old', 'saved')
    const cleanup = vi.spyOn(target, 'removeItem').mockImplementationOnce(() => { throw new Error('denied') })
    expect(() => transferStorageRecord(target, 'old', 'new', () => true)).toThrow()
    target.setItem('new', 'newer')
    cleanup.mockRestore()
    transferStorageRecord(target, 'old', 'new', () => true)
    expect(target.getItem('new')).toBe('newer')
    expect(target.getItem('old')).toBeNull()
  })
  it('does not read completed legacy phases on a second visit', async () => {
    const env = environment()
    await migrateLegacyStorage(env)
    const read = vi.spyOn(env.local!, 'getItem')
    await migrateLegacyStorage(env)
    expect(read.mock.calls.some(([key]) => key.startsWith('techdaily'))).toBe(false)
  })
  it('does not suppress an unmigrated tab session with a persistent checkpoint', async () => {
    const env = environment()
    completeMigrationPhase(env.local!, 'identity')
    legacySession(env, 'session')
    await migrateLegacyStorage(env)
    expect(env.session!.getItem(CLIENT_STORAGE.token)).toBe(token())
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
  })
  it('does not reopen completed persistent identity sources for a new tab', async () => {
    const env = environment()
    completeMigrationPhase(env.local!, 'identity')
    completeMigrationPhase(env.local!, 'preferences')
    const read = vi.spyOn(env.local!, 'getItem')
    await migrateLegacyStorage(env)
    expect(read.mock.calls.some(([key]) => key.startsWith('techdaily'))).toBe(false)
  })
  it('retains the entire valid identity source when a pair write fails', async () => {
    const env = environment()
    legacySession(env)
    const original = env.local!.setItem.bind(env.local)
    const write = vi.spyOn(env.local!, 'setItem').mockImplementation((key, value) => {
      if (key === CLIENT_STORAGE.user) throw new Error('quota')
      original(key, value)
    })
    expect(await migrateLegacyStorage(env)).toBe('deferred')
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(env.local!.getItem(LEGACY_SESSION.user)).not.toBeNull()
    write.mockRestore()
    await migrateLegacyStorage(env)
    expect(JSON.parse(env.local!.getItem(CLIENT_STORAGE.user)!).preferredLocale).toBe('vi')
  })
})

describe('Identity migration and fencing', () => {
  it.each(['local', 'session'] as const)('preserves the %s persistence scope and identity pair', async scope => {
    const env = environment()
    legacySession(env, scope)
    const cookies = vi.spyOn(env.cookies, 'set')
    await migrateLegacyStorage(env)
    expect(env[scope]!.getItem(CLIENT_STORAGE.token)).toBe(token())
    expect(JSON.parse(env[scope]!.getItem(CLIENT_STORAGE.user)!).preferredLocale).toBe('vi')
    expect(env[scope === 'local' ? 'session' : 'local']!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(cookies).toHaveBeenCalledWith(CLIENT_STORAGE.token, token(), scope === 'local')
    expect(env[scope]!.getItem(LEGACY_SESSION.token)).toBeNull()
  })
  it('keeps ambiguous cookie-only credentials session scoped', async () => {
    const env = environment()
    env.cookies.set(LEGACY_SESSION.token, token(), true)
    const writes = vi.spyOn(env.cookies, 'set')
    await migrateLegacyStorage(env)
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(env.session!.getItem(CLIENT_STORAGE.token)).toBe(token())
    expect(writes).toHaveBeenCalledWith(CLIENT_STORAGE.token, token(), false)
  })
  it('never imports an older account when canonical credentials exist', async () => {
    const env = environment()
    legacySession(env)
    env.session!.setItem(CLIENT_STORAGE.token, token('canonical'))
    await migrateLegacyStorage(env)
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(env.session!.getItem(CLIENT_STORAGE.token)).toBe(token('canonical'))
    expect(env.cookies.get(CLIENT_STORAGE.token)).toBeNull()
  })
  it('defers different legacy identities and never copies expired credentials', async () => {
    const env = environment()
    legacySession(env, 'local', 'account-a')
    legacySession(env, 'session', 'account-b')
    expect(await migrateLegacyStorage(env)).toBe('deferred')
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
    const expired = environment()
    expired.local!.setItem(LEGACY_SESSION.token, token('account-a', -60))
    await migrateLegacyStorage(expired)
    expect(expired.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
  })
  it('fences delayed migration when logout retires identity before lock acquisition', async () => {
    const env = environment()
    legacySession(env)
    let enter: (() => void) | undefined
    env.exclusive = work => new Promise(resolve => { enter = () => resolve(work()) })
    const pending = migrateLegacyStorage(env)
    retireLegacyIdentity(env)
    enter!()
    await pending
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(env.cookies.get(CLIENT_STORAGE.token)).toBeNull()
  })
  it('serializes updated tabs and safely defers without coordination/storage access', async () => {
    const env = environment()
    legacySession(env)
    let queue: Promise<unknown> = Promise.resolve()
    env.exclusive = work => { const result = queue.then(work); queue = result; return result }
    await Promise.all([migrateLegacyStorage(env), migrateLegacyStorage(env)])
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBe(token())
    const blocked = environment()
    legacySession(blocked)
    blocked.exclusive = undefined
    expect(await migrateLegacyStorage(blocked)).toBe('deferred')
    expect(blocked.local!.getItem(LEGACY_SESSION.token)).not.toBeNull()
    blocked.exclusive = async work => work()
    vi.spyOn(blocked.local!, 'getItem').mockImplementation(() => { throw new Error('denied') })
    expect(await migrateLegacyStorage(blocked)).toBe('deferred')
  })
  it('treats SSR legacy cookies only as a candidate signal', () => {
    expect(hasLegacySessionCookies(key => key === LEGACY_SESSION.token ? 'untrusted' : null)).toBe(true)
    expect(hasLegacySessionCookies(() => null)).toBe(false)
  })
})
