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
