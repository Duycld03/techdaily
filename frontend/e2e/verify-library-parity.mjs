import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.join(__dirname, 'screenshots')
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

const envPath = path.resolve(__dirname, '../../.env')
const envContent = fs.readFileSync(envPath, 'utf8')
const email = envContent.match(/E2E_PROD_EMAIL=(.+)/)?.[1]?.trim()
const password = envContent.match(/E2E_PROD_PASSWORD=(.+)/)?.[1]?.trim()

console.log('🔑 Authenticating E2E user:', email)

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

try {
  // Phase 1: Desktop Viewport (1440x900)
  console.log('\n🖥️  --- Phase 1: Desktop Viewport (1440x900) ---')
  const desktopContext = await browser.newContext({
    viewport: { width: 1440, height: 900 }
  })

  await desktopContext.addCookies([
    { name: 'deeppace_token', value: token, domain: 'localhost', path: '/' },
    { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
    { name: 'deeppace_token', value: token, domain: 'localhost', path: '/' },
    { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' }
  ])

  const page = await desktopContext.newPage()
  await page.addInitScript(({ token, user }) => {
    localStorage.setItem('deeppace_token', token)
    localStorage.setItem('deeppace_user', JSON.stringify(user))
    localStorage.setItem('deeppace_token', token)
    localStorage.setItem('deeppace_user', JSON.stringify(user))
  }, { token, user })

  // 1. My Library Tab
  console.log('Navigating to /library (My Library)...')
  await page.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
  await page.waitForSelector('[data-testid="library-category-chips"]')
  await page.waitForTimeout(1000)
  const mineShot = path.join(screenshotsDir, 'library-mine-desktop.png')
  await page.screenshot({ path: mineShot })
  console.log('📸 Captured:', mineShot)

  // 2. Discover Tab
  console.log('Navigating to /library?tab=discover...')
  await page.goto('http://localhost:3000/library?tab=discover', { waitUntil: 'networkidle' })
  await page.waitForSelector('[data-testid="catalog-advanced-filter-btn"]')
  await page.waitForTimeout(1000)
  const discoverShot = path.join(screenshotsDir, 'library-discover-desktop.png')
  await page.screenshot({ path: discoverShot })
  console.log('📸 Captured:', discoverShot)

  // 3. Open My Library Filter Modal
  console.log('Opening Library Filter Modal...')
  await page.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
  await page.waitForSelector('[data-testid="library-advanced-filter-btn"]')
  await page.click('[data-testid="library-advanced-filter-btn"]')
  await page.waitForSelector('[role="dialog"]')
  await page.waitForTimeout(500)
  const modalShot = path.join(screenshotsDir, 'library-filter-modal-desktop.png')
  await page.screenshot({ path: modalShot })
  console.log('📸 Captured:', modalShot)

  // Phase 2: Mobile Viewport (390x844)
  console.log('\n📱 --- Phase 2: Mobile Viewport (390x844) ---')
  const mobileContext = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true
  })
  await mobileContext.addCookies([
    { name: 'deeppace_token', value: token, domain: 'localhost', path: '/' },
    { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
    { name: 'deeppace_token', value: token, domain: 'localhost', path: '/' },
    { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' }
  ])

  const mobilePage = await mobileContext.newPage()
  await mobilePage.addInitScript(({ token, user }) => {
    localStorage.setItem('deeppace_token', token)
    localStorage.setItem('deeppace_user', JSON.stringify(user))
    localStorage.setItem('deeppace_token', token)
    localStorage.setItem('deeppace_user', JSON.stringify(user))
  }, { token, user })

  await mobilePage.goto('http://localhost:3000/library', { waitUntil: 'networkidle' })
  await mobilePage.waitForSelector('[data-testid="library-category-chips"]')
  await mobilePage.waitForTimeout(1000)
  const mobileShot = path.join(screenshotsDir, 'library-mine-mobile.png')
  await mobilePage.screenshot({ path: mobileShot })
  console.log('📸 Captured:', mobileShot)

  console.log('\n🎉 All visual verification screenshots captured successfully!')
} finally {
  await browser.close()
}
