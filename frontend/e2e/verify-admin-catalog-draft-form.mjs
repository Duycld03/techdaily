import assert from 'node:assert/strict'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium, request } from 'playwright'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..')
const targetUrl = process.env.TARGET_URL || 'http://localhost:3000'
const apiUrl = process.env.API_URL || 'http://localhost:5000'
const output = path.join(root, 'frontend/e2e/screenshots/catalog-draft')
fs.mkdirSync(output, { recursive: true })

const envFile = path.join(root, '.env')
const env = fs.existsSync(envFile) ? Object.fromEntries(fs.readFileSync(envFile, 'utf8').split('\n')
  .filter(line => line.trim() && !line.startsWith('#') && line.includes('='))
  .map(line => {
    const split = line.indexOf('=')
    return [line.slice(0, split).trim(), line.slice(split + 1).trim().replace(/^(['"])(.*)\1$/, '$2')]
  })) : {}

const admin = { email: env.E2E_PROD_EMAIL, password: env.E2E_PROD_PASSWORD }
assert(admin.email && admin.password, 'Real administrator credentials are required.')

// Sign in via API
const client = await request.newContext({ baseURL: apiUrl })
const loginRes = await client.post('/api/v1/auth/login', {
  data: { ...admin, rememberMe: true }
})
assert.equal(loginRes.status(), 200, 'Admin login must succeed.')
const session = await loginRes.json()
await client.dispose()

const browser = await chromium.launch({ headless: true })

function createStorageState(locale = 'vi') {
  const host = new URL(targetUrl).hostname
  const cookie = (name, value) => ({ name, value, domain: host, path: '/', expires: -1, httpOnly: false, secure: false, sameSite: 'Lax' })
  return {
    cookies: [
      cookie('i18n_redirected', locale),
      cookie('techdaily_token', session.token),
      cookie('techdaily_user', encodeURIComponent(JSON.stringify(session.user))),
      cookie('deeppace_token', session.token),
      cookie('deeppace_user', encodeURIComponent(JSON.stringify(session.user)))
    ],
    origins: [{
      origin: new URL(targetUrl).origin,
      localStorage: [
        { name: 'nuxt-color-mode', value: 'dark' },
        { name: 'techdaily_token', value: session.token },
        { name: 'techdaily_user', value: JSON.stringify(session.user) },
        { name: 'deeppace_token', value: session.token },
        { name: 'deeppace_user', value: JSON.stringify(session.user) }
      ]
    }]
  }
}

console.log('Testing Desktop Viewport (1440x900)...')
{
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    storageState: createStorageState({ width: 1440, height: 900 }, 'vi')
  })
  const page = await context.newPage()
  await page.goto(`${targetUrl}/admin/catalog`, { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)

  // Click "+ Tạo Bản Nháp" button if draft form is not open
  const newDraftBtn = page.locator('button:has-text("Tạo Bản Nháp"), button:has-text("New Draft")').first()
  if (await newDraftBtn.isVisible()) {
    await newDraftBtn.click()
    await page.waitForTimeout(500)
  }

  // Switch to Web URL mode
  const webModeBtn = page.locator('button:has-text("Nhập Từ Web URL"), button:has-text("Web URL")').first()
  await webModeBtn.click()
  await page.waitForTimeout(500)

  // Fill URL input
  const urlInput = page.locator('input[type="url"]').first()
  await urlInput.fill('https://nextjs.org/docs')

  // Capture desktop screenshot before preview
  await page.screenshot({ path: path.join(output, 'admin-catalog-draft-web-desktop.png'), fullPage: true })
  console.log('✓ Captured admin-catalog-draft-web-desktop.png')

  // Trigger preview
  const previewBtn = page.locator('button:has-text("Xem Trước Trích Xuất"), button:has-text("Preview")').first()
  await previewBtn.click()
  await page.waitForTimeout(3000)

  // Capture desktop screenshot with preview result loaded
  await page.screenshot({ path: path.join(output, 'admin-catalog-draft-preview-desktop.png'), fullPage: true })
  console.log('✓ Captured admin-catalog-draft-preview-desktop.png')

  await context.close()
}

console.log('Testing Mobile Viewport (390x844)...')
{
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    storageState: createStorageState({ width: 390, height: 844 }, 'vi')
  })
  const page = await context.newPage()
  await page.goto(`${targetUrl}/admin/catalog`, { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)

  // Click "+ Tạo Bản Nháp"
  const newDraftBtn = page.locator('button:has-text("Tạo Bản Nháp"), button:has-text("New Draft")').first()
  if (await newDraftBtn.isVisible()) {
    await newDraftBtn.click()
    await page.waitForTimeout(500)
  }

  // Switch to Web URL mode
  const webModeBtn = page.locator('button:has-text("Nhập Từ Web URL"), button:has-text("Web URL")').first()
  await webModeBtn.click()
  await page.waitForTimeout(500)

  // Fill URL input
  const urlInput = page.locator('input[type="url"]').first()
  await urlInput.fill('https://nextjs.org/docs')

  // Capture mobile screenshot
  await page.screenshot({ path: path.join(output, 'admin-catalog-draft-web-mobile.png'), fullPage: true })
  console.log('✓ Captured admin-catalog-draft-web-mobile.png')

  await context.close()
}

await browser.close()
console.log('Dual-gate visual verification completed successfully.')
