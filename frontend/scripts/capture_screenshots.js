import { chromium } from 'playwright'
import path from 'path'
import fs from 'fs'

const screenshotDir = '/home/duycld03/.gemini/antigravity/brain/8bc582b3-6eb5-46f5-9f36-d744098549ff/screenshots'
if (!fs.existsSync(screenshotDir)) {
  fs.mkdirSync(screenshotDir, { recursive: true })
}

async function captureAll(theme) {
  const browser = await chromium.launch({ headless: true })
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  })
  const page = await context.newPage()

  // Set color mode in localStorage
  await page.addInitScript((mode) => {
    localStorage.setItem('nuxt-color-mode', mode)
  }, theme)

  console.log('Capturing for theme:', theme)

  // 1. /today
  await page.goto('http://localhost:3000/today', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `01_today_${theme}.png`), fullPage: true })
  console.log(`Saved 01_today_${theme}.png`)

  // 2. /today?day=2
  await page.goto('http://localhost:3000/today?day=2', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `02_today_day2_${theme}.png`), fullPage: true })
  console.log(`Saved 02_today_day2_${theme}.png`)

  // 3. /library
  await page.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `03_library_${theme}.png`), fullPage: true })
  console.log(`Saved 03_library_${theme}.png`)

  // 4. /read/:bookId
  await page.goto('http://localhost:3000/read/10000000-0000-0000-0000-000000000001', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `04_reader_${theme}.png`), fullPage: true })
  console.log(`Saved 04_reader_${theme}.png`)

  // 5. /login
  await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `05_login_${theme}.png`), fullPage: true })
  console.log(`Saved 05_login_${theme}.png`)

  // Login with real user
  await page.fill('input[type="email"]', 'duy@techdaily.local')
  await page.fill('input[type="password"]', 'Password123!')
  await page.click('button[type="submit"]')
  await page.waitForURL('**/today', { timeout: 8000 })
  await page.waitForTimeout(1000)

  // 6. /profile
  await page.goto('http://localhost:3000/profile', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `06_profile_${theme}.png`), fullPage: true })
  console.log(`Saved 06_profile_${theme}.png`)

  // 7. /notes
  await page.goto('http://localhost:3000/notes', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `07_notes_${theme}.png`), fullPage: true })
  console.log(`Saved 07_notes_${theme}.png`)

  // 8. /review
  await page.goto('http://localhost:3000/review', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `08_review_${theme}.png`), fullPage: true })
  console.log(`Saved 08_review_${theme}.png`)

  // 9. /settings
  await page.goto('http://localhost:3000/settings', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1000)
  await page.screenshot({ path: path.join(screenshotDir, `09_settings_${theme}.png`), fullPage: true })
  console.log(`Saved 09_settings_${theme}.png`)

  await browser.close()
}

async function main() {
  await captureAll('light')
  await captureAll('dark')
  console.log('ALL SCREENSHOTS COMPLETED!')
}

main().catch(console.error)
