# Contributing — Portfolio Backend

Gracias por tu interés en contribuir. Este documento explica las convenciones de código del proyecto.

---

## 1. Workflow de Contribución

```
1. Fork del repositorio
2. Crear una branch desde main:
   git checkout -b feature/nombre-de-la-feature
   git checkout -b fix/descripcion-del-bug
   git checkout -b docs/mejora-documentacion

3. Hacer los cambios siguiendo las convenciones de este documento

4. Verificar que todo compila y pasa los tests:
   dotnet build
   dotnet test

5. Commitear con mensaje descriptivo (ver sección 4)

6. Push y abrir Pull Request contra main
```

### Reglas de la Branch

| Prefijo | Uso | Ejemplo |
|---------|-----|---------|
| `feature/` | Nueva funcionalidad | `feature/agregar-búsqueda-fulltext` |
| `fix/` | Corrección de bug | `fix/corregir-slug-duplicado-en-create` |
| `docs/` | Solo documentación | `docs/actualizar-readme` |
| `refactor/` | Refactoring sin cambiar comportamiento | `refactor/extraer-auth-service` |
| `chore/` | Tareas de mantenimiento | `chore/actualizar-dependencies` |

**No hacer push directo a `main`.** La branch `main` está protegida.

---

## 2. Convenciones de Código C#

### 2.1 Nomenclatura

```csharp
// Clases: PascalCase
public class ProjectService { }

// Interfaces: I + PascalCase
public interface IProjectRepository { }
public class ProjectRepository : IProjectRepository { }

// Métodos: PascalCase
public Project GetBySlug(string slug) { }

// Variables y parámetros: camelCase
var projectTitle = "Portfolio";
void CreateProject(string projectSlug) { }

// Constantes: PascalCase (no SCREAMING_SNAKE)
public const int MaxProjectsPerPage = 100;

// Privado: _camelCase con underscore
private readonly IProjectRepository _projectRepository;

// Enum values: PascalCase
public enum ProjectStatus { Draft, Published, Archived }
```

### 2.2 Estructura de Archivos

```
src/
└── Api/
    └── Controllers/
        └── ProjectsController.cs    ← Controller + endpoint
    └── Models/
        └── Project.cs               ← Entidad de dominio
    └── DTOs/
        └── ProjectDto.cs            ← Data Transfer Objects
    └── Services/
        └── ProjectService.cs         ← Lógica de negocio
    └── Repositories/
        └── IProjectRepository.cs    ← Interfaz
        └── ProjectRepository.cs     ← Implementación
    └── Data/
        └── AppDbContext.cs          ← EF Core DbContext
    └── Middleware/
        └── JwtMiddleware.cs         ← Middleware custom
```

**Regla:** Un archivo = una clase/interface. No poner múltiples clases en el mismo archivo.

### 2.3 Estructura de un Controller

```csharp
[ApiController]
[Route("api/[controller]")]  // [controller] = "projects"
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    // Inyección por constructor
    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Lista proyectos públicos con paginación.
    /// </summary>
    /// <param name="limit">Cantidad máxima (default 10, max 100)</param>
    /// <param name="offset">Offset para paginación</param>
    [HttpGet]
    [ProducesResponseType(typeof(ProjectListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectListResponse>> GetProjects(
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0)
    {
        // TODO: Validar límites
        var result = await _projectService.GetProjectsAsync(limit, offset);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Project), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Project>> CreateProject(
        [FromBody] CreateProjectRequest request)
    {
        var project = await _projectService.CreateAsync(request);
        return CreatedAtAction(nameof(GetProject), new { slug = project.Slug }, project);
    }
}
```

### 2.4 Returns y Responses

```csharp
// Usar ActionResult<T> cuando devolvés el objeto
[HttpGet("{slug}")]
public async Task<ActionResult<Project>> GetProject(string slug)
{
    var project = await _projectService.GetBySlugAsync(slug);
    if (project == null)
        return NotFound(new ErrorResponse { Error = "PROJECT_NOT_FOUND", Message = "..." });
    return Ok(project);
}

// Usar IActionResult cuando no devolvés nada específico
[HttpDelete("{slug}")]
[Authorize]
public async Task<IActionResult> DeleteProject(string slug)
{
    await _projectService.DeleteAsync(slug);
    return NoContent();  // 204
}
```

### 2.5 Exceptions y Errores

```csharp
// Usar throw con clase custom, no Exception genérica
public class ProjectNotFoundException : Exception
{
    public string Slug { get; }
    public ProjectNotFoundException(string slug)
        : base($"Project with slug '{slug}' not found.")
    {
        Slug = slug;
    }
}

// En el controller
[HttpGet("{slug}")]
public async Task<ActionResult<Project>> GetProject(string slug)
{
    var project = await _projectService.GetBySlugAsync(slug);
    if (project == null)
        throw new ProjectNotFoundException(slug);
    return Ok(project);
}
```

### 2.6 Properties en DTOs

```csharp
public record CreateProjectRequest
{
    [Required]
    [MaxLength(70)]
    public string Title { get; init; }

    [Required]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must be lowercase with only letters, numbers and hyphens")]
    [MaxLength(70)]
    public string Slug { get; init; }

    [MaxLength(255)]
    public string? Description { get; init; }

    public string? UrlThumbnail { get; init; }

    public string? Content { get; init; }

    public bool IsPublished { get; init; }
}

// Usar init para immutabilidad en DTOs de request
// Usar get/set para entidades y DTOs de respuesta
```

### 2.7 Async/Await

```csharp
// SIEMPRE usar async/await, nunca .Result o .Wait()
public async Task<List<Project>> GetProjectsAsync()
{
    // ✅ Correcto
    return await _context.Projects.Where(p => p.IsPublished).ToListAsync();
}

// ❌ Incorrecto
return _context.Projects.Where(p => p.IsPublished).ToList();
```

### 2.8 Inyección de Dependencias

```csharp
// Registrar en Program.cs o en la clase de extensión对应
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();

// No usar singleton a menos que sea stateless
// Usar scoped para la mayoría de los servicios
// Usar transient para servicios muy livianos
```

---

## 3. Convenciones de Mensajes de Commit

Usamos **Conventional Commits**:

```
<tipo>(<alcance>): <descripción corta>

[body opcional]

[footer opcional]
```

**Tipos:**

| Tipo | Cuándo usar |
|------|-------------|
| `feat` | Nueva funcionalidad |
| `fix` | Corrección de bug |
| `docs` | Solo documentación |
| `style` | Formateo, espacios, punto y coma (sin cambio de lógica) |
| `refactor` | Refactoring sin cambio de comportamiento |
| `test` | Agregar o modificar tests |
| `chore` | Dependencias, build, CI, config |
| `perf` | Mejoras de performance |

**Ejemplos:**

```bash
feat(auth): agregar endpoint de logout

fix(projects): corregir validación de slug duplicado en create

docs(readme): actualizar sección de instalación local

refactor(repository): extraer query de proyectos publicados

chore(deps): actualizar Microsoft.AspNetCore.Authentication.JwtBearer a 8.0.0
```

**Regla**: Un commit = un cambio atómico. No mezclar features con fixes en el mismo commit.

---

## 4. Convenciones de Git

```bash
# Trabajar en branch, no en main
git checkout -b feature/mi-feature

# Mantener la branch actualizada con main
git fetch origin
git rebase origin/main

# Antes de commitear, verificar que compila
dotnet build

# Antes de abrir PR, verificar tests
dotnet test

# No hacer force push a main (nunca)
# Sí hacer force push a tu branch feature (si reorganizás commits)
```

---

## 5. Tests

### 5.1 Estructura de Tests

```csharp
// src/Api/tests/Api.Tests/
// ProjectServiceTests.cs

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _mockRepo;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _mockRepo = new Mock<IProjectRepository>();
        _service = new ProjectService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetBySlugAsync_WhenProjectExists_ReturnsProject()
    {
        // Arrange
        var slug = "portfolio-backend";
        var expected = new Project { Title = "Portfolio", Slug = slug };
        _mockRepo.Setup(r => r.GetBySlugAsync(slug)).ReturnsAsync(expected);

        // Act
        var result = await _service.GetBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetBySlugAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

        // Act
        var result = await _service.GetBySlugAsync("no-existe");

        // Assert
        Assert.Null(result);
    }
}
```

### 5.2 Reglas de Tests

- **Un test = una sola afirmación** (o un grupo de afirmaciones que testean lo mismo)
- Nombres descriptivos: `MethodName_WhenCondition_ThenExpectedResult`
- Always: Arrange → Act → Assert
- Tests que fallan deben dar un mensaje claro de por qué fallan

```bash
# Correr todos los tests
dotnet test

# Correr con coverage
dotnet test --collect:"XPlat Code Coverage"

# Correr tests de un proyecto específico
dotnet test src/Api/tests/Api.Tests/
```

---

## 6. Documentación de Código

### 6.1 XML Comments en Methods públicos

```csharp
/// <summary>
/// Obtiene un proyecto por su URL slug.
/// </summary>
/// <param name="slug">El slug único del proyecto (ej: "portfolio-backend")</param>
/// <returns>El proyecto si existe; null en caso contrario.</returns>
/// <exception cref="ProjectNotFoundException">
/// Se lanza cuando el proyecto no existe.
/// </exception>
public async Task<Project?> GetBySlugAsync(string slug)
{
    ...
}
```

### 6.2 README del módulo

Si creás un servicio o módulo complejo, agregar un `README.md` dentro de la carpeta del módulo explicando su responsabilidad y cómo usarlo.

---

## 7. Code Review Checklist (antes de abrir PR)

- [ ] `dotnet build` pasa sin warnings
- [ ] `dotnet test` pasa al 100%
- [ ] Los nombres de variables, métodos y clases siguen las convenciones
- [ ] No hay hardcoded de secrets o connection strings (usar `appsettings.json` / environment variables)
- [ ] Los nuevos endpoints tienen OpenAPI documentation (summary, responses)
- [ ] Se creó o actualizó la migración de EF Core si hay cambio de modelo
- [ ] El mensaje de commit sigue Conventional Commits

---

## 8. Issues y Feature Requests

Abrir un issue en [GitHub Issues](https://github.com/tu-usuario/portfolio-backend/issues) para:

- Reportar un bug (usar template de bug si existe)
- Solicitar una feature (usar template de feature si existe)
- Preguntar sobre decisiones de diseño

**Para bugs, incluir:**
- Pasos para reproducir
- Output esperado vs. output real
- Versión de .NET, SO, y cualquier contexto relevante

---

*Última actualización: 2026-06-21*
*Para preguntas: abrir un issue o contactar al maintainer*
