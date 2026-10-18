import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots')
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

const manifestPath = '/tmp/deeppace-library-state-qa-7c92/fixture-manifest.json'
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'))
const learnerAcc = manifest.accounts.find(a => a.key === 'learner')
const adminAcc = manifest.accounts.find(a => a.key === 'admin')

async function login(email, password) {
  const res = await fetch('http://localhost:5000/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })
  if (!res.ok) {
    throw new Error(`Login failed for ${email}: ${await res.text()}`)
  }
  return await res.json()
}

const learnerAuth = await login(learnerAcc.Email, learnerAcc.password)
const adminAuth = await login(adminAcc.Email, adminAcc.password)

console.log('✅ Logged in learner:', learnerAuth.user?.name, learnerAuth.user?.email)
console.log('✅ Logged in admin:', adminAuth.user?.name, adminAuth.user?.email)

const browser = await chromium.launch({
  headless: true,
  executablePath: '/home/duycld03/.omp/puppeteer/chrome/linux-150.0.7871.24/chrome-linux64/chrome',
  args: ['--enable-webgl', '--use-gl=angle', '--no-sandbox']
})

try {
  // -------------------------------------------------------------
  // Part 1: Learner Journey (Desktop & Mobile, English & Vietnamese)
  // -------------------------------------------------------------
  for (const locale of ['vi', 'en']) {
    for (const [width, height, label] of [[1440, 900, 'desktop'], [390, 844, 'mobile']]) {
      console.log(`\n--- Learner ${label} (${width}x${height}) [${locale}] ---`)
      const context = await browser.newContext({ viewport: { width, height } })
      await context.addCookies([
        { name: 'deeppace_token', value: learnerAuth.token, domain: 'localhost', path: '/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(learnerAuth.user)), domain: 'localhost', path: '/' },
        { name: 'techdaily_token', value: learnerAuth.token, domain: 'localhost', path: '/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(learnerAuth.user)), domain: 'localhost', path: '/' },
        { name: 'i18n_redirected', value: locale, domain: 'localhost', path: '/' }
      ])
      const page = await context.newPage()
      await page.addInitScript(({ token, user }) => {
        localStorage.setItem('deeppace_token', token)
        localStorage.setItem('deeppace_user', JSON.stringify(user))
        localStorage.setItem('techdaily_token', token)
        localStorage.setItem('techdaily_user', JSON.stringify(user))
      }, { token: learnerAuth.token, user: learnerAuth.user })

      // 1. My Library
      await page.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1000)

      // Verify no admin links in DOM
      const adminLinks = await page.$$eval('a[href*="/admin"]', elms => elms.map(e => e.href))
      if (adminLinks.length > 0) {
        console.error('❌ Detected leaked admin links in learner DOM:', adminLinks)
      } else {
        console.log('✅ Learner DOM verified: zero admin links.')
      }

      const mineShot = path.join(screenshotsDir, `library-${label}-mine-${locale}.png`)
      await page.screenshot({ path: mineShot })
      console.log('📸 Saved:', mineShot)

      // 2. Discover Tab
      await page.goto('http://localhost:3000/library?tab=discover', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1000)

      // Exercise search & clear
      const searchInput = page.locator('input[type="text"]').first()
      if (await searchInput.isVisible()) {
        await searchInput.fill('Distributed')
        await searchInput.press('Enter')
        await page.waitForTimeout(600)

        // Clear search
        const clearBtn = page.locator('button[aria-label="Clear search"], button[aria-label="Xóa tìm kiếm"]').first()
        if (await clearBtn.isVisible()) {
          console.log('✅ Clear search button visible, clicking it...')
          await clearBtn.click()
          await page.waitForTimeout(600)
        }
      }

      // Exercise category filter
      const categoryChip = page.locator('[data-testid="library-category-chips"] button').nth(1)
      if (await categoryChip.isVisible()) {
        await categoryChip.click()
        await page.waitForTimeout(600)
      }

      const discoverShot = path.join(screenshotsDir, `library-${label}-discover-${locale}.png`)
      await page.screenshot({ path: discoverShot })
      console.log('📸 Saved:', discoverShot)

      // 3. Open Filter Modal
      const filterBtn = page.locator('[data-testid="catalog-advanced-filter-btn"], [data-testid="library-advanced-filter-btn"]').first()
      if (await filterBtn.isVisible()) {
        await filterBtn.click()
        await page.waitForSelector('[role="dialog"]', { timeout: 3000 })
        await page.waitForTimeout(500)
        const modalShot = path.join(screenshotsDir, `library-filter-modal-${label}-${locale}.png`)
        await page.screenshot({ path: modalShot })
        console.log('📸 Saved:', modalShot)
        await page.keyboard.press('Escape')
        await page.waitForTimeout(300)
      }

      await context.close()
    }
  }

  // -------------------------------------------------------------
  // Part 2: Admin Document Operations (Desktop & Mobile, English & Vietnamese)
  // -------------------------------------------------------------
  for (const locale of ['vi', 'en']) {
    for (const [width, height, label] of [[1440, 900, 'desktop'], [390, 844, 'mobile']]) {
      console.log(`\n--- Admin Document Operations ${label} (${width}x${height}) [${locale}] ---`)
      const context = await browser.newContext({ viewport: { width, height } })
      await context.addCookies([
        { name: 'deeppace_token', value: adminAuth.token, domain: 'localhost', path: '/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminAuth.user)), domain: 'localhost', path: '/' },
        { name: 'techdaily_token', value: adminAuth.token, domain: 'localhost', path: '/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminAuth.user)), domain: 'localhost', path: '/' },
        { name: 'i18n_redirected', value: locale, domain: 'localhost', path: '/' }
      ])
      const page = await context.newPage()
      await page.addInitScript(({ token, user }) => {
        localStorage.setItem('deeppace_token', token)
        localStorage.setItem('deeppace_user', JSON.stringify(user))
        localStorage.setItem('techdaily_token', token)
        localStorage.setItem('techdaily_user', JSON.stringify(user))
      }, { token: adminAuth.token, user: adminAuth.user })

      await page.goto('http://localhost:3000/admin/documents', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1000)

      // Switch to Catalog scope if present
      const catalogScopeBtn = page.locator('button', { hasText: 'Catalog' }).first()
      if (await catalogScopeBtn.isVisible()) {
        await catalogScopeBtn.click()
        await page.waitForTimeout(800)
      }

      // Click on a document row to see detail panel
      const docRow = page.locator('button.w-full.text-left').first()
      if (await docRow.isVisible()) {
        await docRow.click()
        await page.waitForTimeout(800)
      }

      const adminShot = path.join(screenshotsDir, `admin-documents-${label}-${locale}.png`)
      await page.screenshot({ path: adminShot })
      console.log('📸 Saved:', adminShot)

      await context.close()
    }
  }

  console.log('\n✨ All visual verification screenshots captured successfully!')
} finally {
  await browser.close()
}
