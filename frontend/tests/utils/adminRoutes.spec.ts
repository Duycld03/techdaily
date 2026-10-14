import { describe, it, expect } from 'vitest'
import { isAdminRoute, isAdminLogin, safeAdminReturnPath, loginRedirectFor } from '~/utils/adminRoutes'

describe('administrator route boundaries', () => {
  it.each(['/admin', '/admin/', '/admin/catalog', '/admin/missing'])('recognizes protected area %s', path => {
    expect(isAdminRoute(path)).toBe(true)
  })
  it.each(['/administrator', '/admin-other', '/today'])('does not broaden the administrator prefix to %s', path => {
    expect(isAdminRoute(path)).toBe(false)
  })
  it('recognizes only the normalized admin login', () => {
    expect(isAdminLogin('/admin/login/')).toBe(true)
    expect(isAdminLogin('/admin/login-extra')).toBe(false)
  })
  it.each([null, [], 'https://example.test/admin', '//example.test/admin', '/admin\\catalog', '/admin/../today', '/admin/%2e%2e/catalog', '/admin/%252f/catalog', '/admin/login', '/admin/login/', '/today', '/administrator', '/admin/login/extra'])('rejects unsafe return destination %s', path => {
    expect(safeAdminReturnPath(path)).toBe('/admin')
  })
  it('preserves safe catalog queries and uses the correct login area', () => {
    expect(safeAdminReturnPath('/admin/catalog/?page=2')).toBe('/admin/catalog?page=2')
    expect(loginRedirectFor('/admin/catalog?page=2')).toEqual({ path: '/admin/login', query: { redirect: '/admin/catalog?page=2' } })
    expect(loginRedirectFor('/library?page=2')).toEqual({ path: '/login', query: { redirect: '/library?page=2' } })
  })
})
