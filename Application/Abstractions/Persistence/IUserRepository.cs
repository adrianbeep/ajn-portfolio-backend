using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

        Task AddAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default);

        Task<bool> CheckPasswordAsync(ApplicationUser applicationUser, string password, CancellationToken ct = default);
    }
}
