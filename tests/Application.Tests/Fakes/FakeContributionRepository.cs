using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;

namespace Application.Tests.Fakes;

public class FakeContributionRepository : IContributionRepository
{
    private readonly List<Contribution> _contributions = new();

    public IReadOnlyList<Contribution> Contributions => _contributions;

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

    public void Seed(params Contribution[] contributions) => _contributions.AddRange(contributions);

    public Task<Contribution?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_contributions.FirstOrDefault(c => c.Id == id));

    public Task<Contribution?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        GetBySlugCallCount++;
        return Task.FromResult(_contributions.FirstOrDefault(c => c.Slug == slug));
    }

    public Task<IReadOnlyList<Contribution>> GetAllNotDeletedAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Contribution>)_contributions.Where(c => c.DeletedAt is null).ToList());

    public Task<IReadOnlyList<Contribution>> GetAllAndDeletedAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Contribution>)_contributions.ToList());

    public Task<IReadOnlyList<Contribution>> GetPagedNotDeletedAsync(int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Contribution>)_contributions
            .Where(c => c.DeletedAt is null)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList());

    public Task<int> CountNotDeletedAsync(CancellationToken ct = default)
        => Task.FromResult(_contributions.Count(c => c.DeletedAt is null));

    public Task<Result> AddAsync(Contribution contribution, CancellationToken ct = default)
    {
        AddCallCount++;
        if (AddShouldFail)
            return Task.FromResult(Result.Failure(new Error("AddFailed", "Simulated failure.")));
        _contributions.Add(contribution);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UpdateAsync(Contribution project, CancellationToken ct = default)
    {
        UpdateCallCount++;
        if (UpdateShouldFail)
            return Task.FromResult(Result.Failure(new Error("UpdateFailed", "Simulated failure.")));
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SoftDeleteAsync(Contribution contribution, CancellationToken ct = default)
    {
        SoftDeleteCallCount++;
        if (SoftDeleteShouldFail)
            return Task.FromResult(Result.Failure(new Error("SoftDeleteFailed", "Simulated failure.")));
        contribution.MarkAsDeleted();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RestoreAsync(Contribution contribution, CancellationToken ct = default)
    {
        RestoreCallCount++;
        if (RestoreShouldFail)
            return Task.FromResult(Result.Failure(new Error("RestoreFailed", "Simulated failure.")));
        contribution.Restore();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> HardDeleteAsync(Contribution contribution, CancellationToken ct = default)
    {
        HardDeleteCallCount++;
        if (HardDeleteShouldFail)
            return Task.FromResult(Result.Failure(new Error("HardDeleteFailed", "Simulated failure.")));
        _contributions.Remove(contribution);
        return Task.FromResult(Result.Success());
    }
}
