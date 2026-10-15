import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots/admin-i18n-polish')
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

const envPath = path.resolve(__dirname, '../../.env')
const envContent = fs.readFileSync(envPath, 'utf8')
const email = envContent.match(/E2E_PROD_EMAIL=(.+)/)?.[1]?.trim()
const password = envContent.match(/E2E_PROD_PASSWORD=(.+)/)?.[1]?.trim()

const browser = await chromium.launch({
  headless: true,
  args: ['--disable-web-security', '--no-sandbox']
})

const viewports = [
  { width: 1440, height: 900, label: 'desktop' },
  { width: 390, height: 844, label: 'mobile' }
]

for (const vp of viewports) {
  for (const locale of ['en', 'vi']) {
    console.log(`\n📸 Viewport ${vp.label} (${vp.width}x${vp.height}) - Locale: ${locale}`)
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height }
    })

    await context.addCookies([
      { name: 'i18n_redirected', value: locale, domain: 'localhost', path: '/' }
    ])

    const page = await context.newPage()

    // 1. Log in authentically via /admin/login
    console.log('Logging in via /admin/login...')
    await page.goto('http://localhost:3001/admin/login', { waitUntil: 'networkidle' })
    await page.waitForTimeout(1000)
    await page.fill('#admin-email', email)
    await page.fill('#admin-password', password)
    await page.click('button[type="submit"]')
    await page.waitForURL(url => url.pathname === '/admin', { timeout: 15000 })
    console.log('✅ Logged in successfully!')
    await page.waitForTimeout(1000)

    // Ensure desired language
    const localeBtn = page.locator('button', { hasText: locale.toUpperCase() }).first()
    if (await localeBtn.isVisible()) {
      await localeBtn.click()
      await page.waitForTimeout(600)
    }

    // 2. /admin/audit
    console.log(`Navigating to /admin/audit (${vp.label}-${locale})...`)
    await page.goto('http://localhost:3001/admin/audit', { waitUntil: 'networkidle' })
    await page.waitForTimeout(2000)
    await page.screenshot({
      path: path.join(screenshotsDir, `audit-${vp.label}-${locale}.png`),
      fullPage: false
    })

    // Open AppSelect outcome dropdown
    const outcomeTrigger = page.locator('[data-testid="app-select-trigger"]').first()
    if (await outcomeTrigger.isVisible()) {
      await outcomeTrigger.click()
      await page.waitForTimeout(500)
      await page.screenshot({
        path: path.join(screenshotsDir, `audit-select-open-${vp.label}-${locale}.png`),
        fullPage: false
      })
      await page.keyboard.press('Escape')
      await page.waitForTimeout(300)
    }

    // 3. /admin/documents
    console.log(`Navigating to /admin/documents (${vp.label}-${locale})...`)
    await page.goto('http://localhost:3001/admin/documents', { waitUntil: 'networkidle' })
    await page.waitForTimeout(2500)

    // Select the first document in the master rail to show detail
    const firstDoc = page.locator('div.flex-1.overflow-y-auto button').first()
    if (await firstDoc.isVisible()) {
      await firstDoc.click()
      await page.waitForTimeout(1200)
    }

    await page.screenshot({
      path: path.join(screenshotsDir, `documents-${vp.label}-${locale}.png`),
      fullPage: false
    })

    // 4. /admin/catalog
    console.log(`Navigating to /admin/catalog (${vp.label}-${locale})...`)
    await page.goto('http://localhost:3001/admin/catalog', { waitUntil: 'networkidle' })
    await page.waitForTimeout(2500)

    // Select the first document in catalog rail to inspect header pill
    const firstCatalogDoc = page.locator('div.flex-1.overflow-y-auto button').first()
    if (await firstCatalogDoc.isVisible()) {
      await firstCatalogDoc.click()
      await page.waitForTimeout(1200)
    }

    await page.screenshot({
      path: path.join(screenshotsDir, `catalog-detail-${vp.label}-${locale}.png`),
      fullPage: false
    })

    await context.close()
  }
}

await browser.close()
console.log('\n🎉 Real session screenshots captured successfully in:', screenshotsDir)
