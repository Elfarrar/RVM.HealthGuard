import { test, expect } from '@playwright/test';

test.describe('Health Check', () => {
  test.skip(!process.env.HEALTHGUARD_RUN_SMOKE, 'Smoke tests are not enabled');

  test('should return 200 Healthy', async ({ request }) => {
    const response = await request.get('/health');
    expect(response.status()).toBe(200);
    expect(await response.text()).toContain('Healthy');
  });
});
