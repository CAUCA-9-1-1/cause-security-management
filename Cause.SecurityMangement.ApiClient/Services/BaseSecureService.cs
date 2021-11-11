using System;
using System.Net;
using System.Threading.Tasks;
using Cauca.ApiClient.Configuration;
using Cauca.ApiClient.Extensions;
using Flurl.Http;

namespace Cauca.ApiClient.Services
{
    public abstract class BaseSecureService<TConfiguration> 
        : BaseService<TConfiguration> 
        where TConfiguration : IConfiguration
    {
        protected BaseSecureService(TConfiguration configuration) 
            : base(configuration)
        {
        }

        protected override IFlurlRequest GenerateRequest(string url)
        {
            return base.GenerateRequest(url)
                .WithHeader("Authorization", GetAuthorizationHeaderValue());
        }

        protected string GetAuthorizationHeaderValue()
        {
            return $"{Configuration.AuthorizationType} {Configuration.AccessToken}";
        }

        protected override async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> request)
        {
            await LoginWhenLoggedOut();
            try
            {
                return await request();
            }
            catch (FlurlHttpException exception)
            {
                if (exception.Call.AccessTokenIsExpired())
                {
                    return await RefreshTokenThenRetry(request);
                }

                new RestResponseValidator()
                    .ThrowExceptionForStatusCode(exception.Call.Request.Url, exception.Call.Succeeded,
                        (HttpStatusCode?)exception.Call.Response?.StatusCode, exception);
                throw;
            }
        }

        protected async Task LoginWhenLoggedOut()
        {
            if (string.IsNullOrWhiteSpace(Configuration.AccessToken))
                await new RefreshTokenHandler(Configuration)
                    .Login();
        }

        private async Task<TResult> RefreshTokenThenRetry<TResult>(Func<Task<TResult>> action)
        {
            await new RefreshTokenHandler(Configuration)
                .RefreshToken();
            return await action();
        }
    }
}