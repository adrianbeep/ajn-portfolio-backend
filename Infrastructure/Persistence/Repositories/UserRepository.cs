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
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserRepository(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
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

    public async Task<Result> AddToRoleAsync(ApplicationUser applicationUser, string role, CancellationToken ct = default)
    {
        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!roleResult.Succeeded)
            {
                var description = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return Result.Failure(new Error("RoleCreationError", description));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(applicationUser, role);
        if (!addResult.Succeeded)
        {
            var description = string.Join("; ", addResult.Errors.Select(e => e.Description));
            return Result.Failure(new Error("AddToRoleError", description));
        }

        return Result.Success();
    }
}