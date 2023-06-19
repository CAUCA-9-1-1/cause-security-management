using Cause.SecurityManagement.Models;

namespace Cause.SecurityManagement.Interfaces.Services
{
    public interface ITokenReader
    {
        string GetSidFromExpiredToken(string token);
        string GetClaimValueFromExpiredToken(string token, string type);
        void ThrowExceptionWhenTokenIsNotValid(string refreshToken, BaseToken token);
    }
}
