using Application.Abstractions.Persistence;

namespace Application.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }
    public int SaveChangesReturnValue { get; set; } = 1;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(SaveChangesReturnValue);
    }
}
