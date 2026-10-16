import { describe, it, expect } from 'vitest'
import { isAdminRoute, isAdminLogin, safeAdminReturnPath, loginRedirectFor, getFirstAccessibleAdminRoute, canAccessAdminDestination } from '~/utils/adminRoutes'

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

  describe('deterministic landing and destination access', () => {
    it('resolves deterministic landing in order: dashboard, catalog, categories, users, documents, audit, fallback to login', () => {
      expect(getFirstAccessibleAdminRoute({ canViewAdminDashboard: true, canManageCatalog: true })).toBe('/admin/dashboard')
      expect(getFirstAccessibleAdminRoute({ canManageCatalog: true, canManageAccounts: true })).toBe('/admin/catalog')
      expect(getFirstAccessibleAdminRoute({ canManageCategories: true, canManageAccounts: true })).toBe('/admin/categories')
      expect(getFirstAccessibleAdminRoute({ canManageAccounts: true, canReadAudit: true })).toBe('/admin/users')
      expect(getFirstAccessibleAdminRoute({ canManageDocumentOperations: true, canReadAudit: true })).toBe('/admin/documents')
      expect(getFirstAccessibleAdminRoute({ canReadAudit: true })).toBe('/admin/audit')
      expect(getFirstAccessibleAdminRoute({ canManageRoles: true })).toBe('/login')
      expect(getFirstAccessibleAdminRoute(null)).toBe('/login')
      expect(getFirstAccessibleAdminRoute({})).toBe('/login')
    })

    it('verifies destination access per registered capability and denies unknown destinations', () => {
      const allCaps = {
        canViewAdminDashboard: true,
        canManageCatalog: true,
        canManageCategories: true,
        canManageAccounts: true,
        canReadAudit: true,
        canManageDocumentOperations: true,
        canManageRoles: true
      }

      expect(canAccessAdminDestination('/admin', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/dashboard', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/catalog', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/categories', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/users', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/documents', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/audit', allCaps)).toBe(true)
      expect(canAccessAdminDestination('/admin/unknown', allCaps)).toBe(false)
      expect(canAccessAdminDestination('/admin/missing', allCaps)).toBe(false)
    })

    it('denies all admin destinations when account only has canManageRoles', () => {
      const rolesOnly = { canManageRoles: true }
      expect(canAccessAdminDestination('/admin', rolesOnly)).toBe(false)
      expect(canAccessAdminDestination('/admin/dashboard', rolesOnly)).toBe(false)
      expect(canAccessAdminDestination('/admin/catalog', rolesOnly)).toBe(false)
      expect(canAccessAdminDestination('/admin/categories', rolesOnly)).toBe(false)
      expect(canAccessAdminDestination('/admin/users', rolesOnly)).toBe(false)
      expect(canAccessAdminDestination('/admin/documents', rolesOnly)).toBe(false)
      expect(canAccessAdminDestination('/admin/audit', rolesOnly)).toBe(false)
    })
  })
})
