# Testes — RVM.HealthGuard

## Testes Unitarios
- **Framework:** xUnit + Moq
- **Localizacao:** `test/RVM.HealthGuard.Test/`
- **Total:** 63 testes
- **Foco:** UptimeCalculatorService, NotifyAlertService, HealthCheckWorker (com HttpClient mockado), logica de incidentes

```bash
dotnet test test/RVM.HealthGuard.Test/
```

## Testes E2E (Playwright)
- **Localizacao:** `test/playwright/`
- **Cobertura:** dashboard Blazor (listagem de endpoints, historico de uptime, detalhes de incidente, atualizacoes SignalR)

```bash
cd test/playwright
npm install
npx playwright install --with-deps
npx playwright test
```

Variaveis de ambiente necessarias:
```
HEALTHGUARD_BASE_URL=http://localhost:5000
HEALTHGUARD_API_KEY=<api-key-dev>
```

## CI
- **Arquivo:** `.github/workflows/ci.yml`
- Pipeline: build → testes unitarios → Playwright
- `HealthCheckWorker` desativado em testes (via `IHostedService` mock ou configuracao de ambiente)
