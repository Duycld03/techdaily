export function normalizeRoutePath(path: string): string {
  return path.replace(/\/+$/, '') || '/'
}

export function isAdminRoute(path: string): boolean {
  const normalized = normalizeRoutePath(path)
  return normalized === '/admin' || normalized.startsWith('/admin/')
}

export function isAdminLogin(path: string): boolean {
  return normalizeRoutePath(path) === '/admin/login'
}

export function safeAdminReturnPath(value: unknown): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || /[\\\u0000-\u0020]/.test(value)) return '/admin'
  const path = value.split(/[?#]/)[0] || ''
  if (path.includes('%') || path.split('/').some(segment => segment === '.' || segment === '..')) return '/admin'
  try {
    const url = new URL(value, 'https://deeppace.invalid')
    if (url.origin !== 'https://deeppace.invalid' || !isAdminRoute(url.pathname)
      || isAdminLogin(url.pathname) || url.pathname.startsWith('/admin/login/')) return '/admin'
    return normalizeRoutePath(url.pathname) + url.search + url.hash
  } catch {
    return '/admin'
  }
}

export function loginRedirectFor(path: string) {
  return isAdminRoute(path.split(/[?#]/)[0] || '/')
    ? { path: '/admin/login', query: { redirect: safeAdminReturnPath(path) } }
    : { path: '/login', query: { redirect: path } }
}

export interface AdminCapabilityFlags {
  canViewAdminDashboard?: boolean
  canManageCatalog?: boolean
  canManageAccounts?: boolean
  canReadAudit?: boolean
  canManageDocumentOperations?: boolean
  canManageRoles?: boolean
  canManageCategories?: boolean
}

export function getFirstAccessibleAdminRoute(capabilities?: AdminCapabilityFlags | null): string {
  if (capabilities?.canViewAdminDashboard) return '/admin/dashboard'
  if (capabilities?.canManageCatalog) return '/admin/catalog'
  if (capabilities?.canManageCategories) return '/admin/categories'
  if (capabilities?.canManageAccounts) return '/admin/users'
  if (capabilities?.canManageDocumentOperations) return '/admin/documents'
  if (capabilities?.canReadAudit) return '/admin/audit'
  return '/login'
}

export function canAccessAdminDestination(path: string, capabilities?: AdminCapabilityFlags | null): boolean {
  if (!capabilities) return false
  const norm = normalizeRoutePath(path)
  if (norm === '/admin' || norm === '/admin/dashboard' || norm.startsWith('/admin/dashboard/')) {
    return Boolean(capabilities.canViewAdminDashboard)
  }
  if (norm === '/admin/catalog' || norm.startsWith('/admin/catalog/')) {
    return Boolean(capabilities.canManageCatalog)
  }
  if (norm === '/admin/categories' || norm.startsWith('/admin/categories/')) {
    return Boolean(capabilities.canManageCategories)
  }
  if (norm === '/admin/users' || norm.startsWith('/admin/users/')) {
    return Boolean(capabilities.canManageAccounts)
  }
  if (norm === '/admin/audit' || norm.startsWith('/admin/audit/')) {
    return Boolean(capabilities.canReadAudit)
  }
  if (norm === '/admin/documents' || norm.startsWith('/admin/documents/')) {
    return Boolean(capabilities.canManageDocumentOperations)
  }
  return false
}
