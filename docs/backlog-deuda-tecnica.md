# Backlog de Deuda Técnica — Portfolio Backend

> Documento vivo. Cada item representa una decisión consciente de "aceptar el costo" con un plan explícito de cómo pagarla.

---

## Formato de cada item

```
## [ID] Título descriptivo

**Tipo**: <kind>  (architecture | performance | security | maintainability | testing)
**Prioridad**: <priority>  (high | medium | low)
**Estado**: <state>  (acknowledged | in-progress | resolved)
**Fecha de registro**: YYYY-MM-DD
**Impacto**: Descripción concreta del problema actual
**Costo de no resolver**: Qué pasa si seguimos así
**Plan de resolución**: Pasos concretos para pagar la deuda
**Estimación**: <time>
```

---

## Backlog

---

## [TD-001] Falta de tests de integración en la API

**Tipo**: testing
**Prioridad**: high
**Estado**: acknowledged
**Fecha de registro**: 2026-06-21

**Impacto**: Actualmente solo hay tests unitarios en `Api.Tests`. No hay tests que prueben la API end-to-end con una base de datos real (o en memoria).

**Costo de no resolver**:
- Cada refactor o cambio en endpoints requiere testing manual
- Riesgo de regressions en el JSON de request/response
- No se puede hacer CI/CD confiable

**Plan de resolución**:
1. Agregar `Microsoft.AspNetCore.Mvc.Testing` al proyecto de tests
2. Configurar `WebApplicationFactory` con base de datos en memoria (SQLite o `Testcontainers`)
3. Escribir tests de integración para los endpoints críticos:
   - `GET /api/projects` (público)
   - `POST /api/auth/login` (validar 200 y 401)
   - `POST /api/projects` con JWT válido
   - `GET /api/projects/{slug}` (200 y 404)
4. Correr en CI (GitHub Actions) antes de cada merge

**Estimación**: ~2 días

---

## [TD-002] El JWT secret está hardcodeado en appsettings.json.example

**Tipo**: security
**Prioridad**: high
**Estado**: acknowledged

**Impacto**: El `appsettings.json.example` contiene un `Jwt__Secret` de ejemplo que podría ser usado tal cual en producción si alguien no cambia el valor.

**Costo de no resolver**:
- Un deploy con el secret de ejemplo significaría tokens JWT firmados con una clave pública y predecible
- Cualquiera que lea el repo podría falsificar tokens de admin

**Plan de resolución**:
1. Remover `Jwt__Secret` del `appsettings.json.example` completamente
2. Documentar que el secret debe configurarse via environment variable
3. Agregar validación en startup: si `Jwt__Secret` es null o menor a 32 caracteres, lanzar excepción con mensaje claro
4. En Render: configurar `Jwt__Secret` como environment variable encriptada
5. Actualizar el Runbook con la sección de cómo rotar el secret

**Estimación**: ~2 horas

---

## [TD-003] Falta de rate limiting en producción

**Tipo**: security
**Prioridad**: medium
**Estado**: acknowledged

**Impacto**: No hay rate limiting implementado a nivel de API. Un atacante o un cliente mal configurado podría hacer miles de requests por minuto sin restricciones.

**Costo de no resolver**:
- Exposición a ataques de fuerza bruta en `/api/auth/login`
- Exposición a DDoS accidental o intencional
- Costos unexpectedly altos en Render por uso excesivo de CPU

**Plan de resolución**:
1. Implementar `AspNetCoreRateLimit` middleware:
   ```bash
   dotnet add package AspNetCoreRateLimit
   ```
2. Configurar reglas por IP y por endpoint en `appsettings.json`:
   - `/api/auth/login`: 5 requests/minuto/IP
   - `/api/projects`: 100 requests/minuto/IP
3. Responder con `429 Too Many Requests` cuando se exceda el límite
4. Agregar header `Retry-After` en la respuesta 429
5. Documentar en el Runbook de Operaciones

**Alternativa**: Usar el rate limiting de Render (más simple pero menos granular).

**Estimación**: ~4 horas

---

## [TD-004] Slug generation automática no existe (requiere slug manual)

**Tipo**: maintainability
**Prioridad**: medium
**Estado**: acknowledged

**Impacto**: El campo `slug` es obligatorio al crear un proyecto. El usuario debe inventar un slug válido (lowercase, guiones, sin espacios). No hay fallback a un slug generado automáticamente desde el título.

**Costo de no resolver**:
- UX pobre: si el usuario pone título "Mi Portfolio Increíble", tiene que pensar cómo escribir "mi-portfolio-increible"
- Slugs mal formados pueden romper rutas del frontend

**Plan de resolución**:
1. Crear helper `SlugGenerator.Generate(title)`:
   - Lowercase
   - Replace spaces with hyphens
   - Remove special chars
   - Handle duplicates appending `-1`, `-2`
2. Modificar `CreateProjectRequest` para hacer `Slug` opcional (`string?`)
3. Si `Slug` es null o vacío, generar automáticamente desde `Title`
4. Agregar test para `SlugGenerator` con casos: "Mi Portfolio!", "Test   con   espacios", "ÁÉÍÓÚ"
5. Actualizar el DTO en OpenAPI

**Estimación**: ~4 horas

---

## [TD-005] No hay soft delete en Projects

**Tipo**: architecture
**Prioridad**: low
**Estado**: acknowledged

**Impacto**: `DELETE /api/projects/{slug}` elimina el registro de la base de datos permanentemente. No hay forma de recuperar un proyecto eliminado.

**Costo de no resolver**:
- Un delete accidental significa pérdida de datos permanente
- No se puede "despublicar" algo que ya se eliminó

**Plan de resolución** (cuando el budget lo justifique):
1. Agregar campo `deleted_at: datetime?` a `Project`
2. Modificar queries para filtrar `deleted_at IS NOT NULL`
3. Cambiar `DELETE` a actualizar `deleted_at` en lugar de `Remove()`
4. Crear endpoint `POST /api/projects/{slug}/restore` para admins
5. Agregar job de purga para eliminar registros con `deleted_at` anterior a 30 días

**Estimación**: ~1 día

---

## [TD-006] Falta paginación real en GET /projects

**Tipo**: maintainability
**Prioridad**: medium
**Estado**: acknowledged

**Impacto**: El endpoint `GET /api/projects?limit=X&offset=Y` retorna todos los proyectos y luego hace `Skip()` y `Take()` en memoria (client-side pagination), no en la DB.

**Costo de no resolver**:
- Con 1000+ proyectos, el endpoint será lento y consumirá mucha memoria
- El header `Total` de la respuesta no es confiable si hay filtros

**Plan de resolución**:
1. Reemplazar `ToListAsync()` + `Skip()`/`Take()` por:
   ```csharp
   var total = await _context.Projects.CountAsync();
   var projects = await _context.Projects
       .OrderByDescending(p => p.CreatedAt)
       .Skip(offset)
       .Take(limit)
       .ToListAsync();
   ```
2. Verificar que los índices en `slug` y `created_at` existan en la migración
3. Si no existen, crear migración con:
   ```csharp
   migrationBuilder.CreateIndex(nameof(Project.Slug));
   migrationBuilder.CreateIndex(nameof(Project.CreatedAt));
   ```

**Estimación**: ~2 horas

---

## [TD-007] Falta health check endpoint

**Tipo**: maintainability
**Prioridad**: medium
**Estado**: acknowledged

**Impacto**: Render y Vercel no tienen forma fácil de saber si la API está "viva" y la DB reachable sin hacer un request a `/api/projects` que requiere lógica de negocio.

**Costo de no resolver**:
- Difficulty para que Render detecte si la app está realmente healthy
- Alerts falsos en monitoring

**Plan de resolución**:
1. Agregar endpoint:
   ```csharp
   [HttpGet("/health")]
   public async Task<IActionResult> Health()
   {
       try
       {
           await _context.Database.CanConnectAsync();
           return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
       }
       catch
       {
           return StatusCode(503, new { status = "unhealthy", timestamp = DateTime.UtcNow });
       }
   }
   ```
2. Configurar Render para hacer health check en `/health`
3. Agregar al Runbook de Operaciones

**Estimación**: ~1 hora

---

## [TD-008] No hay logging estructurado

**Tipo**: maintainability
**Prioridad**: low
**Estado**: acknowledged

**Impacto**: Los logs van a stdout como strings libres. No hay structured logging (JSON) con campos como `userId`, `requestId`, `duration`.

**Costo de no resolver**:
- Dificultad para filtrar y buscar en logs en producción (Render logs)
- Imposible correlacionar requests entre servicios
- No se pueden hacer métricas de latency por endpoint

**Plan de resolución**:
1. Agregar `Serilog`:
   ```bash
   dotnet add package Serilog.AspNetCore
   dotnet add package Serilog.Sinks.Console
   dotnet add package Serilog.Formatting.Compact
   ```
2. Configurar en `Program.cs`:
   ```csharp
   Log.Logger = new LoggerConfiguration()
       .MinimumLevel.Information()
       .Enrich.FromLogContext()
       .WriteTo.Console(new CompactJsonFormatter())
       .CreateLogger();
   ```
3. En cada endpoint, usar `Log.Information("GetProject called with slug={Slug}", slug)` en lugar de `Console.WriteLine`

**Estimación**: ~3 horas

---

## [TD-009] No hay CI/CD (GitHub Actions)

**Tipo**: testing
**Prioridad**: high
**Estado**: resolved
**Fecha de registro**: 2026-09-02
**Fecha de resolución**: 2026-11-15

**Impacto**: No hay ningún workflow en `.github/workflows/`. Cada push directo a `main` entra sin que nadie valide nada en la nube. Los tests solo corren localmente cuando alguien los ejecuta a mano. No hay deploy automático a Render.

**Costo de no resolver**:
- Push con tests rotos pasa desapercibido (ya pasó: commit `0887cdb` eliminó 9 tests rotos sin que nada lo detectara antes)
- No hay forma de bloquear merges con tests fallando
- Deploy a producción requiere pasos manuales propensos a error

**Plan de resolución**:
1. Crear `.github/workflows/ci.yml` que:
   - Levanta runner Ubuntu
   - Instala .NET 10 SDK
   - Corre `dotnet restore && dotnet build && dotnet test`
2. Activar branch protection en `main` (Settings → Branches → Require status checks)
3. (Opcional) Segundo workflow `cd.yml` que deploya a Render cuando CI pasa y hay push a `main`
4. (Opcional) Adoptar flujo de branches feature/* + PRs para no pushear directo a main

**Estimación**: ~2 horas (solo CI), +1h con CD a Render

**Resolución**: Se creó la cadena completa de CI/CD sobre GitHub Actions:

| Archivo | Qué aporta |
|---------|-----------|
| `.github/workflows/ci.yml` | Build + 121 tests en cada PR y push a `main`, build de la imagen y smoke test del contenedor |
| `.github/workflows/cd.yml` | Publica la imagen en GHCR y dispara el deploy hook de Render (opcional) |
| `Dockerfile` | Imagen multi-stage (.NET 10 SDK → ASP.NET 10) con healthcheck y entrada por `docker-entrypoint.sh` |
| `docker-entrypoint.sh` | Prepara `/data` y baja privilegios del usuario `root` al usuario `app` |
| `docker-compose.yml` | Servicio local con volumen `api-data` y healthcheck |
| `.dockerignore` | La imagen no lleva tests, docs, bases locales ni historial de Git |
| `render.yaml` | Blueprint de Render con disco persistente de 1 GB en `/data` |

Además, la API recibió el endpoint `GET /health` (usado por el `HEALTHCHECK` del contenedor y por el smoke test de CI) y la auto-migración de EF Core controlled por `Database:AutoMigrate` (`Api/Program.cs` + `Api/appsettings.json`), que es lo que permite que un contenedor arranque con la base ya creada.

---

## [TD-010] Logout no invalida el JWT (sigue válido hasta expirar)

**Tipo**: security
**Prioridad**: medium
**Estado**: acknowledged
**Fecha de registro**: 2026-09-02

**Impacto**: `POST /admin/auth/logout` solo borra la cookie `auth_token`. El JWT sigue siendo válido hasta su `ExpirationMinutes` (10h por default). Si alguien capturó el token antes del logout, puede seguir usándolo.

**Costo de no resolver**:
- Logout no es realmente "salir" — el token sigue activo
- Si se roba una cookie (XSS, etc.), el atacante tiene 10h de acceso incluso después del logout
- Falso sentido de seguridad para el admin

**Plan de resolución** (cuando se justifique):
- Opción A (simple): mantener una blacklist en memoria de `jti` revocados hasta su expiración
- Opción B (correcta): guardar `jti` en DB al emitir, marcar como revoked al logout
- Opción C (overkill para single-owner): refresh tokens + access tokens cortos

Para un single-owner con HTTPS + cookie httpOnly + SameSite=Strict, probablemente el riesgo sea aceptable.

**Estimación**: ~3 horas (Opción A), ~6 horas (Opción B)

---

## [TD-011] Swagger/OpenAPI UI no está expuesto

**Tipo**: maintainability
**Prioridad**: low
**Estado**: acknowledged
**Fecha de registro**: 2026-09-02

**Impacto**: `AddEndpointsApiExplorer()` está registrado pero no hay `Swashbuckle.AspNetCore` instalado ni UI de Swagger expuesta. El archivo `Api/Api.http` cubre testing manual pero no hay docs interactivas.

**Costo de no resolver**:
- Frontend devs tienen que leer el README o el código para conocer los endpoints
- No hay forma fácil de probar la API desde el navegador sin escribir requests a mano
- La spec OpenAPI no se genera automáticamente

**Plan de resolución**:
1. `dotnet add package Swashbuckle.AspNetCore` en `Api/`
2. Agregar `app.UseSwagger()` y `app.UseSwaggerUI()` en `Program.cs`
3. Proteger `/swagger` con la misma auth de admin (solo accesible logueado)
4. Documentar cada endpoint con summaries de Minimal API (`WithSummary`, `WithDescription`)

**Estimación**: ~1 hora

---

## [TD-012] No hay dump schema de EF contra PostgreSQL para validar

**Tipo**: testing
**Prioridad**: low
**Estado**: acknowledged
**Fecha de registro**: 2026-09-02

**Impacto**: D6 dice "PostgreSQL en producción" pero el proyecto solo se probó contra SQLite. No hay tests E2E ni migraciones generadas contra Postgres. Si Postgres tiene features incompatibles (tipos, constraints, collations), nos enteramos en deploy.

**Costo de no resolver**:
- Deploy a Render con Postgres podría fallar por incompatibilidad de schema
- Queries que funcionan en SQLite podrían ser lentas o incorrectas en Postgres
- Índices únicos, FKs y tipos `TEXT` vs `VARCHAR` se comportan distinto

**Plan de resolución** (cuando haya presupuesto de Postgres para tests):
1. Levantar Postgres en CI con `services:` de GitHub Actions
2. Apuntar `ConnectionStrings__DefaultConnection` al Postgres del CI
3. Correr `dotnet ef migrations script` y comparar contra `EnsureCreated`
4. (Opcional) Tests E2E reales contra Postgres

**Estimación**: ~4 horas

---

## [TD-013] Los finales de linea del codigo C# varian por plataforma

**Tipo**: maintainability
**Prioridad**: high
**Estado**: resolved
**Fecha de registro**: 2026-11-18
**Fecha de resolución**: 2026-11-18

**Impacto**: `.gitattributes` normalizaba `*.sh`, `*.yml`, `*.yaml` y `Dockerfile` a LF, pero no `*.cs`. Con `core.autocrlf=true` (default de Git for Windows) el working tree en Windows queda con CRLF mientras el indice guarda LF, y CI (ubuntu) hace checkout con LF.

El contenido de un `.cs` se compila dentro de la aplicacion: los raw string literals embeben los saltos de linea tal cual estan en el archivo. El mismo codigo produce entonces dos modelos de EF Core distintos segun donde se compile, y el snapshot — que tiene que coincidir byte a byte — deja de coincidir.

**Costo de no resolver**:
- El PR #4 rompio CI tres veces con `PendingModelChangesWarning` antes de que se entendiera la causa
- El fallo es invisible en la maquina del autor (compila bien) y solo aparece en Linux
- "Build y tests" no lo detecta: los tests no ejecutan el Migrator. Solo lo caza el smoke test del contenedor, a los 3 minutos
- Cualquier `.cs` con texto multilinea es una bomba: no hace falta tocar el seed para rearmarla

**Plan de resolucion**:
1. Declarar `*.cs text eol=lf` en `.gitattributes`, que fija indice y working tree en LF
2. Normalizar el working tree del repo a LF
3. Agregar un check en CI que falle si algun archivo versionado entra al indice con CRLF, o si algun `.cs` pierde el atributo `eol=lf`
4. Normalizar ademas el valor del seed en codigo (`SeedText.Normalize`), para que el dato no dependa del checkout

**Estimacion**: ~1 hora

**Resolucion**: Quedo en tres piezas:

| Pieza | Que aporta |
|-------|-----------|
| `.gitattributes` | `*.cs text eol=lf`: indice y working tree en LF en cualquier plataforma |
| `.github/workflows/ci.yml` | Check `Verificar finales de linea` que corre antes de compilar: falla si hay CRLF en el indice o si un `.cs` pierde el atributo |
| `Infrastructure/Persistence/Seed/SeedText.cs` | `SeedText.Normalize` deja el valor del seed en LF aunque el archivo se compile con CRLF |

El check de CI es lo que convierte esto en una invariante verificada y no en una sugerencia: el error aparece en 20 segundos, en el job mas rapido, en lugar de a los 3 minutos del smoke test.

---

## Resumen del Backlog

| ID | Tipo | Prioridad | Estado | Estimación |
|----|------|-----------|--------|-----------|
| TD-001 | testing | high | acknowledged | 2 días |
| TD-002 | security | high | acknowledged | 2 horas |
| TD-003 | security | medium | acknowledged | 4 horas |
| TD-004 | maintainability | medium | acknowledged | 4 horas |
| TD-005 | architecture | low | acknowledged | 1 día |
| TD-006 | maintainability | medium | acknowledged | 2 horas |
| TD-007 | maintainability | medium | acknowledged | 1 hora |
| TD-008 | maintainability | low | acknowledged | 3 horas |
| TD-009 | testing | high | resolved | 2-3 horas |
| TD-010 | security | medium | acknowledged | 3-6 horas |
| TD-011 | maintainability | low | acknowledged | 1 hora |
| TD-012 | testing | low | acknowledged | 4 horas |
| TD-013 | maintainability | high | resolved | 1 hora |

**Total estimado**: ~5-7 días de trabajo

### Prioridad de resolución sugerida

```
Alta (primero):
  TD-002 → TD-009 → TD-001 → TD-003

Media (cuando haya tiempo):
  TD-004 → TD-006 → TD-007 → TD-010

Baja (cuando el proyecto crezca):
  TD-005 → TD-008 → TD-011 → TD-012
```

---

## Cómo usar este documento

1. **Nueva deuda identificada** → Agregar item con ID secuencial, fecha, y plan
2. **Deuda resuelta** → Cambiar estado a `resolved`, agregar fecha de resolución
3. **Deuda que ya no aplica** → Mover a la sección "Resueltas" con fecha
4. **Revisar mensualmente** → Verificar que los planes sigan vigentes

---

*Última actualización: 2026-11-18*
*Responsable: Usuario (owner del proyecto)*
