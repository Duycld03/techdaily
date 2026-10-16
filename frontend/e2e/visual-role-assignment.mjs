import { chromium } from 'playwright'
import http from 'node:http'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const screenshotsDir = path.resolve(__dirname, '../../docs/screenshots/role-assignment')
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

const learnerUser = {
  id: '10000000-0000-4000-8000-000000000001',
  email: 'bob.learner@example.com',
  name: 'Bob Learner',
  avatarUrl: null,
  status: 'Active',
  createdAt: '2026-10-15T00:00:00Z',
  preferredLocale: 'en',
  adminControlVersion: 2,
  hasPassword: true,
  hasGoogleAuth: false,
  suspendedByUserId: null,
  suspendedAt: null,
  suspensionReason: null,
  role: {
    id: '20000000-0000-4000-8000-000000000001',
    key: 'learner',
    nameEn: 'Learner',
    nameVi: 'Học viên',
    isProtected: false
  }
}

const rolesList = [
  {
    id: '20000000-0000-4000-8000-000000000001',
    key: 'learner',
    nameEn: 'Learner',
    nameVi: 'Học viên',
    permissionKeys: []
  },
  {
    id: '20000000-0000-4000-8000-000000000002',
    key: 'admin',
    nameEn: 'Administrator',
    nameVi: 'Quản trị viên',
    permissionKeys: [
      'ViewAdminDashboard',
      'ManageCatalog',
      'ManageAccounts',
      'ReadAudit',
      'ManageDocumentOperations',
      'ManageRoles',
      'ManageCategories'
    ]
  }
]

const server = http.createServer((req, res) => {
  const origin = req.headers.origin || 'http://localhost:3333'
  res.setHeader('Access-Control-Allow-Origin', origin)
  res.setHeader('Access-Control-Allow-Credentials', 'true')
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS')
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
      canManageRoles: true
    }))
    return
  }

  if (url.pathname === '/api/v1/admin/roles') {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify({
      items: rolesList,
      totalCount: rolesList.length,
      page: 1,
      pageSize: 100
    }))
    return
  }

  if (url.pathname === '/api/v1/admin/users') {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify({
      items: [learnerUser, adminUser],
      totalCount: 2,
      page: 1,
      pageSize: 20
    }))
    return
  }

  if (url.pathname === `/api/v1/admin/users/${learnerUser.id}`) {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify(learnerUser))
    return
  }

  if (url.pathname === `/api/v1/admin/users/${adminUser.id}`) {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify(adminUser))
    return
  }

  if (url.pathname.endsWith('/role') && req.method === 'PUT') {
    res.writeHead(200, { 'Content-Type': 'application/json' })
    res.end(JSON.stringify({
      ...learnerUser,
      adminControlVersion: learnerUser.adminControlVersion + 1,
      role: rolesList[1]
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

async function main() {
  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  })

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
      console.log('Navigating to http://localhost:3333/admin/users (Desktop EN)...')
      await page.goto('http://localhost:3333/admin/users', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      // Click Bob Learner
      const userItem = page.locator('text=Bob Learner').first()
      await userItem.waitFor({ state: 'visible', timeout: 5000 })
      await userItem.click()
      await page.waitForTimeout(500)

      // Capture desktop users table
      const tablePath = path.join(screenshotsDir, 'desktop-users-table.png')
      await page.screenshot({ path: tablePath, fullPage: false })
      console.log('Saved:', tablePath)

      // Open Change Role Modal
      const changeRoleBtn = page.locator('[data-testid="change-role-btn"]')
      await changeRoleBtn.waitFor({ state: 'visible', timeout: 5000 })
      await changeRoleBtn.click()
      await page.waitForTimeout(500)

      // Select Admin role
      const selectTrigger = page.locator('[data-testid="admin-role-select"] [data-testid="app-select-trigger"]')
      await selectTrigger.click()
      await page.waitForTimeout(400)

      const adminOption = page.locator('[data-testid="app-select-option-20000000-0000-4000-8000-000000000002"]')
      await adminOption.click()
      await page.waitForTimeout(400)

      // Fill reason
      const reasonInput = page.locator('[data-testid="admin-role-reason"]')
      await reasonInput.fill('Promoting Bob Learner to Administrator for system operations.')
      await page.waitForTimeout(400)

      const modalEnPath = path.join(screenshotsDir, 'desktop-en-role-modal.png')
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
      console.log('Navigating to http://localhost:3333/admin/users (Desktop VI)...')
      await page.goto('http://localhost:3333/admin/users', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      const viButton = page.locator('button:has-text("VI"), button:has-text("vi")').first()
      if (await viButton.isVisible()) {
        await viButton.click()
        await page.waitForTimeout(500)
      }

      const userItem = page.locator('text=Bob Learner').first()
      await userItem.waitFor({ state: 'visible', timeout: 5000 })
      await userItem.click()
      await page.waitForTimeout(500)

      const changeRoleBtn = page.locator('[data-testid="change-role-btn"]')
      await changeRoleBtn.waitFor({ state: 'visible', timeout: 5000 })
      await changeRoleBtn.click()
      await page.waitForTimeout(500)

      const selectTrigger = page.locator('[data-testid="admin-role-select"] [data-testid="app-select-trigger"]')
      await selectTrigger.click()
      await page.waitForTimeout(400)

      const adminOption = page.locator('[data-testid="app-select-option-20000000-0000-4000-8000-000000000002"]')
      await adminOption.click()
      await page.waitForTimeout(400)

      const reasonInput = page.locator('[data-testid="admin-role-reason"]')
      await reasonInput.fill('Thăng cấp Bob Learner lên Quản trị viên để quản lý hệ thống kỹ thuật.')
      await page.waitForTimeout(400)

      const modalViPath = path.join(screenshotsDir, 'desktop-vi-role-modal.png')
      await page.screenshot({ path: modalViPath, fullPage: false })
      console.log('Saved:', modalViPath)

      await context.close()
    }

    // 3. Mobile 390x844 English
    {
      const context = await browser.newContext({
        viewport: { width: 390, height: 844 },
        isMobile: true
      })
      await context.addCookies([
        { name: 'deeppace_token', value: token, url: 'http://localhost:3333/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'techdaily_token', value: token, url: 'http://localhost:3333/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'i18n_redirected', value: 'en', url: 'http://localhost:3333/' }
      ])

      const page = await context.newPage()
      console.log('Navigating to http://localhost:3333/admin/users (Mobile EN)...')
      await page.goto('http://localhost:3333/admin/users', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      const userItem = page.locator('text=Bob Learner').first()
      await userItem.waitFor({ state: 'visible', timeout: 5000 })
      await userItem.click()
      await page.waitForTimeout(600)

      const changeRoleBtn = page.locator('[data-testid="change-role-btn"]')
      await changeRoleBtn.waitFor({ state: 'visible', timeout: 5000 })
      await changeRoleBtn.click()
      await page.waitForTimeout(500)

      const selectTrigger = page.locator('[data-testid="admin-role-select"] [data-testid="app-select-trigger"]')
      await selectTrigger.click()
      await page.waitForTimeout(400)

      const adminOption = page.locator('[data-testid="app-select-option-20000000-0000-4000-8000-000000000002"]')
      await adminOption.click()
      await page.waitForTimeout(400)

      const reasonInput = page.locator('[data-testid="admin-role-reason"]')
      await reasonInput.fill('Promoting Bob Learner on mobile viewport.')
      await page.waitForTimeout(400)

      const mobileEnPath = path.join(screenshotsDir, 'mobile-en-role-modal.png')
      await page.screenshot({ path: mobileEnPath, fullPage: false })
      console.log('Saved:', mobileEnPath)

      await context.close()
    }

    // 4. Mobile 390x844 Vietnamese
    {
      const context = await browser.newContext({
        viewport: { width: 390, height: 844 },
        isMobile: true
      })
      await context.addCookies([
        { name: 'deeppace_token', value: token, url: 'http://localhost:3333/' },
        { name: 'deeppace_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'techdaily_token', value: token, url: 'http://localhost:3333/' },
        { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(adminUser)), url: 'http://localhost:3333/' },
        { name: 'i18n_redirected', value: 'vi', url: 'http://localhost:3333/' }
      ])

      const page = await context.newPage()
      console.log('Navigating to http://localhost:3333/admin/users (Mobile VI)...')
      await page.goto('http://localhost:3333/admin/users', { waitUntil: 'networkidle' })
      await page.waitForTimeout(1500)

      const viButton = page.locator('button:has-text("VI"), button:has-text("vi")').first()
      if (await viButton.isVisible()) {
        await viButton.click()
        await page.waitForTimeout(500)
      }

      const userItem = page.locator('text=Bob Learner').first()
      await userItem.waitFor({ state: 'visible', timeout: 5000 })
      await userItem.click()
      await page.waitForTimeout(600)

      const changeRoleBtn = page.locator('[data-testid="change-role-btn"]')
      await changeRoleBtn.waitFor({ state: 'visible', timeout: 5000 })
      await changeRoleBtn.click()
      await page.waitForTimeout(500)

      const selectTrigger = page.locator('[data-testid="admin-role-select"] [data-testid="app-select-trigger"]')
      await selectTrigger.click()
      await page.waitForTimeout(400)

      const adminOption = page.locator('[data-testid="app-select-option-20000000-0000-4000-8000-000000000002"]')
      await adminOption.click()
      await page.waitForTimeout(400)

      const reasonInput = page.locator('[data-testid="admin-role-reason"]')
      await reasonInput.fill('Phân quyền quản trị viên trên di động.')
      await page.waitForTimeout(400)

      const mobileViPath = path.join(screenshotsDir, 'mobile-vi-role-modal.png')
      await page.screenshot({ path: mobileViPath, fullPage: false })
      console.log('Saved:', mobileViPath)

      await context.close()
    }

    console.log('🎉 All 5 visual verification screenshots captured successfully!')
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
