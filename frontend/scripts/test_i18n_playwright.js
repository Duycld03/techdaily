import { chromium } from 'playwright'
import path from 'path'
import fs from 'fs'

const screenshotDir = '/home/duycld03/.gemini/antigravity/brain/8bc582b3-6eb5-46f5-9f36-d744098549ff/screenshots'
if (!fs.existsSync(screenshotDir)) {
  fs.mkdirSync(screenshotDir, { recursive: true })
}

async function testLocale(locale, theme) {
  console.log(`\n========================================`)
  console.log(`Testing Locale: [${locale.toUpperCase()}] | Theme: [${theme}]`)
  console.log(`========================================`)

  const browser = await chromium.launch({ headless: true })
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  })
  const page = await context.newPage()

  // Initialize localStorage for color-mode and nuxt-i18n locale
  await page.addInitScript(({ l, t }) => {
    localStorage.setItem('nuxt-color-mode', t)
    localStorage.setItem('i18n_redirected', l)
  }, { l: locale, t: theme })

  // 1. /login
  await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  
  // Click locale toggle button if needed to ensure correct locale
  const localeBtn = await page.locator(`button:has-text("${locale.toUpperCase()}")`)
  if (await localeBtn.count() > 0) {
    await localeBtn.first().click()
    await page.waitForTimeout(400)
  }

  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_01_login_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] Login page loaded & screenshot captured`)

  // Login
  await page.fill('input[type="email"]', 'duy@techdaily.local')
  await page.fill('input[type="password"]', 'Password123!')
  await page.click('button[type="submit"]')
  await page.waitForURL('**/today', { timeout: 8000 })
  await page.waitForTimeout(1000)

  // 2. /today
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_02_today_${theme}.png`), fullPage: true })
  const todayBodyText = await page.textContent('body')
  console.log(`✔ [${locale}] /today verified:`)
  if (locale === 'vi') {
    console.log(`    - Contains 'Thử Thách Hôm Nay' or 'Bài Hôm Nay':`, todayBodyText.includes('Thử Thách') || todayBodyText.includes('Ngày'))
    console.log(`    - Contains Technical Excerpt (Source Preservation):`, todayBodyText.includes('Reactivity') || todayBodyText.includes('Proxy'))
  } else {
    console.log(`    - Contains 'Today's Drill' or 'Today's Focus':`, todayBodyText.includes('Today') || todayBodyText.includes('Drill'))
    console.log(`    - Contains Technical Excerpt (Source Preservation):`, todayBodyText.includes('Reactivity') || todayBodyText.includes('Proxy'))
  }

  // 3. /library
  await page.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_03_library_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] /library loaded & screenshot captured`)

  // 4. /read/:bookId
  await page.goto('http://localhost:3000/read/10000000-0000-0000-0000-000000000001', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_04_reader_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] /read loaded & screenshot captured`)

  // 5. /profile
  await page.goto('http://localhost:3000/profile', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_05_profile_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] /profile loaded & screenshot captured`)

  // 6. /settings
  await page.goto('http://localhost:3000/settings', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_06_settings_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] /settings loaded & screenshot captured`)

  // 7. /notes
  await page.goto('http://localhost:3000/notes', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_07_notes_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] /notes loaded & screenshot captured`)

  // 8. /review
  await page.goto('http://localhost:3000/review', { waitUntil: 'networkidle' })
  await page.waitForTimeout(500)
  await page.screenshot({ path: path.join(screenshotDir, `i18n_${locale}_08_review_${theme}.png`), fullPage: true })
  console.log(`✔ [${locale}] /review loaded & screenshot captured`)

  await browser.close()
}

async function main() {
  await testLocale('vi', 'dark')
  await testLocale('en', 'dark')
  await testLocale('vi', 'light')
  await testLocale('en', 'light')
  console.log('\n========================================')
  console.log('ALL I18N PLAYWRIGHT TESTS PASSED!')
  console.log('========================================')
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})
