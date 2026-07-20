using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Seed;

public static class RoleSeed
{
    public const string AdminRoleName = "Admin";

    public static readonly IdentityRole<Guid> Data = new()
    {
        Id = new Guid("11111111-1111-1111-1111-111111111111"),
        Name = AdminRoleName,
        NormalizedName = AdminRoleName.ToUpperInvariant(),
        ConcurrencyStamp = "fixed-stamp-admin-role"
    };
}
