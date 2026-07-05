using Application.Abstractions.Common;
using Domain.Entities;

namespace Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

        Task<Result> AddAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default);

        Task<bool> CheckPasswordAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default);
    }
}
