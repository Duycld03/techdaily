import { describe, it, expect, vi } from 'vitest'
import { CLIENT_STORAGE, isCurrentSessionToken, parseAudioPreference, profileForToken, readAudioPreference, writeAudioPreference, type StorageAdapter } from '~/utils/clientStorage'
import { LEGACY_SESSION, completeMigrationPhase, hasLegacySessionCookies, legacyBookRecord, migrateLegacyStorage, migrationPhaseComplete, retireLegacyIdentity, transferStorageRecord, type MigrationEnvironment } from '~/utils/legacyStorageMigration'

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
  const currentToken = token(id)
  env[scope]!.setItem(LEGACY_SESSION.token, currentToken)
  env[scope]!.setItem(LEGACY_SESSION.user, JSON.stringify({ id, email: `${id}@example.test`, name: id, preferredLocale: 'vi' }))
  return currentToken
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
    const activeToken = legacySession(env, scope)
    const cookies = vi.spyOn(env.cookies, 'set')
    await migrateLegacyStorage(env)
    expect(env[scope]!.getItem(CLIENT_STORAGE.token)).toBe(activeToken)
    expect(JSON.parse(env[scope]!.getItem(CLIENT_STORAGE.user)!).preferredLocale).toBe('vi')
    expect(env[scope === 'local' ? 'session' : 'local']!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(cookies).toHaveBeenCalledWith(CLIENT_STORAGE.token, activeToken, scope === 'local')
    expect(env[scope]!.getItem(LEGACY_SESSION.token)).toBeNull()
  })
  it('keeps ambiguous cookie-only credentials session scoped', async () => {
    const env = environment()
    const activeToken = token()
    env.cookies.set(LEGACY_SESSION.token, activeToken, true)
    const writes = vi.spyOn(env.cookies, 'set')
    await migrateLegacyStorage(env)
    expect(env.local!.getItem(CLIENT_STORAGE.token)).toBeNull()
    expect(env.session!.getItem(CLIENT_STORAGE.token)).toBe(activeToken)
    expect(writes).toHaveBeenCalledWith(CLIENT_STORAGE.token, activeToken, false)
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

describe('Explicit narration preference transition', () => {
  const oldEngine = 'deeppace_reader_audio_engine'
  const preference = (mode: string) => JSON.stringify({ version: 1, mode })

  it.each(['auto', 'system', 'cloud', 'device'] as const)('accepts and verifies the explicit %s preference', mode => {
    const target = storage()
    expect(writeAudioPreference(mode, target)).toBe(true)
    expect(target.getItem(CLIENT_STORAGE.audioPreference)).toBe(preference(mode))
    expect(parseAudioPreference(target.getItem(CLIENT_STORAGE.audioPreference))).toBe(mode)
    expect(readAudioPreference(target)).toBe(mode)
  })

  it.each([null, '', 'cloud', '{}', '{"version":2,"mode":"cloud"}', '{"version":1,"mode":"local"}',
    '{"version":1,"mode":"Auto"}', '{"version":1,"mode":"auto","unexpected":true}', '[]', 'null'])('rejects unsupported preference records: %s', raw => {
    expect(parseAudioPreference(raw)).toBeNull()
  })

  it('does not claim a preference write succeeded without matching readback', () => {
    const target = storage()
    vi.spyOn(target, 'setItem').mockImplementation(() => {})
    expect(writeAudioPreference('cloud', target)).toBe(false)
    expect(readAudioPreference(target)).toBe('auto')
    expect(writeAudioPreference('system', null)).toBe(false)
  })

  it('preserves an explicit canonical choice while retiring the ambiguous old value', async () => {
    const env = environment()
    env.local!.setItem(oldEngine, 'device')
    env.local!.setItem(CLIENT_STORAGE.audioPreference, preference('system'))
    expect(await migrateLegacyStorage(env)).toBe('complete')
    expect(readAudioPreference(env.local)).toBe('system')
    expect(env.local!.getItem(oldEngine)).toBeNull()
    expect(migrationPhaseComplete(env.local, 'audioPreference')).toBe(true)
  })

  it.each(['cloud', 'system', 'device', 'invalid'])('resets ambiguous %s to Auto after earlier phases completed without touching other data', async old => {
    const env = environment()
    for (const phase of ['identity', 'preferences', 'audio'] as const) completeMigrationPhase(env.local!, phase)
    completeMigrationPhase(env.session!, 'identity')
    const untouched = {
      deeppace_reader_audio_speed: '1.5',
      deeppace_reader_audio_voice_vi: 'vi-VN-Neural2-A',
      deeppace_reader_audio_system_voice: 'native-voice',
      deeppace_bookmark_book: '7',
      deeppace_completed_book: '[1,2]',
      third_party: 'keep',
    }
    for (const [key, value] of Object.entries(untouched)) env.local!.setItem(key, value)
    env.local!.setItem(oldEngine, old)
    expect(await migrateLegacyStorage(env)).toBe('complete')
    expect(env.local!.getItem(CLIENT_STORAGE.audioPreference)).toBe(preference('auto'))
    expect(env.local!.getItem(oldEngine)).toBeNull()
    for (const [key, value] of Object.entries(untouched)) expect(env.local!.getItem(key)).toBe(value)
    for (const phase of ['identity', 'preferences', 'audio', 'audioPreference'] as const) {
      expect(migrationPhaseComplete(env.local, phase)).toBe(true)
    }
  })

  it('replaces malformed canonical preference with verified Auto before retirement', async () => {
    const env = environment()
    env.local!.setItem(oldEngine, 'cloud')
    env.local!.setItem(CLIENT_STORAGE.audioPreference, '{"version":9,"mode":"cloud"}')
    await migrateLegacyStorage(env)
    expect(env.local!.getItem(CLIENT_STORAGE.audioPreference)).toBe(preference('auto'))
    expect(env.local!.getItem(oldEngine)).toBeNull()
  })

  it.each(['throw', 'ignored'] as const)('keeps retirement pending on %s destination writes and retries safely', async failure => {
    const env = environment()
    env.local!.setItem(oldEngine, 'cloud')
    const original = env.local!.setItem.bind(env.local)
    const write = vi.spyOn(env.local!, 'setItem').mockImplementation((key, value) => {
      if (key === CLIENT_STORAGE.audioPreference) {
        if (failure === 'throw') throw new Error('quota')
        return
      }
      original(key, value)
    })
    expect(await migrateLegacyStorage(env)).toBe('deferred')
    expect(readAudioPreference(env.local)).toBe('auto')
    expect(env.local!.getItem(oldEngine)).toBe('cloud')
    expect(migrationPhaseComplete(env.local, 'audioPreference')).toBe(false)
    expect(migrationPhaseComplete(env.local, 'identity')).toBe(true)
    write.mockRestore()
    expect(await migrateLegacyStorage(env)).toBe('complete')
    expect(env.local!.getItem(oldEngine)).toBeNull()
    expect(migrationPhaseComplete(env.local, 'audioPreference')).toBe(true)
  })

  it('retries interrupted retirement without replacing a newer explicit choice', async () => {
    const env = environment()
    env.local!.setItem(oldEngine, 'cloud')
    const original = env.local!.removeItem.bind(env.local)
    const remove = vi.spyOn(env.local!, 'removeItem').mockImplementation(key => {
      if (key === oldEngine) throw new Error('denied')
      original(key)
    })
    expect(await migrateLegacyStorage(env)).toBe('deferred')
    expect(env.local!.getItem(CLIENT_STORAGE.audioPreference)).toBe(preference('auto'))
    expect(migrationPhaseComplete(env.local, 'audioPreference')).toBe(false)
    expect(writeAudioPreference('device', env.local)).toBe(true)
    remove.mockRestore()
    expect(await migrateLegacyStorage(env)).toBe('complete')
    expect(readAudioPreference(env.local)).toBe('device')
    expect(env.local!.getItem(oldEngine)).toBeNull()
  })

  it('leaves completion pending when source removal is silently ignored', async () => {
    const env = environment()
    env.local!.setItem(oldEngine, 'cloud')
    const original = env.local!.removeItem.bind(env.local)
    vi.spyOn(env.local!, 'removeItem').mockImplementation(key => {
      if (key !== oldEngine) original(key)
    })
    expect(await migrateLegacyStorage(env)).toBe('deferred')
    expect(env.local!.getItem(oldEngine)).toBe('cloud')
    expect(migrationPhaseComplete(env.local, 'audioPreference')).toBe(false)
  })

  it.each(['unavailable', 'occupied'] as const)('uses canonical-or-Auto without consulting old engine when coordination is %s', async coordination => {
    const env = environment()
    env.local!.setItem(oldEngine, 'cloud')
    env.exclusive = coordination === 'unavailable' ? undefined : async () => undefined
    expect(await migrateLegacyStorage(env)).toBe('deferred')
    const read = vi.spyOn(env.local!, 'getItem')
    expect(readAudioPreference(env.local)).toBe('auto')
    env.local!.setItem(CLIENT_STORAGE.audioPreference, preference('device'))
    expect(readAudioPreference(env.local)).toBe('device')
    expect(read.mock.calls.some(([key]) => key === oldEngine)).toBe(false)
    expect(migrationPhaseComplete(env.local, 'audioPreference')).toBe(false)
    read.mockRestore()
    expect(env.local!.getItem(oldEngine)).toBe('cloud')
    env.exclusive = async work => work()
    expect(await migrateLegacyStorage(env)).toBe('complete')
    expect(readAudioPreference(env.local)).toBe('device')
  })

  it('guards denied preference reads and writes without consulting obsolete data', () => {
    const target = storage()
    vi.spyOn(target, 'getItem').mockImplementation(() => { throw new Error('denied') })
    vi.spyOn(target, 'setItem').mockImplementation(() => { throw new Error('denied') })
    expect(() => readAudioPreference(target)).not.toThrow()
    expect(readAudioPreference(target)).toBe('auto')
    expect(writeAudioPreference('system', target)).toBe(false)
  })

  it('does not revisit an independently completed preference transition', async () => {
    const env = environment()
    await migrateLegacyStorage(env)
    env.local!.setItem(oldEngine, 'reintroduced')
    const read = vi.spyOn(env.local!, 'getItem')
    await migrateLegacyStorage(env)
    expect(read.mock.calls.some(([key]) => key === oldEngine)).toBe(false)
  })
})
