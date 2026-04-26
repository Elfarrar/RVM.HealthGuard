/**
 * RVM.HealthGuard — Gerador de Manual Visual
 *
 * Playwright script que navega por todas as telas do sistema de monitoramento HTTP,
 * captura screenshots em desktop e mobile, e gera as imagens para o manual.
 *
 * Uso:
 *   cd test/playwright
 *   npx playwright test tests/generate-manual.spec.ts --reporter=list
 */
import { test, type Page } from '@playwright/test';
import path from 'path';

const BASE_URL = process.env.HEALTHGUARD_BASE_URL ?? 'https://healthguard.lab.rvmtech.com.br';
const SCREENSHOTS_DIR = path.resolve(__dirname, '../../../docs/screenshots');

/** Captura desktop (1280x800) + mobile (390x844) */
async function capture(page: Page, name: string, opts?: { fullPage?: boolean }) {
  const fullPage = opts?.fullPage ?? true;
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, `${name}--desktop.png`), fullPage });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, `${name}--mobile.png`), fullPage });
  await page.setViewportSize({ width: 1280, height: 800 });
}

test.describe('RVM.HealthGuard — Manual Visual', () => {
  test('01 Dashboard', async ({ page }) => {
    await page.goto(`${BASE_URL}/`);
    await page.waitForLoadState('networkidle');
    await capture(page, '01-dashboard');
  });

  test('02 Servicos', async ({ page }) => {
    await page.goto(`${BASE_URL}/services`);
    await page.waitForLoadState('networkidle');
    await capture(page, '02-services');
  });

  test('03 Incidentes', async ({ page }) => {
    await page.goto(`${BASE_URL}/incidents`);
    await page.waitForLoadState('networkidle');
    await capture(page, '03-incidents');
  });

  test('04 Uptime (API)', async ({ page }) => {
    await page.goto(`${BASE_URL}/api/uptime`);
    await page.waitForLoadState('networkidle');
    await capture(page, '04-uptime');
  });

  test('05 Alertas (API)', async ({ page }) => {
    await page.goto(`${BASE_URL}/api/alerts`);
    await page.waitForLoadState('networkidle');
    await capture(page, '05-alerts');
  });
});
