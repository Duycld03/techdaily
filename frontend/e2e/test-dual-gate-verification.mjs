import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots')
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

// 1. Read .env for credentials
const envPath = path.resolve(__dirname, '../../.env')
const envContent = fs.readFileSync(envPath, 'utf8')
const email = envContent.match(/E2E_PROD_EMAIL=(.+)/)?.[1]?.trim()
const password = envContent.match(/E2E_PROD_PASSWORD=(.+)/)?.[1]?.trim()

console.log('🔑 Authenticating E2E prod user:', email)

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
let passedAssertions = 0
let failedAssertions = 0

function assert(condition, message) {
  if (condition) {
    passedAssertions++
    console.log(`  ✅ PASS: ${message}`)
  } else {
    failedAssertions++
    console.error(`  ❌ FAIL: ${message}`)
  }
}

try {
  // ==========================================
  // PHASE 1: Desktop Viewport (1440x900)
  // ==========================================
  console.log('\n🖥️  --- Phase 1: Desktop Viewport (1440x900) ---')
  const desktopContext = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  })

  // Set cookies
  await desktopContext.addCookies([
    {
      name: 'techdaily_token',
      value: token,
      domain: 'localhost',
      path: '/'
    },
    {
      name: 'techdaily_user',
      value: encodeURIComponent(JSON.stringify(user)),
      domain: 'localhost',
      path: '/'
    }
  ])

  const page = await desktopContext.newPage()

  // Set localStorage via addInitScript before loading
  await page.addInitScript(({ token, user }) => {
    localStorage.setItem('techdaily_token', token)
    localStorage.setItem('techdaily_user', JSON.stringify(user))
  }, { token, user })

  // 1. Navigate to Home
  console.log('Navigating to http://localhost:3000/ ...')
  await page.goto('http://localhost:3000/', { waitUntil: 'networkidle' })
  assert(!page.url().includes('/login'), 'Desktop home does not redirect to /login')
  assert(page.url().includes('localhost:3000/'), 'Desktop loaded home URL')

  await page.screenshot({ path: path.join(screenshotsDir, '1-desktop-home.png') })
  console.log('  📸 Screenshot saved: 1-desktop-home.png')

  // 2. Click Continue Reading button
  console.log('Clicking Continue Reading button...')
  const continueReadingBtn = page.locator('button:has-text("Đọc Tiếp"), button:has-text("Continue Reading")').first()
  await continueReadingBtn.waitFor({ state: 'visible', timeout: 8000 })
  await continueReadingBtn.click()

  // 3. Wait for Reader page to load
  await page.waitForURL(url => url.pathname.startsWith('/read/'), { timeout: 10000 })
  console.log('  Current URL after clicking Continue Reading:', page.url())
  assert(page.url().includes('/read/'), 'Successfully navigated to /read/[bookId]')
  assert(page.url().includes('from=%2F') || page.url().includes('from=/'), 'Reader URL contains from=/ parameter')

  // Wait for reader header bar back button
  const backBtn = page.locator('header button[title*="Quay Lại"], header button[title*="Back"]').first()
  await backBtn.waitFor({ state: 'visible', timeout: 10000 })

  await page.screenshot({ path: path.join(screenshotsDir, '2-desktop-reader.png') })
  console.log('  📸 Screenshot saved: 2-desktop-reader.png')

  // 4. Click Back button
  console.log('Clicking < Quay Lại back button...')
  await backBtn.click()

  // 5. Wait for return navigation
  await page.waitForURL(url => url.pathname === '/', { timeout: 10000 })
  console.log('  Current URL after back button:', page.url())
  assert(page.url() === 'http://localhost:3000/' || page.url() === 'http://localhost:3000', 'Returned cleanly to home (/)')
  assert(!page.url().includes('/login'), 'Back navigation did NOT redirect to /login')

  await page.screenshot({ path: path.join(screenshotsDir, '3-desktop-after-back.png') })
  console.log('  📸 Screenshot saved: 3-desktop-after-back.png')

  // 6. Navigate to /today
  console.log('Navigating to http://localhost:3000/today ...')
  await page.goto('http://localhost:3000/today', { waitUntil: 'networkidle' })
  assert(!page.url().includes('/login'), 'Today studio does not redirect to /login')
  assert(page.url().includes('/today'), 'Navigated to /today')

  await page.screenshot({ path: path.join(screenshotsDir, '4-desktop-today.png') })
  console.log('  📸 Screenshot saved: 4-desktop-today.png')

  await desktopContext.close()

  // ==========================================
  // PHASE 2: Mobile Viewport (390x844)
  // ==========================================
  console.log('\n📱 --- Phase 2: Mobile Viewport (390x844) ---')
  const mobileContext = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true
  })

  await mobileContext.addCookies([
    {
      name: 'techdaily_token',
      value: token,
      domain: 'localhost',
      path: '/'
    },
    {
      name: 'techdaily_user',
      value: encodeURIComponent(JSON.stringify(user)),
      domain: 'localhost',
      path: '/'
    }
  ])

  const mobilePage = await mobileContext.newPage()
  await mobilePage.addInitScript(({ token, user }) => {
    localStorage.setItem('techdaily_token', token)
    localStorage.setItem('techdaily_user', JSON.stringify(user))
  }, { token, user })

  console.log('Navigating mobile to home...')
  await mobilePage.goto('http://localhost:3000/', { waitUntil: 'networkidle' })
  assert(mobilePage.url().includes('localhost:3000/'), 'Mobile loaded home URL')
  await mobilePage.screenshot({ path: path.join(screenshotsDir, '5-mobile-home.png') })
  console.log('  📸 Screenshot saved: 5-mobile-home.png')

  console.log('Navigating mobile to /today...')
  await mobilePage.goto('http://localhost:3000/today', { waitUntil: 'networkidle' })
  assert(mobilePage.url().includes('/today'), 'Mobile loaded /today')
  await mobilePage.screenshot({ path: path.join(screenshotsDir, '6-mobile-today.png') })
  console.log('  📸 Screenshot saved: 6-mobile-today.png')

  await mobileContext.close()

  // ==========================================
  // PHASE 3: Expired Token Redirection & Zero Hydration Mismatch
  // ==========================================
  console.log('\n🔒 --- Phase 3: Expired Token Redirection ---')
  const expiredContext = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  })

  const expiredJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjoxNTE2MjM5MDIyfQ.invalid'
  await expiredContext.addCookies([
    {
      name: 'techdaily_token',
      value: expiredJwt,
      domain: 'localhost',
      path: '/'
    }
  ])

  const expiredPage = await expiredContext.newPage()
  const consoleWarnings = []
  expiredPage.on('console', msg => {
    if (msg.type() === 'warning' || msg.type() === 'error') {
      consoleWarnings.push(msg.text())
    }
  })

  console.log('Accessing protected route /today with expired token...')
  await expiredPage.goto('http://localhost:3000/today', { waitUntil: 'networkidle' })

  console.log('  Current URL after accessing /today with expired token:', expiredPage.url())
  assert(expiredPage.url().includes('/login'), 'Redirected to /login')
  assert(expiredPage.url().includes('redirect=%2Ftoday') || expiredPage.url().includes('redirect=/today'), 'Redirect preserved target /today')

  const hydrationWarnings = consoleWarnings.filter(w => w.toLowerCase().includes('hydration') || w.toLowerCase().includes('mismatch'))
  assert(hydrationWarnings.length === 0, `Zero hydration mismatch warnings on /login (found: ${hydrationWarnings.length})`)

  await expiredPage.screenshot({ path: path.join(screenshotsDir, '7-expired-token-redirect.png') })
  console.log('  📸 Screenshot saved: 7-expired-token-redirect.png')

  await expiredContext.close()

} finally {
  await browser.close()
}

console.log('\n=============================================')
console.log(`📊 SUMMARY: ${passedAssertions} passed, ${failedAssertions} failed`)
console.log('=============================================')

if (failedAssertions > 0) {
  process.exit(1)
}
