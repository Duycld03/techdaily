export function clearAuthStorage() {
  localStorage.clear()
  sessionStorage.clear()
  for (const name of ['deeppace_token', 'deeppace_user', 'techdaily_token', 'techdaily_user', 'refreshToken']) useCookie(name).value = null
}

export function adminTestJwt(id = 'admin-1', lifetime = 3600) {
  return `${btoa(JSON.stringify({ alg: 'HS256' }))}.${btoa(JSON.stringify({ nameid: id, exp: Math.floor(Date.now() / 1000) + lifetime }))}.test`
}

export function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no })
  return { promise, resolve, reject }
}
