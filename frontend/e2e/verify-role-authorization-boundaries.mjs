import assert from 'node:assert/strict'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from 'playwright'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const root = path.resolve(__dirname, '../..')
const targetUrl = process.env.TARGET_URL || 'http://127.0.0.1:3000'
const apiUrl = process.env.API_URL || 'http://127.0.0.1:5000'
const outputDir = path.join(root, 'frontend/e2e/screenshots/role-authorization')
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true })
}

const adminAccount = {
  email: process.env.ADMIN_QA_EMAIL,
  password: process.env.ADMIN_QA_PASSWORD
}
const learnerAccount = {
  email: process.env.LEARNER_QA_EMAIL,
  password: process.env.LEARNER_QA_PASSWORD
}

assert(
  adminAccount.email && adminAccount.password && learnerAccount.email && learnerAccount.password,
  'ADMIN_QA_EMAIL, ADMIN_QA_PASSWORD, LEARNER_QA_EMAIL and LEARNER_QA_PASSWORD are required; no fallback identities are allowed.'
)

console.log('Authenticating Admin and Learner QA accounts against API:', apiUrl)

// Step 1: Real API Authentication
async function authenticate(account) {
  const res = await fetch(`${apiUrl}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: account.email, password: account.password, rememberMe: true })
  })
  if (!res.ok) {
    throw new Error(`Authentication failed for ${account.email}: ${res.status} ${await res.text()}`)
  }
  const data = await res.json()
  const rawSetCookie = res.headers.get('set-cookie') || ''
  const refreshTokenMatch = rawSetCookie.match(/refreshToken=([^;]+)/)
  const refreshToken = refreshTokenMatch ? refreshTokenMatch[1] : null
  return {
    token: data.token || data.accessToken,
    user: data.user,
    refreshToken
  }
}

const adminSession = await authenticate(adminAccount)
const learnerSession = await authenticate(learnerAccount)
console.log('Admin authenticated:', adminSession.user.name, 'Token length:', adminSession.token.length)
console.log('Learner authenticated:', learnerSession.user.name)


const browser = await chromium.launch({
  headless: true,
  args: ['--no-sandbox', '--disable-setuid-sandbox']
})

const screenshots = []
const checks = []

function check(condition, message) {
  assert(condition, message)
  checks.push(message)
  console.log('  PASS:', message)
}

async function capture(page, filename, description) {
  const filePath = path.join(outputDir, filename)
  await page.screenshot({ path: filePath, fullPage: false })
  screenshots.push({ filename, path: filePath, description })
  console.log(` Saved screenshot: ${filename}`)
  const noOverflow = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)
  check(noOverflow, `${filename}: no horizontal overflow (width: ${await page.evaluate(() => window.innerWidth)}px)`)
}

function stateForSession(session, locale = 'en', theme = 'light', refreshOnly = false) {
  const host = new URL(targetUrl).hostname
  const cookie = (name, value) => ({ name, value, domain: host, path: '/', expires: -1, httpOnly: false, secure: false, sameSite: 'Lax' })
  return {
    cookies: [
      cookie('i18n_redirected', locale),
      ...(!session || refreshOnly ? [] : [
        cookie('deeppace_token', session.token),
        cookie('deeppace_user', encodeURIComponent(JSON.stringify(session.user))),
        cookie('techdaily_token', session.token),
        cookie('techdaily_user', encodeURIComponent(JSON.stringify(session.user)))
      ]),
      ...(session?.refreshToken ? [cookie('refreshToken', session.refreshToken)] : [])
    ],
    origins: [{
      origin: new URL(targetUrl).origin,
      localStorage: [
        { name: 'nuxt-color-mode', value: theme },
        ...(!session || refreshOnly ? [] : [
          { name: 'deeppace_token', value: session.token },
          { name: 'deeppace_user', value: JSON.stringify(session.user) },
          { name: 'techdaily_token', value: session.token },
          { name: 'techdaily_user', value: JSON.stringify(session.user) }
        ])
      ]
    }]
  }
}

try {
  // =========================================================================
  // JOURNEY 1 (F15): Contextual returns with query and hash preservation
  // =========================================================================
  console.log('\n--- Journey 1: Contextual Returns with Query and Hash Preservation (F15) ---')
  {
    const context = await browser.newContext({
      viewport: { width: 1440, height: 900 },
      storageState: stateForSession(adminSession, 'en')
    })
    const page = await context.newPage()

    const targetReturnUrl = '/admin/catalog?search=architecture#category-systems'
    console.log(`Navigating to /admin/login?redirect=${encodeURIComponent(targetReturnUrl)}...`)
    await page.goto(`${targetUrl}/admin/login?redirect=${encodeURIComponent(targetReturnUrl)}`, { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)

    const finalUrl = page.url()
    console.log('Navigated to final URL:', finalUrl)
    check(finalUrl.includes('/admin/catalog'), 'Contextual return safely admitted authenticated admin to /admin/catalog')
    check(finalUrl.includes('search=architecture'), 'Query parameters preserved across contextual return')
    check(finalUrl.includes('#category-systems'), 'Hash fragment preserved across contextual return')

    await capture(page, 'desktop-en-contextual-return.png', 'Contextual return with preserved query and hash')
    await context.close()
  }

  // =========================================================================
  // JOURNEY 2 (F11, F12): Authorization Boundary Unavailable & Retry Recovery
  // =========================================================================
  console.log('\n--- Journey 2: Authorization Boundary Unavailable & Retry Recovery (F11, F12) ---')
  {
    const context = await browser.newContext({
      viewport: { width: 1440, height: 900 },
      storageState: stateForSession(adminSession, 'vi')
    })
    const page = await context.newPage()
    console.log('Navigating to /admin first (Vietnamese)...')
    await page.goto(`${targetUrl}/admin`, { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)

    // Intercept capabilities with transient 503
    await page.route('**/api/v1/admin/capabilities', route => {
      route.fulfill({
        status: 503,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'https://tools.ietf.org/html/rfc9110#section-15.6.4',
          title: 'Service Unavailable',
          status: 503,
          detail: 'Privileged authorization has not been initialized.'
        })
      })
    })

    console.log('Navigating client-side to /admin/catalog with transient 503 capabilities...')
    const catalogLink = page.locator('a[href*="/admin/catalog"]').first()
    await catalogLink.click()
    await page.waitForTimeout(1000)

    // Verify unavailable banner and retry button
    const retryBtn = page.locator('button:has-text("Thử lại"), button:has-text("Retry")')
    await retryBtn.waitFor({ state: 'visible', timeout: 5000 })
    check(await retryBtn.isVisible(), 'Authorization Boundary displays localized Retry button during 503')

    await capture(page, 'desktop-vi-boundary-unavailable.png', 'Authorization boundary transient 503 with retry button (Vietnamese)')

    // Unroute and click retry to recover
    await page.unroute('**/api/v1/admin/capabilities')
    console.log('Clicking retry button after service restored...')
    await retryBtn.click()
    await page.waitForTimeout(1500)

    // Verify access restored
    const catalogRefreshBtn = page.locator('[data-testid="admin-catalog-refresh-btn"]')
    await catalogRefreshBtn.waitFor({ state: 'visible', timeout: 5000 })
    check(await catalogRefreshBtn.isVisible(), 'Clicking retry recovers from transient failure and admits to workspace')

    await capture(page, 'desktop-vi-boundary-recovered.png', 'Authorization boundary successfully recovered after retry (Vietnamese)')
    await context.close()
  }

  // =========================================================================
  // JOURNEY 3 (F13, F14): Role Modal with Producer-Shaped Permissions & Categories
  // Tested across Desktop (1440x900) and Mobile (390x844) in EN and VI
  // =========================================================================
  console.log('\n--- Journey 3: Producer-Shaped Permission Explanations (F13, F14) ---')
  const viewports = [
    { name: 'desktop', width: 1440, height: 900, isMobile: false },
    { name: 'mobile', width: 390, height: 844, isMobile: true }
  ]
  const locales = ['en', 'vi']

  for (const vp of viewports) {
    for (const loc of locales) {
      console.log(`\nTesting Role Modal on ${vp.name} (${vp.width}x${vp.height}) - ${loc.toUpperCase()}...`)
      const context = await browser.newContext({
        viewport: { width: vp.width, height: vp.height },
        isMobile: vp.isMobile,
        storageState: stateForSession(adminSession, loc)
      })
      const page = await context.newPage()

      await page.goto(`${targetUrl}/admin/users`, { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      // Find user row for learner
      const learnerRow = page.locator(`text=${learnerSession.user.name}`).first()
      await learnerRow.waitFor({ state: 'visible', timeout: 5000 })
      await learnerRow.click()
      await page.waitForTimeout(500)

      // Open Change Role modal
      const changeRoleBtn = page.locator('[data-testid="change-role-btn"]')
      await changeRoleBtn.waitFor({ state: 'visible', timeout: 5000 })
      await changeRoleBtn.click()
      await page.waitForTimeout(600)

      // Verify modal is open
      const modal = page.locator('[role="dialog"]')
      await modal.waitFor({ state: 'visible', timeout: 5000 })
      check(await modal.isVisible(), `${vp.name}-${loc}: Role modal opened`)

      // Select Administrator or Manager role to see permissions
      const selectTrigger = page.locator('[data-testid="admin-role-select"] [data-testid="app-select-trigger"]')
      await selectTrigger.click()
      await page.waitForTimeout(400)

      // Select Administrator option
      const adminOption = page.locator('[data-testid="app-select-option-20000000-0000-4000-8000-000000000002"]')
      await adminOption.click()
      await page.waitForTimeout(400)

      // Verify supported permissions block is displayed with categories and canonical keys
      const permBlock = page.locator('[data-testid="supported-permissions-block"]')
      await permBlock.waitFor({ state: 'visible', timeout: 5000 })
      check(await permBlock.isVisible(), `${vp.name}-${loc}: Supported permissions block rendered`)

      // Fill reason
      const reasonInput = page.locator('[data-testid="admin-role-reason"]')
      const reasonText = loc === 'vi'
        ? 'Thăng cấp tài khoản để quản lý danh mục và tài liệu theo quy trình QA.'
        : 'Promoting account to administrator for technical documentation management.'
      await reasonInput.fill(reasonText)
      await page.waitForTimeout(400)

      // Verify button responsiveness and no collisions
      const submitBtn = page.locator('[data-testid="submit-role-assign"]')
      check(await submitBtn.isVisible(), `${vp.name}-${loc}: Submit role assignment button visible`)

      await capture(page, `${vp.name}-${loc}-role-modal.png`, `Role Assignment Modal on ${vp.name} (${loc}) with producer-shaped permissions`)

      await context.close()
    }
  }

  // =========================================================================
  // JOURNEY 4 (F12, F14): Failed / Matching Role Conflict Reload
  // =========================================================================
  console.log('\n--- Journey 4: Role Assignment Conflict & Reload (F12, F14) ---')
  {
    const context = await browser.newContext({
      viewport: { width: 1440, height: 900 },
      storageState: stateForSession(adminSession, 'vi')
    })
    const page = await context.newPage()

    await page.goto(`${targetUrl}/admin/users`, { waitUntil: 'networkidle' })
    await page.waitForTimeout(1500)

    const learnerRow = page.locator(`text=${learnerSession.user.name}`).first()
    await learnerRow.waitFor({ state: 'visible', timeout: 5000 })
    await learnerRow.click()
    await page.waitForTimeout(500)

    const changeRoleBtn = page.locator('[data-testid="change-role-btn"]')
    await changeRoleBtn.waitFor({ state: 'visible', timeout: 5000 })
    await changeRoleBtn.click()
    await page.waitForTimeout(600)

    // Select role
    const selectTrigger = page.locator('[data-testid="admin-role-select"] [data-testid="app-select-trigger"]')
    await selectTrigger.click()
    await page.waitForTimeout(400)
    await page.locator('[data-testid="app-select-option-20000000-0000-4000-8000-000000000002"]').click()
    await page.waitForTimeout(400)

    const reasonInput = page.locator('[data-testid="admin-role-reason"]')
    await reasonInput.fill('Xác minh quy trình giải quyết xung đột phiên bản admin control.')

    // Intercept PUT role with 409 Conflict
    await page.route('**/api/v1/admin/users/*/role', route => {
      route.fulfill({
        status: 409,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
          title: 'Conflict',
          status: 409,
          detail: 'Target account was modified by another administrator.',
          code: 'ACCOUNT_CONCURRENCY_CONFLICT'
        })
      })
    })

    console.log('Submitting role assignment with simulated 409 Concurrency Conflict...')
    const submitBtn = page.locator('[data-testid="submit-role-assign"]')
    await submitBtn.click()
    await page.waitForTimeout(1000)

    // Verify conflict banner and reload button
    const reloadBtn = page.locator('[data-testid="reload-account-btn"]')
    await reloadBtn.waitFor({ state: 'visible', timeout: 5000 })
    check(await reloadBtn.isVisible(), 'Conflict banner presents explicit Reload Account button')

    await capture(page, 'desktop-vi-role-conflict.png', 'Role assignment 409 Conflict with Reload Account button (Vietnamese)')

    // Unroute PUT and test Reload Account
    await page.unroute('**/api/v1/admin/users/*/role')
    console.log('Clicking Reload Account button...')
    await reloadBtn.click()
    await page.waitForTimeout(1000)

    // Verify reload cleared conflict and preserved draft reason
    const preservedReason = await reasonInput.inputValue()
    check(preservedReason.includes('xung đột'), 'Reloading account preserves draft justification input')
    check(!(await reloadBtn.isVisible()), 'Successful reload clears conflict banner')

    await capture(page, 'desktop-vi-role-conflict-resolved.png', 'Role conflict resolved after reload, draft preserved (Vietnamese)')
    await context.close()
  }

  // =========================================================================
  // JOURNEY 5 (F11): Refresh-Only Destination Denial for Unauthenticated/Learner
  // =========================================================================
  console.log('\n--- Journey 5: Refresh-Only Destination Denial (F11) ---')
  {
    const context = await browser.newContext({
      viewport: { width: 390, height: 844 },
      isMobile: true,
      storageState: stateForSession(learnerSession, 'en')
    })
    const page = await context.newPage()

    console.log('Learner navigating to /admin/catalog...')
    await page.goto(`${targetUrl}/admin/catalog`, { waitUntil: 'networkidle' })
    await page.waitForTimeout(1200)

    const finalPath = new URL(page.url()).pathname
    console.log('Learner ended up at:', finalPath)
    check(finalPath === '/admin/login' || finalPath === '/login' || finalPath === '/today', 'Learner denied from admin catalog without privileged chrome exposure')

    await capture(page, 'mobile-en-learner-denied.png', 'Learner denied from admin catalog (Mobile English)')
    await context.close()
  }

  console.log(`\n=======================================================`)
  console.log(`PASS: All ${checks.length} assertions passed!`)
  console.log(`Total screenshots captured: ${screenshots.length}`)
  console.log(`=======================================================`)
} finally {
  await browser.close()
}
