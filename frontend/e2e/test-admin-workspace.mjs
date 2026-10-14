import assert from 'node:assert/strict'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium, request } from 'playwright'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..')
const targetUrl = process.env.TARGET_URL || 'http://127.0.0.1:3001'
const apiUrl = process.env.API_URL || 'http://127.0.0.1:5001'
const output = path.join(root, 'frontend/e2e/screenshots/admin-workspace')
const envFile = path.join(root, '.env')
const env = fs.existsSync(envFile) ? Object.fromEntries(fs.readFileSync(envFile, 'utf8').split('\n')
  .filter(line => line.trim() && !line.startsWith('#') && line.includes('='))
  .map(line => {
    const split = line.indexOf('=')
    return [line.slice(0, split).trim(), line.slice(split + 1).trim().replace(/^(['"])(.*)\1$/, '$2')]
  })) : {}
const fixture = process.env.ADMIN_QA_ACCOUNTS_FILE ? JSON.parse(fs.readFileSync(process.env.ADMIN_QA_ACCOUNTS_FILE, 'utf8')) : {}
const admin = { email: process.env.ADMIN_QA_EMAIL || env.E2E_PROD_EMAIL, password: process.env.ADMIN_QA_PASSWORD || env.E2E_PROD_PASSWORD }
const learner = { email: process.env.LEARNER_QA_EMAIL || fixture.ordinaryEmail, password: process.env.LEARNER_QA_PASSWORD || fixture.ordinaryPassword }
assert(admin.email && admin.password && learner.email && learner.password, 'Real administrator and learner credentials are required; no fallback identities are allowed.')
const checks = []
const screenshots = []
const browserErrors = []
const dictionaries = Object.fromEntries(['en', 'vi'].map(locale => [locale, JSON.parse(fs.readFileSync(path.join(root, `frontend/app/i18n/locales/${locale}.json`), 'utf8')).admin]))
fs.mkdirSync(output, { recursive: true })
const browser = await chromium.launch({ headless: true })

function check(condition, label) { assert(condition, label); checks.push(label) }
async function signIn(account) {
  const client = await request.newContext({ baseURL: apiUrl })
  const response = await client.post('/api/v1/auth/login', { data: { ...account, rememberMe: true } })
  assert.equal(response.status(), 200, 'Real authentication must succeed.')
  const session = await response.json()
  const cookies = (await client.storageState()).cookies
  await client.dispose()
  return { ...session, cookies }
}
function state(session, locale = 'en', theme = 'light', refreshOnly = false) {
  const host = new URL(targetUrl).hostname
  const cookie = (name, value) => ({ name, value, domain: host, path: '/', expires: -1, httpOnly: false, secure: false, sameSite: 'Lax' })
  return {
    cookies: [
      ...(session?.cookies || []), cookie('i18n_redirected', locale),
      ...(!session || refreshOnly ? [] : [cookie('techdaily_token', session.token), cookie('techdaily_user', encodeURIComponent(JSON.stringify(session.user)))])
    ],
    origins: [{ origin: new URL(targetUrl).origin, localStorage: [
      { name: 'nuxt-color-mode', value: theme },
      ...(!session || refreshOnly ? [] : [{ name: 'techdaily_token', value: session.token }, { name: 'techdaily_user', value: JSON.stringify(session.user) }])
    ] }]
  }
}
async function contextFor(session, viewport = { width: 1440, height: 900 }, locale = 'en', theme = 'light', refreshOnly = false) {
  const context = await browser.newContext({ viewport, storageState: state(session, locale, theme, refreshOnly) })
  context.on('page', page => {
    page.on('pageerror', error => browserErrors.push(`${new URL(page.url()).pathname}: ${error.message}`))
    page.on('console', message => { if (/hydration.*mismatch/i.test(message.text())) browserErrors.push(`${new URL(page.url()).pathname}: hydration mismatch`) })
  })
  return context
}
async function capture(page, name) {
  await page.screenshot({ path: path.join(output, `${name}.png`), fullPage: true, animations: 'disabled' })
  screenshots.push(`${name}.png`)
  check(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${name}: no horizontal overflow`)
}
async function open(page, route, marker) {
  await page.goto(`${targetUrl}${route}`, { waitUntil: 'networkidle' })
  await page.locator(marker).waitFor({ state: 'visible' })
}
async function fillLogin(page, account, remember = true) {
  await page.locator('#admin-email').fill(account.email)
  await page.locator('#admin-password').fill(account.password)
  await page.locator('[name="rememberMe"]').setChecked(remember)
  await page.locator('form button[type="submit"]').click()
}

try {
  const adminSession = await signIn(admin)
  const learnerSession = await signIn(learner)
  for (const [label, session, expectedStatus] of [['anonymous', null, 401], ['learner', learnerSession, 403]]) {
    const context = await contextFor(session)
    for (const route of ['/admin', '/admin/catalog/', '/admin/unknown']) {
      const response = await context.request.get(`${targetUrl}${route}`, { maxRedirects: 0 })
      check(response.status() === 302 && response.headers().location.startsWith('/admin/login?redirect='), `${label}: ${route} redirects to admin login`)
      check(!/data-admin-(?:header|sidebar|dashboard)/.test(await response.text()), `${label}: SSR exposes no privileged markup`)
    }
    for (const endpoint of ['overview', 'status', 'ai-probe']) {
      const options = { headers: session ? { Authorization: `Bearer ${session.token}` } : {} }
      const response = endpoint === 'ai-probe' ? await context.request.post(`${apiUrl}/api/v1/admin/dashboard/${endpoint}`, options) : await context.request.get(`${apiUrl}/api/v1/admin/dashboard/${endpoint}`, options)
      check(response.status() === expectedStatus, `${label}: direct ${endpoint} API rejects independently`)
      check(response.headers()['cache-control']?.includes('no-store'), `${label}: rejected API is not cached`)
    }
    await context.close()
  }

  const pendingSession = await signIn(admin)
  const pending = await contextFor(pendingSession, undefined, 'en', 'light', true)
  const pendingHtml = await pending.request.get(`${targetUrl}/admin/catalog`)
  const html = await pendingHtml.text()
  check(pendingHtml.status() === 200 && html.includes('data-admin-boundary'), 'Refresh-only SSR renders a neutral authorization boundary')
  check(!/data-admin-(?:header|sidebar|dashboard)/.test(html) && !html.includes('Distributed Systems Field Notes'), 'Pending SSR contains no protected chrome or catalog content')
  const renewedPage = await pending.newPage()
  let renewals = 0
  renewedPage.on('request', r => { if (r.url().endsWith('/api/v1/auth/refresh')) renewals++ })
  await open(renewedPage, '/admin', '[data-admin-dashboard]')
  check(renewals === 1, 'A genuine refresh cookie renews the session once during hydration')
  check((await pending.cookies()).some(c => c.name === 'deeppace_token'), 'Successful refresh installs the access cookie')
  await pending.close()

  const loginContext = await contextFor(null)
  const loginPage = await loginContext.newPage()
  await open(loginPage, '/admin/login?redirect=%2Fadmin%2Fcatalog', '[data-admin-login]')
  let loginRequests = 0
  loginPage.on('request', r => { if (r.url().endsWith('/api/v1/auth/login')) loginRequests++ })
  await loginPage.locator('form button[type="submit"]').click()
  check(loginRequests === 0 && await loginPage.getByRole('alert').isVisible(), 'Invalid form input blocks authentication')
  await fillLogin(loginPage, { email: admin.email, password: 'invalid-qa-password' })
  await loginPage.getByRole('alert').filter({ hasText: /password|credentials|mật khẩu/i }).waitFor()
  check(new URL(loginPage.url()).pathname === '/admin/login', 'Invalid credentials remain on administrator sign-in')
  await fillLogin(loginPage, learner, false)
  await loginPage.getByRole('alert').filter({ hasText: dictionaries.en.login.denied }).waitFor()
  check(new URL(loginPage.url()).pathname === '/admin/login', 'A regular account can stay on administrator sign-in without redirect loops')
  check((await loginContext.cookies()).find(c => c.name === 'refreshToken')?.expires === -1, 'Remember-me off issues a session-scoped refresh cookie')
  await fillLogin(loginPage, admin, true)
  await loginPage.waitForURL(url => url.pathname === '/admin/catalog')
  await loginPage.waitForLoadState('networkidle')
  check((await loginContext.cookies()).find(c => c.name === 'refreshToken')?.expires > 0, 'Remember-me on issues a persistent refresh cookie')
  const replacementUser = await loginPage.evaluate(() => JSON.parse(localStorage.getItem('deeppace_user')).id)
  check(replacementUser === adminSession.user.id, 'Administrator sign-in replaces the regular account')
  await loginContext.close()

  for (const viewport of [{ name: 'desktop', width: 1440, height: 900 }, { name: 'mobile', width: 390, height: 844 }]) {
    for (const locale of ['en', 'vi']) for (const theme of ['light', 'dark']) {
      const suffix = `${viewport.name}-${locale}-${theme}`
      const guest = await contextFor(null, viewport, locale, theme)
      const guestPage = await guest.newPage()
      await open(guestPage, '/admin/login', '[data-admin-login]')
      check(await guestPage.locator('h1').textContent() === dictionaries[locale].login.title, `${suffix}: localized sign-in`)
      await capture(guestPage, `login-${suffix}`)
      if (locale === 'en' && theme === 'light') {
        const vietnamese = guestPage.getByRole('button', { name: 'Tiếng Việt', exact: true })
        await vietnamese.focus()
        await guestPage.keyboard.press('Enter')
        await guestPage.getByRole('heading', { name: dictionaries.vi.login.title, exact: true }).waitFor()
        check(await vietnamese.getAttribute('aria-pressed') === 'true', `${suffix}: language changes by keyboard`)
        const themeButton = guestPage.getByRole('button', { name: dictionaries.vi.preferences.theme, exact: true })
        await themeButton.focus()
        await guestPage.keyboard.press('Enter')
        await guestPage.waitForFunction(() => document.documentElement.classList.contains('dark'))
        check(await themeButton.evaluate(element => element === document.activeElement), `${suffix}: theme changes by keyboard without losing focus`)
      }
      await guest.close()
      const context = await contextFor(adminSession, viewport, locale, theme)
      const page = await context.newPage()
      let probes = 0
      page.on('request', r => { if (/\/ai-probe|\/ai-health/.test(r.url())) probes++ })
      await open(page, '/admin', '[data-admin-dashboard]')
      check(await page.locator('[data-admin-header]').count() === 1, `${suffix}: one administrator header`)
      check(await page.locator('h1').textContent() === dictionaries[locale].dashboard.title, `${suffix}: localized overview`)
      check(probes === 0, `${suffix}: opening the dashboard does not call AI`)
      await capture(page, `dashboard-${suffix}`)
      if (viewport.name === 'mobile') {
        await page.getByRole('heading', { name: dictionaries[locale].dashboard.services, exact: true }).scrollIntoViewIfNeeded()
        await capture(page, `services-${suffix}`)
        const menu = page.getByRole('button', { name: dictionaries[locale].nav.open, exact: true })
        await menu.click()
        const dialog = page.getByRole('dialog')
        await dialog.waitFor()
        const focusables = dialog.locator('a[href], button:not([disabled]), [tabindex="0"]')
        await focusables.last().focus()
        await page.keyboard.press('Tab')
        check(await focusables.first().evaluate(element => element === document.activeElement), `${suffix}: drawer traps Tab focus`)
        await page.keyboard.press('Shift+Tab')
        check(await focusables.last().evaluate(element => element === document.activeElement), `${suffix}: drawer traps reverse Tab focus`)
        await capture(page, `drawer-${suffix}`)
        await page.keyboard.press('Escape')
        await dialog.waitFor({ state: 'hidden' })
        check(await menu.evaluate(element => element === document.activeElement), `${suffix}: Escape closes drawer and restores focus`)
      }
      await open(page, '/admin/catalog', '#admin-main')
      await page.getByText('Distributed Systems Field Notes', { exact: true }).first().waitFor()
      await capture(page, `catalog-${suffix}`)
      const beforeSwitch = await page.evaluate(() => localStorage.getItem('deeppace_user'))
      if (viewport.name === 'mobile') await page.getByRole('button', { name: dictionaries[locale].nav.open, exact: true }).click()
      await page.getByRole('link', { name: dictionaries[locale].nav.learning, exact: true }).filter({ visible: true }).first().click()
      await page.waitForURL(url => url.pathname === '/today')
      await page.waitForLoadState('networkidle')
      check(await page.evaluate(() => localStorage.getItem('deeppace_user')) === beforeSwitch, `${suffix}: switching to learning retains the same identity`)
      check(await page.locator('[data-admin-header]').count() === 0, `${suffix}: learning has no administrator chrome`)
      if (locale === 'vi' && theme === 'dark') await capture(page, `learning-${suffix}`)
      await context.close()
    }
  }

  const failureContext = await contextFor(adminSession, { width: 390, height: 844 }, 'vi', 'dark')
  const failurePage = await failureContext.newPage()
  await failurePage.route('**/api/v1/admin/dashboard/overview', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ code: 'ADMIN_DASHBOARD_UNAVAILABLE' }) }))
  await open(failurePage, '/admin', '[data-admin-dashboard]')
  check(await failurePage.getByText(dictionaries.vi.dashboard.overview_error, { exact: true }).isVisible(), 'An unavailable overview displays an error instead of fabricated zero counts')
  await capture(failurePage, 'unavailable-mobile-vi-dark')
  await failureContext.close()

  const transientContext = await contextFor(adminSession, { width: 390, height: 844 })
  const transientPage = await transientContext.newPage()
  await open(transientPage, '/admin', '[data-admin-dashboard]')
  await transientPage.route('**/api/v1/catalog/capabilities', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: '{}' }))
  await transientPage.getByRole('button', { name: dictionaries.en.nav.open, exact: true }).click()
  await transientPage.getByRole('dialog').getByRole('link', { name: dictionaries.en.nav.documents, exact: true }).click()
  await transientPage.getByText(dictionaries.en.access.unavailable, { exact: true }).waitFor()
  check(await transientPage.locator('[data-admin-header]').count() === 0 && await transientPage.locator('[data-admin-dashboard]').count() === 0, 'Transient permission failure withholds protected chrome and data')
  check(await transientPage.evaluate(() => JSON.parse(localStorage.getItem('deeppace_user')).id) === adminSession.user.id, 'Transient permission failure retains the authenticated identity')
  await capture(transientPage, 'permission-unavailable-mobile-en-light')
  await transientPage.unroute('**/api/v1/catalog/capabilities')
  await transientPage.getByRole('button', { name: dictionaries.en.access.retry, exact: true }).click()
  await transientPage.getByText('Distributed Systems Field Notes', { exact: true }).first().waitFor()
  check(await transientPage.locator('[data-admin-header]').count() === 1, 'Permission retry restores the authorized catalog shell')
  await transientContext.close()

  const terminalContext = await contextFor(adminSession)
  const terminalPage = await terminalContext.newPage()
  await terminalPage.route('**/api/v1/admin/dashboard/overview', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: JSON.stringify({ detail: 'Session rejected' }) }))
  await terminalPage.route('**/api/v1/auth/refresh', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: '{}' }))
  await terminalPage.goto(`${targetUrl}/admin`)
  await terminalPage.waitForURL(url => url.pathname === '/admin/login')
  await terminalPage.waitForFunction(() => localStorage.getItem('deeppace_token') === null)
  check(await terminalPage.locator('[data-admin-header]').count() === 0, 'Terminal admin 401 ends the session at admin login')
  await terminalContext.close()

  const revokedContext = await contextFor(adminSession, { width: 390, height: 844 }, 'vi', 'dark')
  const revokedPage = await revokedContext.newPage()
  let revoked = false
  await revokedPage.route('**/api/v1/catalog/capabilities', route => revoked ? route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ canManageCatalog: false }) }) : route.continue())
  await revokedPage.route('**/api/v1/admin/dashboard/overview', route => {
    revoked = true
    return route.fulfill({ status: 403, contentType: 'application/problem+json', body: '{}' })
  })
  await revokedPage.goto(`${targetUrl}/admin`)
  await revokedPage.waitForURL(url => url.pathname === '/admin/login')
  await revokedPage.getByText(dictionaries.vi.login.denied, { exact: true }).waitFor()
  check(await revokedPage.evaluate(() => JSON.parse(localStorage.getItem('deeppace_user')).id) === adminSession.user.id, 'Administrator 403 clears privileged access while retaining the learner session')
  await capture(revokedPage, 'denied-login-mobile-vi-dark')
  await revokedContext.close()

  const probeContext = await contextFor(adminSession)
  const probePage = await probeContext.newPage()
  await probePage.clock.install({ time: new Date() })
  await open(probePage, '/admin', '[data-admin-dashboard]')
  const probeButton = probePage.getByRole('button', { name: dictionaries.en.dashboard.check_ai, exact: true })
  const probeResponse = probePage.waitForResponse(response => response.url().endsWith('/api/v1/admin/dashboard/ai-probe'))
  await probeButton.click()
  const probe = await probeResponse
  check([200, 503].includes(probe.status()), 'Only an explicit button click runs the real bounded AI probe')
  await probePage.getByText(dictionaries.en.dashboard.service_not_checked, { exact: true }).first().waitFor({ state: 'hidden' })
  check(await probeButton.isDisabled(), 'An explicit probe starts the client cooldown')
  await capture(probePage, 'probe-desktop-en-light')
  await probePage.clock.fastForward(301_000)
  await probePage.getByText(dictionaries.en.dashboard.service_stale, { exact: true }).first().waitFor()
  check(await probePage.getByText(dictionaries.en.dashboard.service_stale, { exact: true }).count() >= 2, 'AI observations older than five minutes are marked stale')
  await capture(probePage, 'stale-desktop-en-light')
  await probePage.getByRole('button', { name: dictionaries.en.nav.logout, exact: true }).click()
  await probePage.waitForURL(url => url.pathname === '/admin/login')
  await probePage.waitForFunction(() => localStorage.getItem('deeppace_token') === null && localStorage.getItem('deeppace_user') === null)
  check(await probePage.evaluate(() => localStorage.getItem('deeppace_token') === null && localStorage.getItem('deeppace_user') === null), 'Admin logout clears the shared browser session')
  const postLogout = await probeContext.request.get(`${targetUrl}/today`, { maxRedirects: 0 })
  check(postLogout.status() === 302 && postLogout.headers().location.startsWith('/login'), 'Admin logout also ends learner admission')
  await probeContext.close()
  assert.deepEqual(browserErrors, [], 'No JavaScript exceptions or hydration mismatches across verified routes')
  checks.push('No JavaScript exceptions or hydration mismatches across verified routes')
  fs.writeFileSync(path.join(output, 'report.json'), JSON.stringify({ checks, screenshots, viewportMatrix: ['1440x900', '390x844'], locales: ['en', 'vi'], themes: ['light', 'dark'] }, null, 2))
  console.log(`PASS: ${checks.length} browser assertions, ${screenshots.length} screenshots. Report: ${path.join(output, 'report.json')}`)
} finally { await browser.close() }
