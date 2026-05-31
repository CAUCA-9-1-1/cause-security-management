using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Core.Services
{
    public interface ITokenReader
    {
        string GetSidFromExpiredToken(string token);
        string GetClaimValueFromExpiredToken(string token, string type);
        void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token);
    }
}
