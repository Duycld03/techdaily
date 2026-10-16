/**
 * Trims trailing slashes from a path while preserving root '/'.
 */
export function normalizeRoutePath(path: string): string {
  return path.replace(/\/+$/, '') || '/'
}

/**
 * Returns true if the path is within the administration hierarchy (/admin or /admin/*).
 */
export function isAdminRoute(path: string): boolean {
  const normalized = normalizeRoutePath(path)
  return normalized === '/admin' || normalized.startsWith('/admin/')
}

/**
 * Returns true if the path is the administrator login screen.
 */
export function isAdminLogin(path: string): boolean {
  return normalizeRoutePath(path) === '/admin/login'
}

/**
 * Sanitizes an untrusted return-url parameter to ensure it safely resolves within /admin.
 */
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

/**
 * Constructs an appropriate login redirection object preserving the destination.
 */
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

/**
 * Resolves the primary landing route for an administrator based on granted capabilities.
 */
export function getFirstAccessibleAdminRoute(capabilities?: AdminCapabilityFlags | null): string {
  if (capabilities?.canViewAdminDashboard) return '/admin/dashboard'
  if (capabilities?.canManageCatalog) return '/admin/catalog'
  if (capabilities?.canManageCategories) return '/admin/categories'
  if (capabilities?.canManageAccounts) return '/admin/users'
  if (capabilities?.canManageDocumentOperations) return '/admin/documents'
  if (capabilities?.canReadAudit) return '/admin/audit'
  return '/login'
}

/**
 * Checks whether the current capability set grants access to a specific admin route.
 */
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
