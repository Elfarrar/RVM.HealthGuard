*[English](README.en.md) | **Portugues***

# RVM.HealthGuard

Sistema de monitoramento de saude e gerenciamento de incidentes para servicos HTTP.

![build](https://img.shields.io/badge/build-passing-brightgreen)
![tests](https://img.shields.io/badge/tests-29%20passed-brightgreen)
![license](https://img.shields.io/badge/license-MIT-blue)
![dotnet](https://img.shields.io/badge/.NET-10.0-purple)

---

## Sobre

RVM.HealthGuard e um sistema de monitoramento de saude e gerenciamento de incidentes. Monitora continuamente endpoints HTTP de servicos, rastreia status em tempo real, detecta incidentes (downtime, degradacao, timeout) e fornece analytics historicos detalhados. Utiliza PostgreSQL para persistencia, SignalR para notificacoes em tempo real e Background Services para polling automatico dos endpoints.

## Tecnologias

| Tecnologia | Versao |
|---|---|
| .NET | 10.0 |
| ASP.NET Core | 10.0 |
| SignalR | 10.0 |
| Entity Framework Core | 10.0.5 |
| PostgreSQL (Npgsql) | 10.0.1 |
| Serilog | 10.0.0 |
| xUnit | 2.9.3 |
| Moq | 4.20.72 |

## Arquitetura

```
┌─────────────────────────────────────────────────────────┐
│                        API Layer                        │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐ │
│  │  Controllers │ │  SignalR Hub │ │   Middleware      │ │
│  │  - Services  │ │  HealthStatus│ │  - CorrelationId │ │
│  │  - Incidents │ │              │ │  - RateLimiter   │ │
│  │  - Status    │ │              │ │  - ApiKey Auth   │ │
│  └──────┬───────┘ └──────┬───────┘ └──────────────────┘ │
│         │                │                              │
│  ┌──────┴────────────────┴───────┐                      │
│  │      BackgroundService        │                      │
│  │      HealthCheckWorker        │                      │
│  │      UptimeCalculatorService  │                      │
│  └──────────────┬────────────────┘                      │
├─────────────────┼───────────────────────────────────────┤
│                 │         Domain Layer                   │
│  ┌──────────────┴────────────────┐                      │
│  │  Entities                     │                      │
│  │  - MonitoredService           │                      │
│  │  - HealthCheckResult          │                      │
│  │  - ServiceIncident            │                      │
│  │  Interfaces (Repository)      │                      │
│  └──────────────┬────────────────┘                      │
├─────────────────┼───────────────────────────────────────┤
│                 │    Infrastructure Layer                │
│  ┌──────────────┴────────────────┐                      │
│  │  HealthGuardDbContext (EF)    │                      │
│  │  Repositories                 │                      │
│  │  - MonitoredServiceRepository │                      │
│  │  - HealthCheckResultRepository│                      │
│  │  - ServiceIncidentRepository  │                      │
│  └──────────────┬────────────────┘                      │
│                 │                                        │
│           ┌─────┴─────┐                                 │
│           │ PostgreSQL │                                 │
│           └───────────┘                                  │
└─────────────────────────────────────────────────────────┘
```

## Estrutura do Projeto

```
RVM.HealthGuard/
├── src/
│   ├── RVM.HealthGuard.API/
│   │   ├── Auth/
│   │   │   ├── ApiKeyAuthHandler.cs
│   │   │   └── ApiKeyAuthOptions.cs
│   │   ├── Controllers/
│   │   │   ├── IncidentsController.cs
│   │   │   ├── ServicesController.cs
│   │   │   └── StatusController.cs
│   │   ├── Dtos/
│   │   │   ├── IncidentDtos.cs
│   │   │   ├── ServiceDtos.cs
│   │   │   └── StatusDtos.cs
│   │   ├── Health/
│   │   │   └── DatabaseHealthCheck.cs
│   │   ├── Hubs/
│   │   │   └── HealthStatusHub.cs
│   │   ├── Middleware/
│   │   │   └── CorrelationIdMiddleware.cs
│   │   ├── Services/
│   │   │   ├── HealthCheckWorker.cs
│   │   │   └── UptimeCalculatorService.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── RVM.HealthGuard.Domain/
│   │   ├── Entities/
│   │   │   ├── HealthCheckResult.cs
│   │   │   ├── MonitoredService.cs
│   │   │   └── ServiceIncident.cs
│   │   ├── Enums/
│   │   │   ├── IncidentType.cs
│   │   │   └── ServiceHealthStatus.cs
│   │   └── Interfaces/
│   │       ├── IHealthCheckResultRepository.cs
│   │       ├── IMonitoredServiceRepository.cs
│   │       └── IServiceIncidentRepository.cs
│   └── RVM.HealthGuard.Infrastructure/
│       ├── Data/
│       │   ├── Configurations/
│       │   │   ├── HealthCheckResultConfiguration.cs
│       │   │   ├── MonitoredServiceConfiguration.cs
│       │   │   └── ServiceIncidentConfiguration.cs
│       │   └── HealthGuardDbContext.cs
│       ├── Repositories/
│       │   ├── HealthCheckResultRepository.cs
│       │   ├── MonitoredServiceRepository.cs
│       │   └── ServiceIncidentRepository.cs
│       └── DependencyInjection.cs
├── test/
│   └── RVM.HealthGuard.Test/
│       ├── Controllers/
│       │   └── ServicesControllerTests.cs
│       ├── Domain/
│       │   └── EntityTests.cs
│       ├── Infrastructure/
│       │   └── RepositoryTests.cs
│       └── Services/
│           └── UptimeCalculatorTests.cs
├── docker-compose.dev.yml
├── docker-compose.prod.yml
└── global.json
```

## Como Executar

### Pre-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.201+)
- [PostgreSQL](https://www.postgresql.org/) (ou Docker)

### Configuracao

1. Configure a connection string no `appsettings.json` ou via variavel de ambiente:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=rvmhealthguard;Username=postgres;Password=SuaSenha"
```

2. Configure as API Keys:

```bash
export ApiKeys__Keys__0__Key="sua-api-key"
export ApiKeys__Keys__0__AppId="app-id"
export ApiKeys__Keys__0__Name="nome-da-app"
```

### Executar localmente

```bash
cd src/RVM.HealthGuard.API
dotnet run
```

### Executar com Docker

```bash
docker compose -f docker-compose.dev.yml up -d
```

### Executar testes

```bash
dotnet test
```

## Endpoints da API

Todos os endpoints requerem autenticacao via header `X-API-Key`.

| Metodo | Rota | Descricao |
|---|---|---|
| `GET` | `/api/services` | Listar servicos monitorados |
| `GET` | `/api/services/{id}` | Obter servico por ID |
| `POST` | `/api/services` | Adicionar servico |
| `PUT` | `/api/services/{id}` | Atualizar servico |
| `DELETE` | `/api/services/{id}` | Remover servico |
| `GET` | `/api/incidents` | Listar todos os incidentes (filtro por data) |
| `GET` | `/api/incidents/{id}` | Obter incidente por ID |
| `GET` | `/api/incidents/active` | Incidentes ativos |
| `GET` | `/api/incidents/service/{serviceId}` | Incidentes por servico |
| `GET` | `/api/status` | Status de todos os servicos |
| `GET` | `/api/status/{serviceId}` | Status de um servico |
| `GET` | `/api/status/{serviceId}/history` | Historico de health checks |
| `GET` | `/health` | Health check da aplicacao (anonimo) |
| `WS` | `/hubs/health-status` | SignalR Hub - notificacoes em tempo real (anonimo) |

### Eventos SignalR

| Evento | Descricao |
|---|---|
| `ServiceStatusChanged` | Emitido a cada health check realizado |
| `IncidentStarted` | Emitido quando um incidente e detectado |
| `IncidentResolved` | Emitido quando um incidente e resolvido |

## Testes

29 testes automatizados cobrindo todas as camadas:

| Arquivo | Testes | Descricao |
|---|---|---|
| `ServicesControllerTests.cs` | 8 | CRUD do controller, validacoes de entrada, retornos HTTP |
| `EntityTests.cs` | 6 | Valores padrao das entidades, timestamps de criacao |
| `RepositoryTests.cs` | 11 | Operacoes de repositorio (CRUD, filtros, queries por data) |
| `UptimeCalculatorTests.cs` | 4 | Calculo de uptime percentual e tempo medio de resposta |

```bash
dotnet test --verbosity normal
```

## Funcionalidades

- [x] Monitoramento HTTP com intervalos e timeouts configuraveis
- [x] Rastreamento de status em tempo real: Healthy, Unhealthy, Degraded
- [x] Deteccao automatica de incidentes: Down, Degraded, Timeout
- [x] Calculo de uptime percentual em janelas de 24 horas
- [x] Analytics de tempo medio de resposta
- [x] Notificacoes em tempo real via SignalR
- [x] HealthCheckWorker como BackgroundService
- [x] Historico completo de auditorias
- [x] Rate limiting (60 req/min global, 200 req/min por API Key)
- [x] Autenticacao via API Key customizada
- [x] Correlation ID via middleware
- [x] Health check do banco de dados
- [x] Auto-migration do banco na inicializacao
- [x] Suporte a reverse proxy (PathBase + Forwarded Headers)
- [x] Logging estruturado com Serilog (JSON compacto)
- [x] Docker Compose para dev e prod

---

Desenvolvido por **RVM Tech**
