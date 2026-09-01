using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;

namespace Application.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<ApplicationUser> _users = new();

    public IReadOnlyList<ApplicationUser> Users => _users;

    public int GetByEmailCallCount { get; private set; }
    public int ExistsByEmailCallCount { get; private set; }
    public int AddCallCount { get; private set; }
    public int CheckPasswordCallCount { get; private set; }

    public bool AddShouldFail { get; set; }
    public bool CheckPasswordReturnValue { get; set; } = true;

    public void Seed(params ApplicationUser[] users) => _users.AddRange(users);

    public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        GetByEmailCallCount++;
        return Task.FromResult(_users.FirstOrDefault(u => u.Email == email));
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        ExistsByEmailCallCount++;
        return Task.FromResult(_users.Any(u => u.Email == email));
    }

    public Task<Result> AddAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default)
    {
        AddCallCount++;
        if (AddShouldFail)
            return Task.FromResult(Result.Failure(new Error("EmailAlreadyExists", "Simulated failure.")));
        _users.Add(applicationUser);
        return Task.FromResult(Result.Success());
    }

    public Task<bool> CheckPasswordAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default)
    {
        CheckPasswordCallCount++;
        return Task.FromResult(CheckPasswordReturnValue);
    }

    public Task<Result> AddToRoleAsync(ApplicationUser applicationUser, string role, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }
}
