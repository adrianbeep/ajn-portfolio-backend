using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ContributionRepository : IContributionRepository
{
    private readonly AppDbContext _db;

    public ContributionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Contribution?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Contributions.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Contribution?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Contributions.FirstOrDefaultAsync(c => c.Slug == slug, ct);
    }

    public async Task<IReadOnlyList<Contribution>> GetAllNotDeletedAsync(CancellationToken ct = default)
    {
        return await _db.Contributions
            .AsNoTracking()
            // CreatedAt no es único: ThenByDescending(Id) desempata para que el orden
            // sea total y la paginación no repita ni pierda filas.
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);
    }
    
    public async Task<IReadOnlyList<Contribution>> GetAllAndDeletedAsync(CancellationToken ct = default)
    {
        return await _db.Contributions
            .IgnoreQueryFilters()
            .AsNoTracking()
            // CreatedAt no es único: ThenByDescending(Id) desempata para que el orden
            // sea total y la paginación no repita ni pierda filas.
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);
    }
    
    public async Task<IReadOnlyList<Contribution>> GetPagedNotDeletedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Contributions
            .AsNoTracking()
            // CreatedAt no es único: ThenByDescending(Id) desempata para que el corte
            // de página sea estable entre llamadas y una fila no caiga en dos páginas.
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
    
    public async Task<int> CountNotDeletedAsync(CancellationToken ct = default)
    {
        return await _db.Contributions.CountAsync(ct);
    }
    
    public async Task<Result> AddAsync(Contribution contribution, CancellationToken ct = default)
    {
        try
        {
            await _db.Contributions.AddAsync(contribution, ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(new Error("PersistenceError", ex.Message));
        }
    }
    
    public Task<Result> UpdateAsync(Contribution contribution, CancellationToken ct = default)
    {
        //la entidad ya viene tracked (la obtuvimos con GetBySlugAsync)
        //EF detecta cambios automaticamente, no hace falta attach.
        _db.Contributions.Update(contribution);
        return Task.FromResult(Result.Success());
    }
    
    public Task<Result> SoftDeleteAsync(Contribution contribution, CancellationToken ct = default)
    {
        contribution.MarkAsDeleted(); // la entidad es la duena de la regla
        return Task.FromResult(Result.Success());
    }
    
    public Task<Result> RestoreAsync(Contribution contribution, CancellationToken ct = default)
    {
        contribution.Restore();
        return Task.FromResult(Result.Success());
    }
    
    public Task<Result> HardDeleteAsync(Contribution contribution, CancellationToken ct = default)
    {
        _db.Contributions.Remove(contribution);
        return Task.FromResult(Result.Success());
    }
}
