# Oficina Tech — Sistema de Gestão de Ordens de Serviço

Backend REST API for auto-repair shop management. Built with .NET 10 and PostgreSQL, the system covers the full service-order lifecycle: client registration, vehicle linking, service-order creation, automatic budget generation, client approval via CPF/CNPJ, stock deduction, and status tracking from intake to delivery. The codebase follows a Domain-Driven Design layered architecture. This is an academic FIAP Phase 1 postgraduate project.

---

## Objectives

- Manage the complete OS (Ordem de Serviço) lifecycle: open, diagnose, send for approval, finalize, and mark as delivered.
- Authenticate mechanics and admins via JWT with configurable token expiry.
- Provide admin CRUD for the four core aggregates: Clients, Vehicles, ServiceTypes, and Parts.
- Generate automatic budgets from ordered services and parts.
- Allow clients to approve or reject budgets using only their CPF or CNPJ (no account required).
- Enforce stock management: decrement Part quantity on finalization with optimistic-concurrency protection.
- Achieve >= 80% test coverage on the OficinaTech.Domain and OficinaTech.Application namespaces.

---

## Architecture

### Project Structure

The solution follows a four-layer DDD layout. Each layer is a separate C# project under `src/`:

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Domain | OficinaTech.Domain | Entities, value objects, aggregates, repository interfaces, DomainException. Zero external dependencies — no EF Core, no ASP.NET Core. |
| Application | OficinaTech.Application | Use-case services, DTOs, IUnitOfWork interface. Orchestrates domain objects and calls repository interfaces. |
| Infrastructure | OficinaTech.Infrastructure | EF Core DbContext, entity configurations, repository implementations, EfUnitOfWork, migrations, JWT credential service. References Application to implement IUnitOfWork. |
| Presentation | OficinaTech.Presentation | ASP.NET Core 10 host, controllers, middleware (DomainExceptionHandler, global exception handler), Scalar UI registration, startup migrations. |

Tests live under `tests/OficinaTech.Tests/` (xUnit project). Integration tests use Testcontainers to spin up a real PostgreSQL container at test runtime.

### Technology Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| Runtime | .NET 10 / ASP.NET Core 10 | Target framework net10.0 |
| Database | PostgreSQL 16 (Docker) | Managed via EF Core + Npgsql |
| ORM | Entity Framework Core 10.0.11 | Code-first migrations, auto-applied on startup |
| Auth | JWT Bearer | 15-min default expiry (configurable via `Admin__JwtExpiryMinutes`) |
| API Docs | Scalar at /scalar | Replaces Swashbuckle (removed in .NET 9+) |
| Mapping | Mapster 10 | MIT license; faster than AutoMapper |
| Testing | xUnit 2.9.3 + NSubstitute 6.2 + Testcontainers | Unit + integration tests |
| SAST | Security Code Scan | Run via `security-scan OficinaTech.sln`; 0 warnings on current solution |

---

## Prerequisites

- **Docker Desktop >= 20.x** (or Rancher Desktop) — required for the quick-start path
- **.NET SDK 10.0.400 or later** — required for the manual setup path
  - macOS: `brew install dotnet`
  - Other: https://dotnet.microsoft.com/download
- **Git**

---

## Quick Start (Docker Compose — 5 minutes)

This path requires only Docker Desktop. No local .NET SDK installation needed.

1. Clone the repository:

   ```bash
   git clone <repo-url> && cd phase-1-fiap-postgraduate
   ```

2. Set the required secrets and start all containers:

   ```bash
   docker compose up --build
   ```

   > The `api` service requires `Admin__PasswordHash` and `Admin__JwtSecret` to be set. For local development, edit `docker-compose.yml` and replace the placeholder values before running. See the environment variable reference below.

3. Wait for the following line in the container logs:

   ```
   Now listening on: http://0.0.0.0:8080
   ```

   EF Core migrations run automatically on startup. No manual migration step is needed.

4. Open Scalar UI to browse and test all endpoints:

   ```
   http://localhost:8080/scalar
   ```

5. Authenticate — send a POST request to obtain a JWT token:

   ```bash
   curl -s -X POST http://localhost:8080/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@oficina.tech","password":"<your-admin-password>"}'
   ```

   The response body contains a `token` field. Copy that value.

6. Use the token in Scalar (click "Authorize") or in any HTTP client as a Bearer header:

   ```
   Authorization: Bearer <token>
   ```

### Environment Variable Reference (docker-compose.yml)

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `Admin__Email` | Yes | Admin login e-mail |
| `Admin__PasswordHash` | Yes | BCrypt hash of the admin password (min cost 12) |
| `Admin__JwtSecret` | Yes | HMAC-SHA256 signing key (min 32 characters enforced at startup) |
| `Admin__JwtExpiryMinutes` | No | Token lifetime in minutes (default: 15) |

---

## Manual Setup (Without Docker)

Only required for local development without containerizing the API.

1. Start PostgreSQL (Docker for the database only):

   ```bash
   docker compose up -d postgres
   ```

2. Export the connection string:

   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=oficina_tech;Username=oficina;Password=oficina_secret"
   ```

3. Install the EF Core CLI tool (if not already installed):

   ```bash
   dotnet tool restore
   ```

4. Apply migrations:

   ```bash
   dotnet ef database update \
     --project src/OficinaTech.Infrastructure \
     --startup-project src/OficinaTech.Infrastructure
   ```

5. Run the API:

   ```bash
   dotnet run --project src/OficinaTech.Presentation
   ```

6. The API is available at the port shown in the console output (typically `http://localhost:5000`).

---

## API Reference

All endpoints are also documented interactively at `http://localhost:8080/scalar`.

### Authentication

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/auth/login | Body: `{"email":"…","password":"…"}` — Returns `{"token":"…"}` |

### Admin Endpoints (JWT required)

All admin endpoints require the `Authorization: Bearer <token>` header.

| Resource | Base Path | Key Operations |
|----------|-----------|----------------|
| Clients | /api/clients | GET (list, filter by taxId), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Vehicles | /api/vehicles | GET (list), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Vehicles by client | /api/clients/{clientId}/vehicles | GET — list vehicles owned by a specific client |
| Service Types | /api/service-types | GET (list), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Parts | /api/parts | GET (list), GET /{id}, POST, PUT /{id}, DELETE /{id} |
| Service Orders | /api/service-orders | GET (list), GET /{id}, POST, PUT /{id} |

OS lifecycle state-transition endpoints:

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/service-orders/{id}/start-diagnosis | Move OS from Received to InDiagnosis |
| POST | /api/service-orders/{id}/send-for-approval | Move OS from InDiagnosis to AwaitingApproval |
| POST | /api/service-orders/{id}/approve | (Admin path) Force-approve an OS |
| POST | /api/service-orders/{id}/finalize | Move approved OS to Completed; decrements stock |
| POST | /api/service-orders/{id}/mark-delivered | Move completed OS to Delivered |

### Public Endpoints (no authentication required)

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/service-orders/{id}/approve | Client approves (or rejects) a budget using CPF/CNPJ — body: `{"taxId":"…","approved":true}` |
| GET | /api/service-orders/by-client?taxId={taxId} | Client queries own OS list by CPF or CNPJ |

---

## Running Tests

```bash
# Unit tests only (fast — no Docker required)
dotnet test tests/OficinaTech.Tests/OficinaTech.Tests.csproj

# Unit tests with coverage report (namespace-filtered)
dotnet test tests/OficinaTech.Tests/OficinaTech.Tests.csproj \
  --settings coverlet.runsettings \
  --results-directory TestResults/

# Full suite including integration tests (requires Docker Desktop running)
dotnet test tests/OficinaTech.Tests/OficinaTech.Tests.csproj
```

Current coverage: >= 80% on OficinaTech.Domain and OficinaTech.Application namespaces.

Coverage reports are written to `TestResults/` in Cobertura XML format. Open `TestResults/coverage.cobertura.xml` with any Cobertura-compatible viewer (e.g., ReportGenerator).

---

## Security

- JWT secret is configured via `Admin__JwtSecret` (minimum 32 characters enforced at startup; startup fails if shorter).
- Admin password is stored as a BCrypt hash (`Admin__PasswordHash`); the plaintext password is never persisted.
- The login endpoint returns a single generic error on wrong email or wrong password to prevent account enumeration.
- SAST scan via Security Code Scan:

  ```bash
  security-scan OficinaTech.sln --export docs/security-report.sarif
  ```

- Dependency CVE scan:

  ```bash
  dotnet list package --vulnerable --include-transitive
  ```

- Current findings: 0 critical, 0 CVEs. See `docs/security-report.sarif` and `docs/vulnerability-report.txt`.

---

## Domain Glossary (Ubiquitous Language)

| Term (PT) | Term (EN) | Definition |
|-----------|-----------|------------|
| Ordem de Serviço (OS) | Service Order | Central aggregate; tracks the entire repair workflow for one vehicle visit |
| Orçamento | Budget | Auto-calculated total from ordered services and parts; sent to client for approval |
| TaxId | TaxId | CPF (individual) or CNPJ (legal entity) value object; validated by check-digit algorithm |
| Placa | License Plate | Value object accepting old (ABC-1234) and Mercosul (ABC1D23) formats |
| Aprovação | Approval | Client action (approve or reject) that unblocks or cancels OS execution |
| Peça | Part | Inventory item with stock quantity; decremented on OS finalization |
| Tipo de Serviço | Service Type | Named service with a price and average execution-time tracker |

---

## Group and Submission (FIAP)

Grupo: [nome do grupo] | Participantes: [lista] | Discord: [usernames] | Repositório: [link] | Documentação: [Miro Board link]

> This section is completed by the team before PDF submission.
