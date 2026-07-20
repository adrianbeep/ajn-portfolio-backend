using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _userManager.FindByEmailAsync(email);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = _userManager.NormalizeEmail(email);
        return await _db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct);
    }

    public async Task<Result> AddAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default)
    {
        var identityResult = await _userManager.CreateAsync(applicationUser, password);

        if (identityResult.Succeeded)
            return Result.Success();

        var description = string.Join("; ", identityResult.Errors.Select(e => e.Description));
        return Result.Failure(new Error("IdentityError", description));
    }

    public Task<bool> CheckPasswordAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default)
        => _userManager.CheckPasswordAsync(applicationUser, password);
}