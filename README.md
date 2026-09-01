# AJN Portfolio Backend

ASP.NET Core Minimal API with Clean Architecture, CQRS, MediatR, and ASP.NET Identity + JWT authentication.

**Status**: Functional backend. Auth, CRUD operations, and public read endpoints work. Integration tests partially complete (16/24 passing).

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
ConnectionStrings__DefaultConnection=DataSource=portfolio.db
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

API runs at `http://localhost:5000` (HTTP only in development).

## Testing

```bash
# Unit + Integration tests
dotnet test

# Integration tests only
dotnet test tests/Api.Tests/

# With output
dotnet test --logger "console;verbosity=detailed"
```

**Known issue**: 8/24 integration tests fail due to auth cookie propagation in the test environment. The tests verify auth works correctly via browser/client but the test setup has issues with cookie handling across HttpClient instances. The failing tests are pre-existing issues in the test harness, not in the production code.

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
├── Domain/                 # Entities, enums, helpers
│   ├── Entities/          # Project, Contribution, ApplicationUser
│   ├── Enums/             # State enum
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

SQLite during development. The connection string is read from `appsettings.Development.json` or the `ConnectionStrings__DefaultConnection` environment variable.

Production (planned): PostgreSQL on Render/Turso.

## Known Issues

1. **Integration tests**: 8/24 fail due to test harness issues with cookie propagation across HttpClient instances in NUnit
2. **DER diagram** in `docs/diagrams/` is outdated (pre-dates Contribution entity and Identity tables)
3. **Pipeline behavior order** differs from roadmap specification (currently Logging → Audit → Authorization → Validation vs spec's Validation → Authorization → Logging → Audit)
4. **CurrentUser claim mapping**: reads `"sub"` claim but ASP.NET JWT remaps to `ClaimTypes.NameIdentifier` — may cause issues in audit logs
5. **Hardcoded JWT secret** in `appsettings.Development.json` — never commit production secrets

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
