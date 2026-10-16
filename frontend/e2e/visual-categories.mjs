import { chromium } from 'playwright'
import http from 'node:http'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.resolve(__dirname, '../../docs/screenshots/categories')
if (!fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

const adminUser = {
  id: '10000000-0000-4000-8000-000000000002',
  email: 'alice.admin@example.com',
  name: 'Alice Administrator',
  avatarUrl: null,
  status: 'Active',
  createdAt: '2026-10-15T00:00:00Z',
  preferredLocale: 'en',
  adminControlVersion: 1,
  hasPassword: true,
  hasGoogleAuth: false,
  suspendedByUserId: null,
  suspendedAt: null,
  suspensionReason: null,
  role: {
    id: '20000000-0000-4000-8000-000000000002',
    key: 'admin',
    nameEn: 'Administrator',
    nameVi: 'Quản trị viên',
    isProtected: true
  }
}

const mockCategories = [
  {
    id: '10000000-0000-4000-8000-000000000000',
    slug: 'frontend-web',
    nameEn: 'Frontend & Web',
    nameVi: 'Frontend & Web',
    sortOrder: 0,
    isActive: true,
    version: 1
  },
  {
    id: '10000000-0000-4000-8000-000000000001',
    slug: 'backend-runtime',
    nameEn: 'Backend & Runtime',
    nameVi: 'Backend & Runtime',
    sortOrder: 1,
    isActive: true,
    version: 2
  },
  {
    id: '10000000-0000-4000-8000-000000000002',
    slug: 'database-storage',
    nameEn: 'Database & Storage',
    nameVi: 'Cơ Sở Dữ Liệu & Lưu Trữ',
    sortOrder: 2,
    isActive: false,
    version: 1
  },
  {
    id: '10000000-0000-4000-8000-000000000003',
    slug: 'system-design',
    nameEn: 'Distributed Systems',
    nameVi: 'Hệ Thống Phân Tán',
    sortOrder: 3,
    isActive: true,
    version: 1
  },
  {
    id: '10000000-0000-4000-8000-000000000004',
    slug: 'clean-architecture-enterprise-software-engineering',
    nameEn: 'Clean Architecture & Domain-Driven Design for Resilient Enterprise Software Engineering',
    nameVi: 'Kiến Trúc Sạch & Thiết Kế Hướng Miền Cho Kỹ Thuật Phần Mềm Doanh Nghiệp Bền Vững',
    sortOrder: 4,
    isActive: true,
    version: 1
  }
]

const server = http.createServer((req, res) => {
  const origin = req.headers.origin || 'http://localhost:3333'
  res.setHeader('Access-Control-Allow-Origin', origin)
  res.setHeader('Access-Control-Allow-Credentials', 'true')
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, PATCH, DELETE, OPTIONS')
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization, Accept, X-Requested-With')

  if (req.method === 'OPTIONS') {
    res.writeHead(204)
    res.end()
    return
  }

  const url = new URL(req.url, 'http://localhost:5000')

  if (url.pathname === '/api/v1/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify({ status: 'Healthy' }))
    return
  }

  if (url.pathname === '/api/v1/admin/capabilities') {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify({
      canViewAdminDashboard: true,
      canManageCatalog: true,
      canManageAccounts: true,
      canReadAudit: true,
      canManageDocumentOperations: true,
      canManageRoles: true,
      canManageCategories: true
    }))
    return
  }

  if (url.pathname === '/api/v1/categories' || url.pathname === '/api/v1/admin/categories') {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify(mockCategories))
    return
  }

  if (url.pathname.startsWith('/api/v1/admin/categories/')) {
    const id = url.pathname.replace('/api/v1/admin/categories/', '')
    const cat = mockCategories.find(c => c.id === id) || mockCategories[0]
    const canDelete = id === '10000000-0000-4000-8000-000000000000' || id === '10000000-0000-4000-8000-000000000004'
    const usageCount = canDelete ? 0 : 8

    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify({
      ...cat,
      canDelete,
      usageCount
    }))
    return
  }

  res.writeHead(200, { 'Content-Type': 'application/json' })
  res.end(JSON.stringify({}))
})

server.listen(5000)

const claims = {
  sub: adminUser.id,
  email: adminUser.email,
  name: adminUser.name,
  exp: Math.floor(Date.now() / 1000) + 86400 * 30
}
const b64 = Buffer.from(JSON.stringify(claims)).toString('base64')
const token = `header.${b64}.sig`

const chromePath = '/home/duycld03/.omp/puppeteer/chrome/linux-150.0.7871.24/chrome-linux64/chrome'
const launchOptions = fs.existsSync(chromePath)
  ? { headless: true, executablePath: chromePath, args: ['--no-sandbox', '--disable-setuid-sandbox'] }
  : { headless: true, args: ['--no-sandbox', '--disable-setuid-sandbox'] }

async function main() {
  const browser = await chromium.launch(launchOptions)

  try {
    // 1. Desktop 1440x900 English
    {
      const context = await browser.newContext({
        viewport: { width: 1440, height: 900 }
      })
      await context.addCookies([
        { name: 'deeppace_token', value: token, url: 'http://localhost:3333/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'techdaily_token', value: token, url: 'http://localhost:3333/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'i18n_redirected', value: 'en', url: 'http://localhost:3333/' }
      ])

      const page = await context.newPage()
      console.log('Navigating to http://localhost:3333/admin/categories (Desktop EN)...')
      await page.goto('http://localhost:3333/admin/categories', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      // Ensure dark mode
      await page.evaluate(() => {
        document.documentElement.classList.add('dark')
      })
      await page.waitForTimeout(300)

      // Verify category items visible
      const categoryItem = page.locator('text=Frontend & Web').first()
      await categoryItem.waitFor({ state: 'visible', timeout: 8000 })

      // Capture desktop categories view
      const desktopEnPath = path.join(screenshotsDir, 'desktop-en-categories.png')
      await page.screenshot({ path: desktopEnPath, fullPage: false })
      console.log('Saved:', desktopEnPath)

      // Open "New Category" modal
      const newCategoryBtn = page.locator('button:has-text("New Category"), button:has-text("Thêm danh mục mới")').first()
      await newCategoryBtn.waitFor({ state: 'visible', timeout: 5000 })
      await newCategoryBtn.click()
      await page.waitForTimeout(500)

      // Fill in demo data
      await page.fill('#category-slug-input', 'cloud-infrastructure')
      await page.fill('#category-name-en-input', 'Cloud & Platform Engineering')
      await page.fill('#category-name-vi-input', 'Kỹ Thuật Đám Mây & Nền Tảng')
      await page.fill('#category-sort-order-input', '5')
      await page.waitForTimeout(300)

      // Capture modal screenshot
      const modalEnPath = path.join(screenshotsDir, 'desktop-en-category-modal.png')
      await page.screenshot({ path: modalEnPath, fullPage: false })
      console.log('Saved:', modalEnPath)

      await context.close()
    }

    // 2. Desktop 1440x900 Vietnamese
    {
      const context = await browser.newContext({
        viewport: { width: 1440, height: 900 }
      })
      await context.addCookies([
        { name: 'deeppace_token', value: token, url: 'http://localhost:3333/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'techdaily_token', value: token, url: 'http://localhost:3333/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'i18n_redirected', value: 'vi', url: 'http://localhost:3333/' }
      ])

      const page = await context.newPage()
      console.log('Navigating to http://localhost:3333/admin/categories (Desktop VI)...')
      await page.goto('http://localhost:3333/admin/categories', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      // Switch to VI if needed via locale button
      const viButton = page.locator('button:has-text("VI"), button:has-text("vi")').first()
      if (await viButton.isVisible().catch(() => false)) {
        await viButton.click()
        await page.waitForTimeout(800)
      }

      await page.evaluate(() => {
        document.documentElement.classList.add('dark')
      })
      await page.waitForTimeout(300)

      // Click on long category name
      const longCategoryItem = page.locator('text=Clean Architecture').first()
      if (await longCategoryItem.isVisible().catch(() => false)) {
        await longCategoryItem.click()
        await page.waitForTimeout(400)
      }

      const desktopViPath = path.join(screenshotsDir, 'desktop-vi-categories.png')
      await page.screenshot({ path: desktopViPath, fullPage: false })
      console.log('Saved:', desktopViPath)

      // Open Edit Category Modal
      const editBtn = page.locator('button:has-text("Edit"), button:has-text("Chỉnh sửa")').first()
      if (await editBtn.isVisible().catch(() => false)) {
        await editBtn.click()
        await page.waitForTimeout(500)

        const modalViPath = path.join(screenshotsDir, 'desktop-vi-category-modal.png')
        await page.screenshot({ path: modalViPath, fullPage: false })
        console.log('Saved:', modalViPath)
      }

      await context.close()
    }

    // 3. Mobile 390x844 English
    {
      const context = await browser.newContext({
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true
      })
      await context.addCookies([
        { name: 'deeppace_token', value: token, url: 'http://localhost:3333/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'techdaily_token', value: token, url: 'http://localhost:3333/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'i18n_redirected', value: 'en', url: 'http://localhost:3333/' }
      ])

      const page = await context.newPage()
      console.log('Navigating to http://localhost:3333/admin/categories (Mobile EN)...')
      await page.goto('http://localhost:3333/admin/categories', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      await page.evaluate(() => {
        document.documentElement.classList.add('dark')
      })
      await page.waitForTimeout(300)

      // Verify zero horizontal overflow on mobile
      const hasHorizontalOverflow = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth
      })
      console.log('Mobile EN 390px horizontal overflow:', hasHorizontalOverflow ? 'FAIL' : 'PASS (no overflow)')

      const mobileEnPath = path.join(screenshotsDir, 'mobile-en-categories.png')
      await page.screenshot({ path: mobileEnPath, fullPage: false })
      console.log('Saved:', mobileEnPath)

      await context.close()
    }

    // 4. Mobile 390x844 Vietnamese
    {
      const context = await browser.newContext({
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true
      })
      await context.addCookies([
        { name: 'deeppace_token', value: token, url: 'http://localhost:3333/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'techdaily_token', value: token, url: 'http://localhost:3333/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'i18n_redirected', value: 'vi', url: 'http://localhost:3333/' }
      ])

      const page = await context.newPage()
      console.log('Navigating to http://localhost:3333/admin/categories (Mobile VI)...')
      await page.goto('http://localhost:3333/admin/categories', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      const viButton = page.locator('button:has-text("VI"), button:has-text("vi")').first()
      if (await viButton.isVisible().catch(() => false)) {
        await viButton.click()
        await page.waitForTimeout(800)
      }

      await page.evaluate(() => {
        document.documentElement.classList.add('dark')
      })
      await page.waitForTimeout(300)

      const hasHorizontalOverflow = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth
      })
      console.log('Mobile VI 390px horizontal overflow:', hasHorizontalOverflow ? 'FAIL' : 'PASS (no overflow)')

      const mobileViPath = path.join(screenshotsDir, 'mobile-vi-categories.png')
      await page.screenshot({ path: mobileViPath, fullPage: false })
      console.log('Saved:', mobileViPath)

      await context.close()
    }

    console.log('🎉 All visual verification screenshots captured successfully!')
  } finally {
    await browser.close()
    server.close()
  }
}

main().catch(err => {
  console.error('Error during visual verification:', err)
  server.close()
  process.exit(1)
})
