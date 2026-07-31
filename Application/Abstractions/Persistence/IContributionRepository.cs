using Domain.Entities;
using Application.Abstractions.Common;

namespace Application.Abstractions.Persistence;

public interface IContributionRepository
{
    Task<Contribution?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Contribution?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<Contribution>> GetAllNotDeletedAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Contribution>> GetPagedNotDeletedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<int> CountNotDeletedAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Contribution>> GetAllAndDeletedAsync(CancellationToken ct = default);
    
    Task<Result> AddAsync(Contribution contribution, CancellationToken ct = default);

    Task<Result> UpdateAsync(Contribution contribution, CancellationToken ct = default);

    Task<Result> SoftDeleteAsync(Contribution contribution, CancellationToken ct = default);

    Task<Result> RestoreAsync(Contribution contribution, CancellationToken ct = default);

    Task<Result> HardDeleteAsync(Contribution contribution, CancellationToken ct = default);
}
