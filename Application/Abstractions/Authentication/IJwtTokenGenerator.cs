using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Authentication
{
    public interface IJwtTokenGenerator
    {
        JwtTokenResult GenerateToken(ApplicationUser applicationUser, CancellationToken ct = default);
    }
}
