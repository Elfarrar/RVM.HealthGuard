import { expect, test } from '@playwright/test';

const defaultBaseUrl = process.env.HEALTHGUARD_BASE_URL ?? 'https://healthguard.lab.rvmtech.com.br';

test.describe('HealthGuard — API Monitoring', () => {
  test.skip(
    process.env.HEALTHGUARD_RUN_SMOKE !== '1',
    'Defina HEALTHGUARD_RUN_SMOKE=1 para rodar o smoke contra um ambiente real.',
  );

  test('GET /api/status — retorna visão geral do status', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/status`);
    expect(response.status()).toBe(200);
  });

  test('GET /api/services — retorna lista ou exige autenticação', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/services`);
    expect([200, 401]).toContain(response.status());
  });

  test('GET /api/incidents — retorna lista ou exige autenticação', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/incidents`);
    expect([200, 401]).toContain(response.status());
  });

  test('GET /api/status/summary — retorna dados públicos da página de status', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/status/summary`);
    expect(response.status()).toBe(200);
  });

  test('GET /api/services sem autenticação — não retorna erro de servidor', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/services`);
    expect(response.status()).toBeLessThan(500);
  });
});
