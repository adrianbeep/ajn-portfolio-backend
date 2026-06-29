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

**Total estimado**: ~3.5 días de trabajo

### Prioridad de resolución sugerida

```
Alta (primero):
  TD-002 → TD-001 → TD-003

Media (cuando haya tiempo):
  TD-004 → TD-006 → TD-007

Baja (cuando el proyecto crezca):
  TD-005 → TD-008
```

---

## Cómo usar este documento

1. **Nueva deuda identificada** → Agregar item con ID secuencial, fecha, y plan
2. **Deuda resuelta** → Cambiar estado a `resolved`, agregar fecha de resolución
3. **Deuda que ya no aplica** → Mover a la sección "Resueltas" con fecha
4. **Revisar mensualmente** → Verificar que los planes sigan vigentes

---

*Última actualización: 2026-06-21*
*Responsable: Usuario (owner del proyecto)*
