using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Authentication
{
    public sealed record JwtTokenResult(
        string Token,
        DateTime ExpiresAt);
}
