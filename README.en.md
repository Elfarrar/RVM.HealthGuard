***English** | [Portugues](README.md)*

# RVM.HealthGuard

Health monitoring and incident management system for HTTP services.

![build](https://img.shields.io/badge/build-passing-brightgreen)
![tests](https://img.shields.io/badge/tests-29%20passed-brightgreen)
![license](https://img.shields.io/badge/license-MIT-blue)
![dotnet](https://img.shields.io/badge/.NET-10.0-purple)

---

## About

RVM.HealthGuard is a health monitoring and incident management system. It continuously monitors HTTP endpoints, tracks real-time status, detects incidents (downtime, degradation, timeout), and provides detailed historical analytics. It uses PostgreSQL for persistence, SignalR for real-time notifications, and Background Services for automatic endpoint polling.

## Technologies

| Technology | Version |
|---|---|
| .NET | 10.0 |
| ASP.NET Core | 10.0 |
| SignalR | 10.0 |
| Entity Framework Core | 10.0.5 |
| PostgreSQL (Npgsql) | 10.0.1 |
| Serilog | 10.0.0 |
| xUnit | 2.9.3 |
| Moq | 4.20.72 |

## Architecture

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

## Project Structure

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

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.201+)
- [PostgreSQL](https://www.postgresql.org/) (or Docker)

### Configuration

1. Set the connection string in `appsettings.json` or via environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=rvmhealthguard;Username=postgres;Password=YourPassword"
```

2. Configure API Keys:

```bash
export ApiKeys__Keys__0__Key="your-api-key"
export ApiKeys__Keys__0__AppId="app-id"
export ApiKeys__Keys__0__Name="app-name"
```

### Run locally

```bash
cd src/RVM.HealthGuard.API
dotnet run
```

### Run with Docker

```bash
docker compose -f docker-compose.dev.yml up -d
```

### Run tests

```bash
dotnet test
```

## API Endpoints

All endpoints require authentication via the `X-API-Key` header.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/services` | List monitored services |
| `GET` | `/api/services/{id}` | Get service by ID |
| `POST` | `/api/services` | Add a service |
| `PUT` | `/api/services/{id}` | Update a service |
| `DELETE` | `/api/services/{id}` | Remove a service |
| `GET` | `/api/incidents` | List all incidents (date filter) |
| `GET` | `/api/incidents/{id}` | Get incident by ID |
| `GET` | `/api/incidents/active` | Active incidents |
| `GET` | `/api/incidents/service/{serviceId}` | Incidents by service |
| `GET` | `/api/status` | Status of all services |
| `GET` | `/api/status/{serviceId}` | Status of a single service |
| `GET` | `/api/status/{serviceId}/history` | Health check history |
| `GET` | `/health` | Application health check (anonymous) |
| `WS` | `/hubs/health-status` | SignalR Hub - real-time notifications (anonymous) |

### SignalR Events

| Event | Description |
|---|---|
| `ServiceStatusChanged` | Emitted on each health check performed |
| `IncidentStarted` | Emitted when an incident is detected |
| `IncidentResolved` | Emitted when an incident is resolved |

## Tests

29 automated tests covering all layers:

| File | Tests | Description |
|---|---|---|
| `ServicesControllerTests.cs` | 8 | Controller CRUD, input validation, HTTP responses |
| `EntityTests.cs` | 6 | Entity default values, creation timestamps |
| `RepositoryTests.cs` | 11 | Repository operations (CRUD, filters, date-range queries) |
| `UptimeCalculatorTests.cs` | 4 | Uptime percentage and average response time calculations |

```bash
dotnet test --verbosity normal
```

## Features

- [x] HTTP monitoring with configurable intervals and timeouts
- [x] Real-time status tracking: Healthy, Unhealthy, Degraded
- [x] Automatic incident detection: Down, Degraded, Timeout
- [x] Uptime percentage calculation over 24-hour windows
- [x] Average response time analytics
- [x] Real-time notifications via SignalR
- [x] HealthCheckWorker as BackgroundService
- [x] Complete audit history
- [x] Rate limiting (60 req/min global, 200 req/min per API Key)
- [x] Custom API Key authentication
- [x] Correlation ID middleware
- [x] Database health check
- [x] Auto-migration on startup
- [x] Reverse proxy support (PathBase + Forwarded Headers)
- [x] Structured logging with Serilog (compact JSON)
- [x] Docker Compose for dev and prod

---

Developed by **RVM Tech**
