using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;

    public ProjectRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Project?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Projects.FirstOrDefaultAsync(p => p.Slug == slug, ct);
    }

    public async Task<IReadOnlyList<Project>> GetAllNotDeletedAsync(CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            // CreatedAt no es único: ThenByDescending(Id) desempata para que el orden
            // sea total y la paginación no repita ni pierda filas.
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetAllAndDeletedAsync(CancellationToken ct = default)
    {
        return await _db.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            // CreatedAt no es único: ThenByDescending(Id) desempata para que el orden
            // sea total y la paginación no repita ni pierda filas.
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetPagedNotDeletedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            // CreatedAt no es único: ThenByDescending(Id) desempata para que el corte
            // de página sea estable entre llamadas y una fila no caiga en dos páginas.
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountNotDeletedAsync(CancellationToken ct = default)
    {
        return await _db.Projects.CountAsync(ct);
    }

    public async Task<Result> AddAsync(Project project, CancellationToken ct = default)
    {
        try
        {
            await _db.Projects.AddAsync(project, ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(new Error("PersistenceError", ex.Message));
        }
    }

    public Task<Result> UpdateAsync(Project project, CancellationToken ct = default)
    {
        //la entidad ya viene tracked (la obtuvimos con GetBySlugAsync)
        //EF detecta cambios automaticamente, no hace falta attach.
        _db.Projects.Update(project);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SoftDeleteAsync(Project project, CancellationToken ct = default)
    {
        project.MarkAsDeleted(); // la entidad es la duena de la regla
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RestoreAsync(Project project, CancellationToken ct = default)
    {
        project.Restore();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> HardDeleteAsync(Project project, CancellationToken ct = default)
    {
        _db.Projects.Remove(project);
        return Task.FromResult(Result.Success());
    }
}
