using System.Threading.Tasks;
using Cauca.ApiClient.Configuration;
using Cauca.ApiClient.Exceptions;
using Cauca.ApiClient.Extensions;
using Flurl;
using Flurl.Http;

namespace Cauca.ApiClient.Services
{
    public class RefreshTokenHandler
    {
        protected IConfiguration Configuration { get; set; }

        public RefreshTokenHandler(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private Url GenerateRefreshRequest()
        {
            var baseUrl = Configuration.ApiBaseUrlForAuthentication ?? Configuration.ApiBaseUrl;
            return baseUrl
                .AppendPathSegment("Authentication")
                .AppendPathSegment(GetPathForRefresh());
        }

        private Url GenerateLoginRequest()
        {
            var baseUrl = Configuration.ApiBaseUrlForAuthentication ?? Configuration.ApiBaseUrl;
            return baseUrl
                .AppendPathSegment("Authentication")
                .AppendPathSegment(GetPathForLogin());
        }

        private string GetPathForLogin() => Configuration.UseExternalSystemLogin ? "logonforexternalsystem" : "logon";
        private string GetPathForRefresh() => Configuration.UseExternalSystemLogin ? "refreshforexternalsystem" : "refresh";

        public async Task RefreshToken()
        {
            var token = await GetNewAccessToken();
            Configuration.AccessToken = token;
        }

        public async Task Login()
        {
            var login = await GetInitialAccessToken();
            Configuration.AuthorizationType = login.AuthorizationType;
            Configuration.AccessToken = login.AccessToken;
            Configuration.RefreshToken = login.RefreshToken;
        }

        private async Task<LoginResult> GetInitialAccessToken()
        {
            var request = GenerateLoginRequest();

            try
            {
                var response = await request
                    .PostJsonAsync(GetLoginBody())
                    .ReceiveJson<LoginResult>();
                return response;
            }
            catch (FlurlHttpException exception)
            {
                if (exception.Call.IsUnauthorized())
                    throw new InvalidCredentialException(Configuration.UserId);

                if (exception.Call.NoResponse())
                    throw new NoResponseApiException(exception);

                throw new InternalErrorApiException("An error occured in the login process", exception);
            }
        }

        private object GetLoginBody()
        {
            if (Configuration.UseExternalSystemLogin)
                return new {ApiKey = Configuration.UserId};
            return new {Configuration.UserId, Configuration.Password};
        }

        private async Task<string> GetNewAccessToken()
        {
            var request = GenerateRefreshRequest();

            try
            {
                var response = await request
                    .PostJsonAsync(GetRefreshTokenBody())
                    .ReceiveJson<TokenRefreshResult>();
                return response.AccessToken;
            }
            catch (FlurlHttpException exception)
            {
                if (exception.Call.RefreshTokenIsExpired() || exception.Call.RefreshTokenIsInvalid())
                { 
                    await Login();
                    return Configuration.AccessToken;
                }
            }

            return null;
        }

        private TokenRefreshResult GetRefreshTokenBody()
        {
            return new TokenRefreshResult
            {
                AccessToken = Configuration.AccessToken,
                RefreshToken = Configuration.RefreshToken
            };
        }
    }
}