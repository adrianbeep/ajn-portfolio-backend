# AJN Portfolio Backend

ASP.NET Core Minimal API with Clean Architecture, CQRS, MediatR, and ASP.NET Identity + JWT authentication.

**Status**: Functional backend. Auth, CRUD operations, and public read endpoints work. Full test suite green: **214 tests** (127 unit, 64 infrastructure, 23 integration).

## Architecture

```
Api            → Minimal API endpoints, middleware, CORS
Infrastructure → EF Core, Identity, repositories, JWT generation
Application    → MediatR handlers, DTOs, pipeline behaviors
Domain        → Entities, invariants, enums
```

See [docs/diagrams/architecture.md](docs/diagrams/architecture.md) for the full diagram.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQLite (included via `Microsoft.EntityFrameworkCore.Sqlite`)

## Setup

### 1. Environment Variables

Copy `.env.example` to `.env` (or set these directly in your shell):

```bash
# .env
PORTFOLIO_DB_PATH=Data Source=Infrastructure/portfolio-dev.db
Jwt__Secret=your-secret-key-at-least-32-chars
Jwt__Issuer=AJNPortfolio
Jwt__Audience=AJNPortfolioAdmin
Jwt__ExpirationMinutes=600
Bootstrap__Token=your-bootstrap-token
Cors__AllowedOrigins=https://yourfrontend.com,http://localhost:4321
```

**Note**: Use double underscore (`__`) for nested config keys in environment variables.

### 2. Generate Migrations

```bash
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project Api --output-dir Persistence/Migrations
dotnet ef database update --project Infrastructure --startup-project Api
```

### 3. Run

```bash
cd Api
dotnet run
```

API runs at `http://localhost:5239` in development (HTTP only; the port comes from `Api/Properties/launchSettings.json`). In Docker it listens on `http://localhost:8080`.

## Testing

```bash
# Unit + Integration tests
dotnet test

# Integration tests only
dotnet test tests/Api.Tests/

# With output
dotnet test --logger "console;verbosity=detailed"
```

Suite completa en verde: **214 tests** (127 de `Application.Tests`, 64 de `Infrastructure.Tests` y 23 de integración de `Api.Tests`). El CI los corre en cada PR y en cada push a `main`.

## Docker

### 1. Con Docker Compose (recomendado)

```bash
docker compose up --build
```

La API queda en `http://localhost:8080` y la base SQLite se persiste en el volumen `api-data`.

> Los valores por defecto de `docker-compose.yml` son **solo para desarrollo local**. Son públicos: nunca los uses en producción.

### 2. Con `docker build` + `docker run`

```bash
docker build -t ajn-portfolio-backend:local .

docker run -d --name ajn-portfolio-backend \
  -p 8080:8080 \
  -e Jwt__Secret=local-dev-secret-at-least-32-characters-long \
  -e Jwt__Issuer=AJNPortfolio \
  -e Jwt__Audience=AJNPortfolioAdmin \
  -e Bootstrap__Token=local-bootstrap-token \
  -e Cors__AllowedOrigins=http://localhost:4321 \
  ajn-portfolio-backend:local
```

### 3. Variables de entorno

| Variable | Obligatoria | Descripción |
|----------|:-----------:|-------------|
| `PORTFOLIO_DB_PATH` | Sí (en Docker) | Connection string SQLite de la base (`Data Source=/data/portfolio.db`). En local es opcional: la ruta se ancla sola al repo |
| `Jwt__Secret` | Sí | Clave de firma del JWT (mínimo 32 caracteres) |
| `Jwt__Issuer` | Sí | Claim `iss` del JWT |
| `Jwt__Audience` | Sí | Claim `aud` del JWT |
| `Jwt__ExpirationMinutes` | No | Vida del token (600 por defecto) |
| `Bootstrap__Token` | No | Token para registrar el primer admin |
| `Cors__AllowedOrigins` | No | Orígenes permitidos, separados por comas |
| `Database__AutoMigrate` | No | Aplica las migraciones al arrancar (`true` en la imagen, `false` en `appsettings.json`) |

Los secretos **no** se hornean en la imagen: se inyectan por variable de entorno en cada `docker run` o desde el dashboard de Render.

### 4. Dónde vive la base de datos

| Escenario | Ubicación |
|-----------|-----------|
| `docker compose up` | Volumen nombrado `api-data`, montado en `/data` |
| `docker run -v ./data:/data` | Carpeta `./data` del host (el entrypoint le ajusta el propietario para el usuario `app`) |
| `docker run` sin volumen | Se pierde al borrar el contenedor |

Resetear los datos:

```bash
docker compose down -v                 # borra el volumen y la base
docker rm -f ajn-portfolio-backend     # borra el contenedor
```

### 5. Conectar el frontend (Astro)

```bash
PUBLIC_API_BASE_URL=http://localhost:8080
```

## CI/CD

| Workflow | Cuándo corre | Qué hace |
|----------|--------------|----------|
| `.github/workflows/ci.yml` | Pull requests y push a `main` | `restore` + `build` + **214 tests**, construye la imagen Docker y levanta un contenedor de prueba (*smoke test*) contra `/health` y `/projects` |
| `.github/workflows/cd.yml` | Push a `main` (y manual) | Publica la imagen en GHCR (`ghcr.io/<repo>`) con tags `latest` y `sha`, y opcionalmente dispara el deploy de Render |

**Secretos de GitHub**:

| Nombre | Tipo | Uso |
|--------|------|-----|
| `GITHUB_TOKEN` | Automático | Publicar la imagen en GHCR (requiere `packages: write`) |
| `RENDER_DEPLOY_HOOK_URL` | Secret (opcional) | Dispara el deploy de Render. Si no está configurado, el deploy se omite sin error |

**Protección de `main` (obligatoria)**: el CI solo protege el repo si `main` exige el check verde.

Repo → **Settings** → **Branches** → **Branch protection rules** → **Require status checks to pass** → marcar el check `build-test`.

## Project Structure

```
ajn-portfolio-backend/
├── Api/                    # Minimal API endpoints, Program.cs, appsettings
├── Application/            # MediatR handlers, DTOs, pipeline behaviors
│   ├── Abstractions/      # Interfaces (repositories, auth services)
│   ├── Behaviors/         # MediatR pipeline: Validation, Logging, Authorization, Audit
│   ├── Authentication/     # Auth commands, queries, DTOs
│   ├── Projects/           # Project commands, queries, DTOs
│   └── Contributions/      # Contribution commands, queries, DTOs
├── Domain/                 # Entities, enums, value objects, helpers
│   ├── Entities/          # Project, Contribution, ApplicationUser
│   ├── Enums/             # State enum
│   ├── ValueObjects/      # TechnologySet (rules of the project technologies)
│   └── Helpers/            # Validation helpers
├── Infrastructure/         # EF Core, Identity, repositories
│   ├── Authentication/    # JWT token generator, validators
│   ├── Persistence/       # DbContext, repositories, migrations, seed data
│   └── DependencyInjection.cs
├── tests/
│   ├── Application.Tests/
│   ├── Infrastructure.Tests/
│   └── Api.Tests/         # Integration tests (NUnit)
├── docs/
│   └── diagrams/
└── README.md
```

## API Endpoints

### Public

| Method | Path | Description |
|--------|------|-------------|
| GET | `/projects` | List all active projects (paginated) |
| GET | `/projects/{slug}` | Get project by slug |
| GET | `/contributions` | List all active contributions (paginated) |
| GET | `/contributions/{slug}` | Get contribution by slug |

### Auth (Admin)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/admin/auth/register` | Register first admin (requires `X-Bootstrap-Token` header) |
| POST | `/admin/auth/login` | Login with credentials |
| POST | `/admin/auth/logout` | Clear auth cookie |

### Admin CRUD (requires auth cookie with Admin role)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/admin/projects` | List all projects (including deleted) |
| POST | `/admin/projects` | Create new project |
| PUT | `/admin/projects/{slug}` | Update project |
| DELETE | `/admin/projects/{slug}` | Soft-delete project |
| POST | `/admin/projects/{slug}/restore` | Restore deleted project |
| GET | `/admin/contributions` | List all contributions |
| POST | `/admin/contributions` | Create new contribution |
| PUT | `/admin/contributions/{slug}` | Update contribution |
| DELETE | `/admin/contributions/{slug}` | Soft-delete contribution |
| POST | `/admin/contributions/{slug}/restore` | Restore deleted contribution |

## Auth Flow

1. User calls `POST /admin/auth/register` with `X-Bootstrap-Token` header (first admin only)
2. Or `POST /admin/auth/login` with email + password
3. Server validates credentials, generates JWT, stores it in `auth_token` cookie (httpOnly, Secure, SameSite=Strict)
4. Subsequent requests to admin endpoints automatically send the cookie
5. JWT is validated by `JwtBearerEvents.OnMessageReceived` which extracts the token from the cookie

The JWT contains: `sub` (user ID), `email`, `role` (Admin), `jti` (unique token ID).

## Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `Jwt:Secret` | JWT signing key (min 32 chars) | Required |
| `Jwt:Issuer` | JWT issuer claim | Required |
| `Jwt:Audience` | JWT audience claim | Required |
| `Jwt:ExpirationMinutes` | Token lifetime | 600 (10 hours) |
| `Bootstrap:Token` | Token for initial admin registration | Required |
| `Cors:AllowedOrigins` | Comma-separated allowed origins | Empty (allows any) |

## Database

SQLite during development. The path is anchored to the repository root (`Infrastructure/portfolio-dev.db`) so the runtime and `dotnet ef` always hit the same file, regardless of the working directory. Override it with the `PORTFOLIO_DB_PATH` environment variable, which takes a full SQLite connection string.

Production (planned): PostgreSQL on Render/Turso.

## Known Issues

1. **Hardcoded JWT secret and bootstrap token** in `appsettings.Development.json` — development-only placeholders. Docker and Render inject the real values as environment variables.

## Roadmap

See [docs/roadmap-implementacion.md](docs/roadmap-implementacion.md) for the full implementation plan. All 16 phases are documented there.

## Tech Stack

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core 10 + SQLite
- ASP.NET Identity
- MediatR 14.x
- FluentValidation 12.x
- JWT Bearer authentication
