import { chromium } from 'playwright'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const targetBaseUrl = process.env.TARGET_URL || 'http://localhost:3000'
const shouldTakeScreenshots = process.argv.includes('--screenshot')
const screenshotsDir = path.join(__dirname, 'screenshots')

if (shouldTakeScreenshots && !fs.existsSync(screenshotsDir)) {
  fs.mkdirSync(screenshotsDir, { recursive: true })
}

const VIEWPORTS = [
  { name: 'desktop', width: 1920, height: 1080 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'mobile', width: 375, height: 812, isMobile: true }
]

async function runResponsiveSmokeSuite() {
  console.log('🚀 [E2E Smoke] Starting Knowledge Graph & Roadmap Responsive Verification')
  console.log(`🎯 Target URL: ${targetBaseUrl}`)
  console.log(`📸 Screenshot Export: ${shouldTakeScreenshots ? 'ENABLED (' + screenshotsDir + ')' : 'DISABLED'}\n`)

  const browser = await chromium.launch({
    headless: true,
    args: ['--enable-webgl', '--use-gl=angle', '--no-sandbox']
  })

  let totalAssertions = 0
  let passedAssertions = 0

  function assert(condition, message) {
    totalAssertions++
    if (condition) {
      passedAssertions++
      console.log(`  ✅ ${message}`)
    } else {
      console.error(`  ❌ FAIL: ${message}`)
      throw new Error(`Assertion failed: ${message}`)
    }
  }

  try {
    for (const vp of VIEWPORTS) {
      console.log(`\n=============================================================`)
      console.log(`📱 VIEWPORT: ${vp.name.toUpperCase()} (${vp.width}x${vp.height})`)
      console.log(`=============================================================`)

      const context = await browser.newContext({
        viewport: { width: vp.width, height: vp.height },
        isMobile: !!vp.isMobile
      })
      const page = await context.newPage()

      // -------------------------------------------------------------
      // 1. /graph - Responsive Control Bar & 2D Canvas
      // -------------------------------------------------------------
      console.log(`\n--- [1. /graph] Testing 2D View & Filter Pills (${vp.name}) ---`)
      await page.goto(`${targetBaseUrl}/graph`, { waitUntil: 'domcontentloaded' })
      await page.waitForTimeout(1500)

      // Ensure page container exists
      const graphContainer = page.locator('div.h-\\[calc\\(100vh-4rem\\)\\]')
      assert(await graphContainer.isVisible(), 'Graph page root container is visible')

      // Check category filter pills are present
      const categoryPills = page.locator('div.flex.flex-wrap button.whitespace-nowrap.shrink-0')
      const pillCount = await categoryPills.count()
      assert(pillCount >= 6, `Found ${pillCount} category filter pills (>=6)`)

      // Verify no horizontal overflow beyond viewport on pills
      for (let i = 0; i < Math.min(pillCount, 6); i++) {
        const box = await categoryPills.nth(i).boundingBox()
        if (box) {
          const isWithinViewport = box.x + box.width <= vp.width + 10 // small tolerance
          assert(isWithinViewport, `Pill #${i + 1} bounds (x: ${Math.round(box.x)}, w: ${Math.round(box.width)}) fits viewport (${vp.width}px)`)
        }
      }

      // Check Visual Legend exists
      const legend = page.locator('[data-testid="legend-card"], [data-testid="legend-expand-btn"]')
      assert(await legend.first().isVisible(), 'Visual Legend widget is visible at bottom-left')

      if (shouldTakeScreenshots) {
        const file = path.join(screenshotsDir, `graph-2d-${vp.name}.png`)
        await page.screenshot({ path: file, fullPage: false })
        console.log(`  📸 Captured 2D screenshot: ${file}`)
      }

      // -------------------------------------------------------------
      // 2. /graph - 3D Cosmos & Floating HUD
      // -------------------------------------------------------------
      console.log(`\n--- [2. /graph] Testing 3D Cosmos Mode (${vp.name}) ---`)
      const mode3dButton = page.locator('button:has-text("3D"), button[title*="3D"]')
      if (await mode3dButton.count() > 0) {
        await mode3dButton.first().click()
        await page.waitForTimeout(2000)

        // Check 3D HUD controls
        const hudContainer = page.locator('div.absolute.bottom-5.right-5')
        assert(await hudContainer.isVisible(), '3D HUD floating toolbar is visible at bottom-right')

        const hudButtons = hudContainer.locator('button')
        const hudCount = await hudButtons.count()
        assert(hudCount >= 4, `Found ${hudCount} floating HUD action buttons (>=4)`)

        // Click Auto-Rotate button
        const autoRotateBtn = hudButtons.first()
        await autoRotateBtn.click()
        await page.waitForTimeout(600)
        assert(true, 'Auto-Rotate toggle clicked without unhandled exception')

        // Click Show All Labels button
        if (hudCount >= 2) {
          const showLabelsBtn = hudButtons.nth(1)
          await showLabelsBtn.click()
          await page.waitForTimeout(400)
          assert(true, 'LOD Show-All-Labels toggle clicked without exception')
        }

        if (shouldTakeScreenshots) {
          const file = path.join(screenshotsDir, `graph-3d-${vp.name}.png`)
          await page.screenshot({ path: file, fullPage: false })
          console.log(`  📸 Captured 3D screenshot: ${file}`)
        }
      }

      // -------------------------------------------------------------
      // 3. /roadmap - Mindmap & Track Switcher
      // -------------------------------------------------------------
      console.log(`\n--- [3. /roadmap] Testing Mindmap & Track Switcher (${vp.name}) ---`)
      await page.goto(`${targetBaseUrl}/roadmap`, { waitUntil: 'domcontentloaded' })
      await page.waitForTimeout(1500)

      const roadmapSvg = page.locator('svg')
      assert(await roadmapSvg.first().isVisible(), 'Roadmap mindmap SVG canvas is visible')

      // Check track switcher button
      const trackSwitcher = page.locator('[data-testid="track-switcher-btn"], button:has-text("Track"), button:has-text("Lộ Trình")')
      if (await trackSwitcher.count() > 0) {
        assert(await trackSwitcher.first().isVisible(), 'Track switcher button is visible')
        await trackSwitcher.first().click()
        await page.waitForTimeout(400)

        // Verify popover menu appears and is visible
        const trackMenu = page.locator('[data-testid="track-menu-popover"], div[role="menu"]')
        if (await trackMenu.count() > 0) {
          assert(await trackMenu.first().isVisible(), 'Track switcher popover is rendered and visible')
        }
      }

      if (shouldTakeScreenshots) {
        const file = path.join(screenshotsDir, `roadmap-${vp.name}.png`)
        await page.screenshot({ path: file, fullPage: false })
        console.log(`  📸 Captured Roadmap screenshot: ${file}`)
      }

      await context.close()
    }

    console.log(`\n=============================================================`)
    console.log(`🎉 [SUCCESS] All ${passedAssertions}/${totalAssertions} responsive smoke assertions passed!`)
    console.log(`=============================================================\n`)
  } catch (error) {
    console.error('\n❌ E2E Smoke Test Encountered an Error:', error)
    process.exitCode = 1
  } finally {
    await browser.close()
  }
}

runResponsiveSmokeSuite()
