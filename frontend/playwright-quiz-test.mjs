import { chromium } from 'playwright';
import path from 'path';

const ARTIFACT_DIR = '/home/duycld03/.gemini/antigravity/brain/63f799ff-7850-49df-8afc-f2c7df7ccdf3';

async function run() {
  console.log('🚀 Running Bilingual Playwright E2E Verification (EN & VI)...');
  
  const loginRes = await fetch('http://localhost:5000/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'test@techdaily.io', password: 'Password123!' })
  });
  const { token, user } = await loginRes.json();
  console.log('🔑 Authenticated as:', user.name, `(${user.email})`);

  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  // ==========================================
  // 1. TEST ENGLISH LOCALE (EN)
  // ==========================================
  console.log('\n--- 🇬🇧 Testing English (EN) Locale ---');
  const contextEn = await browser.newContext({
    viewport: { width: 1280, height: 900 }
  });
  await contextEn.addCookies([
    { name: 'techdaily_token', value: token, domain: 'localhost', path: '/' },
    { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
    { name: 'i18n_redirected', value: 'en', domain: 'localhost', path: '/' }
  ]);

  const pageEn = await contextEn.newPage();
  await pageEn.goto('http://localhost:3000/quiz', { waitUntil: 'networkidle' });

  // Light Mode - Desktop EN
  await pageEn.evaluate(() => document.documentElement.classList.remove('dark'));
  await pageEn.waitForTimeout(400);
  await pageEn.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_en_desktop_light.png') });

  // Generate EN Quiz
  const generateBtnEn = pageEn.locator('[data-testid="generate-quiz-btn"]');
  await generateBtnEn.click();
  await pageEn.waitForSelector('[data-testid="quiz-option"]', { timeout: 45000 });
  await pageEn.waitForTimeout(600);

  // Submit Answer in EN
  const optionEn = pageEn.locator('[data-testid="quiz-option"]').first();
  await optionEn.click();
  const submitBtnEn = pageEn.locator('[data-testid="submit-answer-btn"]');
  await submitBtnEn.click();
  await pageEn.waitForSelector('.prose', { timeout: 15000 });
  await pageEn.waitForTimeout(600);

  // Dark Mode - Desktop EN
  await pageEn.evaluate(() => document.documentElement.classList.add('dark'));
  await pageEn.waitForTimeout(400);
  await pageEn.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_en_desktop_dark.png') });

  // Mobile (390x844) - EN Dark & Light
  await pageEn.setViewportSize({ width: 390, height: 844 });
  await pageEn.waitForTimeout(400);
  await pageEn.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_en_mobile_dark.png') });

  await pageEn.evaluate(() => document.documentElement.classList.remove('dark'));
  await pageEn.waitForTimeout(400);
  await pageEn.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_en_mobile_light.png') });

  await contextEn.close();

  // ==========================================
  // 2. TEST VIETNAMESE LOCALE (VI)
  // ==========================================
  console.log('\n--- 🇻🇳 Testing Vietnamese (VI) Locale ---');
  const contextVi = await browser.newContext({
    viewport: { width: 1280, height: 900 }
  });
  await contextVi.addCookies([
    { name: 'techdaily_token', value: token, domain: 'localhost', path: '/' },
    { name: 'techdaily_user', value: encodeURIComponent(JSON.stringify(user)), domain: 'localhost', path: '/' },
    { name: 'i18n_redirected', value: 'vi', domain: 'localhost', path: '/' }
  ]);

  const pageVi = await contextVi.newPage();
  await pageVi.goto('http://localhost:3000/quiz', { waitUntil: 'networkidle' });

  // Switch to VI explicitly if needed
  await pageVi.evaluate(() => {
    document.documentElement.classList.remove('dark');
  });
  const viLangBtn = pageVi.locator('button:has-text("VI")').first();
  if (await viLangBtn.isVisible()) {
    await viLangBtn.click();
    await pageVi.waitForTimeout(600);
  }

  // Light Mode - Desktop VI Generator
  await pageVi.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_vi_desktop_light.png') });

  // Generate VI Quiz
  const generateBtnVi = pageVi.locator('[data-testid="generate-quiz-btn"]');
  await generateBtnVi.click();
  await pageVi.waitForSelector('[data-testid="quiz-option"]', { timeout: 45000 });
  await pageVi.waitForTimeout(600);

  // Submit Answer in VI
  const optionVi = pageVi.locator('[data-testid="quiz-option"]').first();
  await optionVi.click();
  const submitBtnVi = pageVi.locator('[data-testid="submit-answer-btn"]');
  await submitBtnVi.click();
  await pageVi.waitForSelector('.prose', { timeout: 15000 });
  await pageVi.waitForTimeout(600);

  // Dark Mode - Desktop VI
  await pageVi.evaluate(() => document.documentElement.classList.add('dark'));
  await pageVi.waitForTimeout(400);
  await pageVi.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_vi_desktop_dark.png') });

  // Mobile (390x844) - VI Dark & Light
  await pageVi.setViewportSize({ width: 390, height: 844 });
  await pageVi.waitForTimeout(400);
  await pageVi.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_vi_mobile_dark.png') });

  await pageVi.evaluate(() => document.documentElement.classList.remove('dark'));
  await pageVi.waitForTimeout(400);
  await pageVi.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_vi_mobile_light.png') });

  await contextVi.close();
  await browser.close();

  console.log('\n🎉 Bilingual E2E Verification Completed: All EN & VI Screenshots Captured!');
}

run().catch(err => {
  console.error('❌ Playwright Test Error:', err);
  process.exit(1);
});
