import { chromium } from 'playwright';
import path from 'path';

const ARTIFACT_DIR = '/home/duycld03/.gemini/antigravity/brain/63f799ff-7850-49df-8afc-f2c7df7ccdf3';

async function run() {
  console.log('🚀 Running Comprehensive Playwright E2E Verification...');
  
  const loginRes = await fetch('http://localhost:5000/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'test@techdaily.io', password: 'Password123!' })
  });
  const { token, user } = await loginRes.json();

  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const context = await browser.newContext({
    viewport: { width: 1280, height: 900 }
  });

  await context.addCookies([
    { name: 'techdaily_token', value: token, domain: 'localhost', path: '/' },
    { name: 'i18n_redirected', value: 'en', domain: 'localhost', path: '/' }
  ]);

  const page = await context.newPage();

  // 1. Light Mode - Desktop Generator
  await page.goto('http://localhost:3000/quiz', { waitUntil: 'networkidle' });
  await page.evaluate(() => {
    document.documentElement.classList.remove('dark');
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_generator_desktop_light.png'), fullPage: true });

  // 2. Generate Quiz
  const generateBtn = page.locator('[data-testid="generate-quiz-btn"]');
  await generateBtn.click();
  await page.waitForSelector('[data-testid="quiz-option"]', { timeout: 45000 });
  await page.waitForTimeout(1000);

  // Light Mode - Desktop Arena Question
  await page.evaluate(() => {
    document.documentElement.classList.remove('dark');
  });
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_arena_desktop_light.png'), fullPage: true });

  // 3. Select Option A and Submit Answer
  const optionA = page.locator('[data-testid="quiz-option"]').first();
  await optionA.click();
  await page.waitForTimeout(500);

  const submitBtn = page.locator('[data-testid="submit-answer-btn"]');
  await submitBtn.click();
  await page.waitForSelector('.prose', { timeout: 15000 });
  await page.waitForTimeout(1000);

  // Light Mode - Answered
  await page.evaluate(() => {
    document.documentElement.classList.remove('dark');
  });
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_answered_desktop_light.png'), fullPage: true });

  // 4. Dark Mode - Desktop Answered
  await page.evaluate(() => {
    document.documentElement.classList.add('dark');
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_answered_desktop_dark.png'), fullPage: true });

  // 5. Mobile Responsive (390x844) - Dark Mode
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_answered_mobile_dark.png'), fullPage: true });

  // 6. Mobile Responsive (390x844) - Light Mode
  await page.evaluate(() => {
    document.documentElement.classList.remove('dark');
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_answered_mobile_light.png'), fullPage: true });

  // 7. Stats Tab - Desktop Light Mode
  await page.setViewportSize({ width: 1280, height: 900 });
  const statsTabBtn = page.locator('[data-testid="stats-tab-btn"]');
  await statsTabBtn.click();
  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_stats_desktop_light.png'), fullPage: true });

  // 8. Stats Tab - Desktop Dark Mode
  await page.evaluate(() => {
    document.documentElement.classList.add('dark');
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'quiz_stats_desktop_dark.png'), fullPage: true });

  await browser.close();
  console.log('🎉 Verification screenshots captured successfully in both Light & Dark modes!');
}

run().catch(err => {
  console.error('❌ Playwright Test Error:', err);
  process.exit(1);
});
