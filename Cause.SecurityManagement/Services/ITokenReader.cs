using Cause.SecurityManagement.Models;
using System;

namespace Cause.SecurityManagement.Services
{
    public interface ITokenReader
    {
        Guid GetSidFromExpiredToken(string token);
        void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token);
    }
}
