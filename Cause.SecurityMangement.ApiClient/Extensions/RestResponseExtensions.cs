using System.Linq;
using System.Net;
using Flurl.Http;

namespace Cauca.ApiClient.Extensions
{
    public static class RestResponseExtensions
    {
        public const string RefreshTokenExpired = "Refresh-Token-Expired";
        public const string RefreshTokenInvalid = "Token-Invalid";
        public const string AccessTokenExpired = "Token-Expired";

        public static bool IsUnauthorized(this FlurlCall response)
        {
            return response.Response?.StatusCode == (int)HttpStatusCode.Unauthorized;
        }

        public static bool RefreshTokenIsExpired(this FlurlCall response)
        {
            return response.Response?.StatusCode == (int)HttpStatusCode.Unauthorized
                && response.Response.Headers.ToList().Any(h => h.Name == RefreshTokenExpired);
        }

        public static bool RefreshTokenIsInvalid(this FlurlCall response)
        {
            return response.Response?.StatusCode == (int)HttpStatusCode.Unauthorized
                && response.Response.Headers.ToList().Any(h => h.Name == RefreshTokenInvalid);
        }

        public static bool AccessTokenIsExpired(this FlurlCall response)
        {
            return response.Response?.StatusCode == (int)HttpStatusCode.Unauthorized
                && response.Response.Headers.ToList().Any(h => h.Name == AccessTokenExpired);
        }

        public static bool NoResponse(this FlurlCall response)
        {
            return response.Response == null;
        }
    }
}