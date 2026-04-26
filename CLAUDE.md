# RVM.HealthGuard

## Visao Geral
Plataforma de monitoramento HTTP de endpoints externos. Executa health checks periodicos via `HealthCheckWorker` (BackgroundService), detecta incidentes, calcula uptime, e notifica via `NotifyAlertService`. Dashboard Blazor Server com atualizacoes em tempo real via SignalR.

Projeto portfolio demonstrando monitoramento proativo, gestao de incidentes e alertas, com arquitetura Clean dividida em Domain/Infrastructure/API.

## Stack
- .NET 10, ASP.NET Core, Blazor Server
- SignalR (`HealthStatusHub` em `/hubs/health-status`)
- Entity Framework Core + PostgreSQL (endpoints, incidentes, historico)
- `IHttpClientFactory` (health checks HTTP)
- Autenticacao via API Key
- Rate limiting: 60 req/min global, 200 req/min por API Key
- Serilog + Seq, RVM.Common.Security
- xUnit 63 testes, Playwright E2E

## Estrutura do Projeto
```
src/
  RVM.HealthGuard.API/
    Auth/                     # ApiKeyAuthHandler
    Components/               # Blazor pages (dashboard, endpoints, incidentes)
    Controllers/              # REST endpoints (CRUD endpoints monitorados)
    Health/                   # DatabaseHealthCheck
    Hubs/                     # HealthStatusHub (SignalR)
    Middleware/               # CorrelationIdMiddleware
    Services/
      UptimeCalculatorService # Calcula % uptime por periodo
      NotifyAlertService      # Notificacoes de alerta (singleton)
      HealthCheckWorker       # BackgroundService: dispara checks periodicos
  RVM.HealthGuard.Domain/     # Entidades (MonitoredEndpoint, Incident, CheckResult)
  RVM.HealthGuard.Infrastructure/
    Data/                     # HealthGuardDbContext
    Repositories/             # IEndpointRepository, IIncidentRepository
test/
  RVM.HealthGuard.Test/       # xUnit (63 testes)
  playwright/                 # Testes E2E
```

## Convencoes
- `NotifyAlertService` e singleton — mantem estado de alertas ativos entre verificacoes
- `HealthCheckWorker` usa `PeriodicTimer` — intervalo configuravel em `appsettings.json`
- SignalR hub anonimo (`AllowAnonymous`) — dashboard publico sem login
- Incidente aberto automaticamente apos N falhas consecutivas (threshold configuravel)
- `EnsureCreated` em dev, migration EF Core em producao

## Como Rodar
### Dev
```bash
docker compose -f docker-compose.dev.yml up -d
cd src/RVM.HealthGuard.API
dotnet run
```

### Testes
```bash
dotnet test test/RVM.HealthGuard.Test/
```

## Decisoes Arquiteturais
- **NotifyAlertService como singleton**: evita disparar alertas duplicados para o mesmo incidente — estado de "ja notificado" precisa sobreviver ao ciclo de request
- **SignalR hub anonimo**: dashboard de monitoramento e publico por design — stakeholders nao precisam login
- **UptimeCalculatorService separado**: logica de calculo de SLA e complexa (janelas de tempo, exclusoes) e testavel isoladamente
- **HttpClientFactory para checks**: respeita boas praticas de reutilizacao de socket e permite named clients com timeout customizado por endpoint
