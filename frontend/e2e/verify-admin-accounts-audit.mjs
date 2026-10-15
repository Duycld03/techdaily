import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots/admin-audit')
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

const envPath = path.resolve(__dirname, '../../.env')
const envContent = fs.readFileSync(envPath, 'utf8')
const email = envContent.match(/E2E_PROD_EMAIL=(.+)/)?.[1]?.trim()
const password = envContent.match(/E2E_PROD_PASSWORD=(.+)/)?.[1]?.trim()

console.log('🔑 Authenticating Admin E2E user:', email)

const loginRes = await fetch('http://localhost:5000/api/v1/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
})

if (!loginRes.ok) {
  throw new Error(`Login failed with status ${loginRes.status}: ${await loginRes.text()}`)
}

const loginData = await loginRes.json()
const token = loginData.accessToken || loginData.token
const user = loginData.user

console.log('✅ Authenticated successfully! User:', user.name)
const browser = await chromium.launch({
  headless: true,
  executablePath: '/home/duycld03/.omp/puppeteer/chrome/linux-150.0.7871.24/chrome-linux64/chrome',
  args: ['--enable-webgl', '--use-gl=angle', '--no-sandbox']
})

async function runForViewport(width, height, deviceLabel) {
  for (const locale of ['en', 'vi']) {
    console.log(`\n📸 Viewport ${deviceLabel} (${width}x${height}) - Locale: ${locale}`)
    const context = await browser.newContext({
      viewport: { width, height }
    })

    await context.addCookies([
      { name: 'techdaily_token', value: token, domain: 'localhost', path: '/' },
      { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
      { name: 'deeppace_token', value: token, domain: 'localhost', path: '/' },
      { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
      { name: 'i18n_redirected', value: locale, domain: 'localhost', path: '/' }
    ])

    const page = await context.newPage()

    // 1. /admin/users
    console.log(`Navigating to /admin/users (${deviceLabel}-${locale})...`)
    await page.goto('http://localhost:3000/admin/users', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)
    await page.screenshot({
      path: path.join(screenshotsDir, `admin-users-${deviceLabel}-${locale}.png`),
      fullPage: true
    })

    // 2. /admin/audit
    console.log(`Navigating to /admin/audit (${deviceLabel}-${locale})...`)
    await page.goto('http://localhost:3000/admin/audit', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)
    await page.screenshot({
      path: path.join(screenshotsDir, `admin-audit-${deviceLabel}-${locale}.png`),
      fullPage: true
    })

    // 3. /admin/catalog
    console.log(`Navigating to /admin/catalog (${deviceLabel}-${locale})...`)
    await page.goto('http://localhost:3000/admin/catalog', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)
    await page.screenshot({
      path: path.join(screenshotsDir, `admin-catalog-${deviceLabel}-${locale}.png`),
      fullPage: true
    })

    await context.close()
  }
}

try {
  await runForViewport(1440, 900, 'desktop')
  await runForViewport(390, 844, 'mobile')
  console.log('\n🎉 All Admin visual verification screenshots captured successfully!')
} finally {
  await browser.close()
}
