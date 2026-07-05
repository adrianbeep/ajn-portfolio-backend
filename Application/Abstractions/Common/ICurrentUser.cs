using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Common
{
    public interface ICurrentUser
    {
        Guid? Id { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
    }
}
