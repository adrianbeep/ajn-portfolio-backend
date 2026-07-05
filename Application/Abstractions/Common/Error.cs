using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Common
{
    public sealed record Error(
        string Code,
        string Message);
}
