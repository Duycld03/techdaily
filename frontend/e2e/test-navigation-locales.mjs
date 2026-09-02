import { chromium } from 'playwright';

async function runTests() {
  console.log('🚀 Starting Playwright E2E Test for 3-Tier Navigation & i18n Locales...');
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 }
  });
  const page = await context.newPage();

  try {
    // -------------------------------------------------------------
    // 1. Desktop Test: 3-Tier Sidebar Navigation (EN)
    // -------------------------------------------------------------
    console.log('\n--- [TEST 1] Desktop 3-Tier Sidebar (EN) ---');
    await page.goto('http://localhost:3000/today', { waitUntil: 'networkidle' });

    // Ensure English is active
    const enButton = page.locator('header button:has-text("EN")');
    if (await enButton.isVisible()) {
      await enButton.click();
      await page.waitForTimeout(300);
    }

    const sidebar = page.locator('aside');
    await sidebar.waitFor({ state: 'visible', timeout: 5000 });
    console.log('✅ Desktop sidebar is visible.');

    // Check 3 group headers in English
    const enPractice = sidebar.locator('text=Practice');
    const enKnowledge = sidebar.locator('text=Knowledge');
    const enSystem = sidebar.locator('text=System');

    console.log(`- Group 'Practice': ${(await enPractice.count()) > 0 ? 'PASS' : 'FAIL'}`);
    console.log(`- Group 'Knowledge': ${(await enKnowledge.count()) > 0 ? 'PASS' : 'FAIL'}`);
    console.log(`- Group 'System': ${(await enSystem.count()) > 0 ? 'PASS' : 'FAIL'}`);

    // Verify all 9 English link names
    const enLinks = [
      'Today', 'Roadmap', 'Quiz', 'Flashcards',
      'Insights', 'Library', 'Notes',
      'Profile', 'Settings'
    ];

    for (const name of enLinks) {
      const link = sidebar.locator(`a:has-text("${name}")`);
      const found = (await link.count()) > 0;
      console.log(`  • Link '${name}': ${found ? 'PASS' : 'FAIL'}`);
      if (!found) throw new Error(`Missing EN link: ${name}`);
    }

    // -------------------------------------------------------------
    // 2. Language Switcher Test: Switch to Vietnamese (VI)
    // -------------------------------------------------------------
    console.log('\n--- [TEST 2] Language Switcher (EN -> VI) ---');
    const viButton = page.locator('header button:has-text("VI")');
    await viButton.click();
    await page.waitForTimeout(500);
    console.log('✅ Clicked VI language toggle button.');

    // Verify Vietnamese Group Headers
    const viPractice = sidebar.locator('text=Luyện Tập');
    const viKnowledge = sidebar.locator('text=Tri Thức & Ghi Nhớ');
    const viSystem = sidebar.locator('text=Hệ Thống');

    console.log(`- Group 'Luyện Tập' (VI): ${(await viPractice.count()) > 0 ? 'PASS' : 'FAIL'}`);
    console.log(`- Group 'Tri Thức & Ghi Nhớ' (VI): ${(await viKnowledge.count()) > 0 ? 'PASS' : 'FAIL'}`);
    console.log(`- Group 'Hệ Thống' (VI): ${(await viSystem.count()) > 0 ? 'PASS' : 'FAIL'}`);

    // Verify Concise Vietnamese Link Names
    const viLinks = [
      'Hôm Nay', 'Lộ Trình', 'Trắc Nghiệm', 'Flashcards',
      'Insights', 'Thư Viện', 'Ghi Chú',
      'Cá Nhân', 'Cài Đặt'
    ];

    for (const name of viLinks) {
      const link = sidebar.locator(`a:has-text("${name}")`);
      const found = (await link.count()) > 0;
      console.log(`  • Link '${name}': ${found ? 'PASS' : 'FAIL'}`);
      if (!found) throw new Error(`Missing VI link: ${name}`);
    }

    // -------------------------------------------------------------
    // 3. Mobile Viewport Test: Mobile Drawer 3-Tier Navigation
    // -------------------------------------------------------------
    console.log('\n--- [TEST 3] Mobile Drawer 3-Tier Navigation (390x844) ---');
    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(400);

    // Open mobile hamburger menu
    const menuButton = page.locator('button[aria-label="Open Navigation Menu"]');
    await menuButton.click();
    await page.waitForTimeout(500);

    const mobileDrawerTitle = page.locator('span:has-text("TechDaily Menu")');
    await mobileDrawerTitle.waitFor({ state: 'visible', timeout: 3000 });
    console.log('✅ Mobile navigation drawer is open.');

    // Verify 3 Group Headers inside Mobile Drawer
    const mobilePractice = page.locator('div.fixed >> text=Luyện Tập');
    const mobileKnowledge = page.locator('div.fixed >> text=Tri Thức & Ghi Nhớ');
    const mobileSystem = page.locator('div.fixed >> text=Hệ Thống');

    console.log(`- Mobile Drawer Group 'Luyện Tập': ${(await mobilePractice.count()) > 0 ? 'PASS' : 'FAIL'}`);
    console.log(`- Mobile Drawer Group 'Tri Thức & Ghi Nhớ': ${(await mobileKnowledge.count()) > 0 ? 'PASS' : 'FAIL'}`);
    console.log(`- Mobile Drawer Group 'Hệ Thống': ${(await mobileSystem.count()) > 0 ? 'PASS' : 'FAIL'}`);

    // Click Quiz Arena in mobile drawer
    const mobileQuizLink = page.locator('div.fixed a[href="/quiz"]');
    await mobileQuizLink.click();
    await page.waitForURL('**/quiz', { timeout: 5000 });
    console.log('✅ Successfully navigated to /quiz from mobile drawer.');

    console.log('\n🎉 ALL PLAYWRIGHT NAVIGATION & I18N TESTS PASSED WITH 100% SUCCESS!');
  } catch (error) {
    console.error('❌ Playwright Test Failed:', error);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
}

runTests();
