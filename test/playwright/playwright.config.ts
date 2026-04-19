import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.HEALTHGUARD_BASE_URL ?? 'https://healthguard.lab.rvmtech.com.br';

export default defineConfig({
  testDir: './tests',
  timeout: 120_000,
  outputDir: 'test-results',
  expect: {
    timeout: 30_000,
  },
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    ignoreHTTPSErrors: true,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
