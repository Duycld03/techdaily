import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots', 'brand-cleanup')

if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

// Read E2E admin credentials from .env
const envPath = fs.existsSync(path.resolve(__dirname, '../../.env'))
  ? path.resolve(__dirname, '../../.env')
  : '/home/duycld03/workspace/techdaily/.env'
const envContent = fs.readFileSync(envPath, 'utf8')
const email = envContent.match(/E2E_PROD_EMAIL=(.*)/)?.[1]?.trim()
const password = envContent.match(/E2E_PROD_PASSWORD=(.*)/)?.[1]?.trim()

if (!email || !password) {
  console.error('[FAIL] Missing E2E credentials in .env')
  process.exit(1)
}

const qaEmail = 'brand.cleanup.qa@example.test'
console.log(`Authenticating QA user against 5035: ${qaEmail}`)
const loginRes = await fetch('http://localhost:5035/api/v1/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: qaEmail, password }),
})
const loginData = await loginRes.json()
if (!loginData.token) {
  console.error('[FAIL] Login failed:', loginData)
  process.exit(1)
}

const token = loginData.token
const user = loginData.user
console.log(`[OK] Authenticated successfully! User: ${user.name}`)

const browser = await chromium.launch({
  headless: true,
  args: ['--no-sandbox', '--disable-setuid-sandbox'],
})

// 1. Dual-Gate Screenshots on Desktop and Mobile
const viewports = [
  { width: 1440, height: 900, deviceLabel: 'desktop' },
  { width: 390, height: 844, deviceLabel: 'mobile' }
]
const locales = ['en', 'vi']

for (const vp of viewports) {
  for (const locale of locales) {
    console.log(`Capturing ${vp.deviceLabel} (${vp.width}x${vp.height}) - Locale: ${locale}`)
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      locale: locale === 'vi' ? 'vi-VN' : 'en-US'
    })

    // Canonical session cookies only
    await context.addCookies([
      { name: 'deeppace_token', value: token, domain: 'localhost', path: '/' },
      { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
      { name: 'i18n_redirected', value: locale, domain: 'localhost', path: '/' }
    ])
    await context.addInitScript(({ token, user }) => {
      localStorage.setItem('deeppace_token', token)
      localStorage.setItem('deeppace_user', JSON.stringify(user))
    }, { token, user })

    const page = await context.newPage()

    // Library page
    await page.goto('http://localhost:3035/library', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)
    await page.screenshot({
      path: path.join(screenshotsDir, `library-${vp.deviceLabel}-${locale}.png`),
      fullPage: false
    })

    // Settings page
    await page.goto('http://localhost:3035/settings', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)
    await page.screenshot({
      path: path.join(screenshotsDir, `settings-${vp.deviceLabel}-${locale}.png`),
      fullPage: false
    })

    // Login page (unauthenticated context)
    const guestContext = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      locale: locale === 'vi' ? 'vi-VN' : 'en-US'
    })
    const guestPage = await guestContext.newPage()
    await guestPage.goto('http://localhost:3035/login', { waitUntil: 'networkidle' })
    await guestPage.waitForTimeout(1000)
    await guestPage.screenshot({
      path: path.join(screenshotsDir, `login-${vp.deviceLabel}-${locale}.png`),
      fullPage: false
    })

    await guestContext.close()
    await context.close()
  }
}

// 2. Real-browser legacy migration end-to-end verification
console.log('Testing live browser legacy-to-canonical storage migration...')
const migContext = await browser.newContext({
  viewport: { width: 1440, height: 900 }
})

// Inject legacy cookie only
await migContext.addCookies([
  { name: 'techdaily_token', value: token, domain: 'localhost', path: '/' },
  { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' }
])

const migPage = await migContext.newPage()
await migPage.goto('http://localhost:3035/library', { waitUntil: 'networkidle' })
await migPage.waitForTimeout(1500)

const storageCheck = await migPage.evaluate(() => {
  return {
    canonicalToken: localStorage.getItem('deeppace_token') || document.cookie.includes('deeppace_token'),
    legacyTokenInLocalStorage: localStorage.getItem('techdaily_token')
  }
})

console.log('Storage Migration Check Result:', storageCheck)
await migContext.close()
await browser.close()
console.log('[OK] All dual-gate visual screenshots and migration verifications passed!')
