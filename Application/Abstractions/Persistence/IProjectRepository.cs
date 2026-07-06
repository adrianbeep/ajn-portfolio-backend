using Application.Abstractions.Common;
using Domain.Entities;

namespace Application.Abstractions.Persistence
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Project?> GetBySlugAsync(string slug, CancellationToken ct = default);

        Task<IReadOnlyList<Project>> GetAllNotDeletedAsync(CancellationToken ct = default);

        Task<IReadOnlyList<Project>> GetAllAndDeletedAsync(CancellationToken ct = default);

        Task<Result> AddAsync(Project project, CancellationToken ct = default);

        Task UpdateAsync(Project project, CancellationToken ct = default);

        Task RemoveAsync(Project project, CancellationToken ct = default);
    }
}
