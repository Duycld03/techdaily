import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots/admin-documents')
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

    // 1. /admin/documents
    console.log(`Navigating to /admin/documents (${deviceLabel}-${locale})...`)
    await page.goto('http://localhost:3000/admin/documents', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1500)

    // Click on the first failed document to display retry eligibility and attempt history
    const failedPill = page.locator('button', { hasText: locale === 'vi' ? 'Thất bại' : 'Failed' }).first()
    if (await failedPill.isVisible()) {
      await failedPill.click()
      await page.waitForTimeout(1500)
    }

    const firstDoc = page.locator('button:has-text("Distributed Cache")').first()
    if (await firstDoc.isVisible()) {
      await firstDoc.click()
      await page.waitForTimeout(1500)
    }
    await page.screenshot({
      path: path.join(screenshotsDir, `admin-documents-${deviceLabel}-${locale}.png`),
      fullPage: true
    })

    // Capture Retry Modal on Desktop
    if (deviceLabel === 'desktop') {
      const retryBtn = page.locator('button:has-text("Retry Processing"), button:has-text("Thử lại xử lý")').first()
      if (await retryBtn.isVisible()) {
        console.log(`Clicking retry button for locale: ${locale}...`)
        await retryBtn.click()
        await page.locator('#retry-reason-input').waitFor({ state: 'visible', timeout: 5000 })
        await page.waitForTimeout(500)
        await page.screenshot({
          path: path.join(screenshotsDir, `admin-documents-retry-modal-${locale}.png`),
          fullPage: true
        })
        const cancelBtn = page.locator('button', { hasText: locale === 'vi' ? 'Hủy bỏ' : 'Cancel' }).first()
        if (await cancelBtn.isVisible()) {
          await cancelBtn.click()
          await page.waitForTimeout(500)
        }
      } else {
        console.log(`Retry button not visible for locale: ${locale}`)
      }
    }

    // 2. /library
    console.log(`Navigating to /library (${deviceLabel}-${locale})...`)
    await page.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1500)
    await page.screenshot({
      path: path.join(screenshotsDir, `library-${deviceLabel}-${locale}.png`),
      fullPage: true
    })

    await context.close()
  }
}

try {
  await runForViewport(1440, 900, 'desktop')
  await runForViewport(390, 844, 'mobile')
  console.log('\n🎉 Visual screenshot inspection complete!')
} finally {
  await browser.close()
}
