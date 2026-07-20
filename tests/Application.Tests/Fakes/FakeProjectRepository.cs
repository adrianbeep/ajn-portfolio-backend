using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;

namespace Application.Tests.Fakes;

public class FakeProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = new();

    public IReadOnlyList<Project> Projects => _projects;

    public int GetBySlugCallCount { get; private set; }
    public int AddCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }
    public int SoftDeleteCallCount { get; private set; }
    public int RestoreCallCount { get; private set; }
    public int HardDeleteCallCount { get; private set; }

    public bool AddShouldFail { get; set; }
    public bool UpdateShouldFail { get; set; }
    public bool SoftDeleteShouldFail { get; set; }
    public bool RestoreShouldFail { get; set; }
    public bool HardDeleteShouldFail { get; set; }

    public void Seed(params Project[] projects) => _projects.AddRange(projects);

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));

    public Task<Project?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        GetBySlugCallCount++;
        return Task.FromResult(_projects.FirstOrDefault(p => p.Slug == slug));
    }

    public Task<IReadOnlyList<Project>> GetAllNotDeletedAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Project>)_projects.Where(p => p.DeletedAt is null).ToList());

    public Task<IReadOnlyList<Project>> GetAllAndDeletedAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Project>)_projects.ToList());

    public Task<IReadOnlyList<Project>> GetPagedNotDeletedAsync(int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Project>)_projects
            .Where(p => p.DeletedAt is null)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountNotDeletedAsync(CancellationToken ct = default)
        => Task.FromResult(_projects.Count(p => p.DeletedAt is null));

    public Task<Result> AddAsync(Project project, CancellationToken ct = default)
    {
        AddCallCount++;
        if (AddShouldFail)
            return Task.FromResult(Result.Failure(new Error("AddFailed", "Simulated failure.")));
        _projects.Add(project);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UpdateAsync(Project project, CancellationToken ct = default)
    {
        UpdateCallCount++;
        if (UpdateShouldFail)
            return Task.FromResult(Result.Failure(new Error("UpdateFailed", "Simulated failure.")));
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SoftDeleteAsync(Project project, CancellationToken ct = default)
    {
        SoftDeleteCallCount++;
        if (SoftDeleteShouldFail)
            return Task.FromResult(Result.Failure(new Error("SoftDeleteFailed", "Simulated failure.")));
        project.MarkAsDeleted();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RestoreAsync(Project project, CancellationToken ct = default)
    {
        RestoreCallCount++;
        if (RestoreShouldFail)
            return Task.FromResult(Result.Failure(new Error("RestoreFailed", "Simulated failure.")));
        project.Restore();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> HardDeleteAsync(Project project, CancellationToken ct = default)
    {
        HardDeleteCallCount++;
        if (HardDeleteShouldFail)
            return Task.FromResult(Result.Failure(new Error("HardDeleteFailed", "Simulated failure.")));
        _projects.Remove(project);
        return Task.FromResult(Result.Success());
    }
}
