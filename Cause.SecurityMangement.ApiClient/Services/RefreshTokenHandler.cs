using System.Threading.Tasks;
using Cause.SecurityMangement.ApiClient.Configuration;
using Cause.SecurityMangement.ApiClient.Exceptions;
using Cause.SecurityMangement.ApiClient.Extensions;
using Flurl;
using Flurl.Http;

namespace Cause.SecurityMangement.ApiClient.Services
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
            return Configuration.ApiBaseUrl
                .AppendPathSegment("Authentication")
                .AppendPathSegment(GetPathForRefresh());
        }

        private Url GenerateLoginRequest()
        {
            return Configuration.ApiBaseUrl
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
                    .PostJsonAsync(new {Configuration.UserId, Configuration.Password})
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
                    await Login();
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