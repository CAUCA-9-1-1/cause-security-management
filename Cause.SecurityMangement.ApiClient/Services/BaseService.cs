using System;
using System.Threading.Tasks;
using Cause.SecurityMangement.ApiClient.Configuration;
using Cause.SecurityMangement.ApiClient.Services.Interfaces;
using Flurl;
using Flurl.Http;

namespace Cause.SecurityMangement.ApiClient.Services
{
    public abstract class BaseService<TConfiguration> : IBaseService
        where TConfiguration : IConfiguration
    {
        protected TConfiguration Configuration { get; set; }

        protected BaseService(TConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<TResult> PostAsync<TResult>(string url, object entity)
        {
            var request = GenerateRequest(url);
            return await ExecuteAsync(() => ExecutePostAsync<TResult>(request, entity));
        }

        public async Task<TResult> PutAsync<TResult>(string url, object entity)
        {
            var request = GenerateRequest(url);
            return await ExecuteAsync(() => ExecutePutAsync<TResult>(request, entity));
        }

        public async Task<TResult> DeleteAsync<TResult>(string url)
        {
            var request = GenerateRequest(url);
            return await ExecuteAsync(() => ExecuteDeleteAsync<TResult>(request));
        }

        public async Task<TResult> GetAsync<TResult>(string url)
        {
            var request = GenerateRequest(url);
            return await ExecuteAsync(() => ExecuteGetAsync<TResult>(request));
        }

        protected virtual async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> request)
        {
            try
            {
                return await request();
            }
            catch (FlurlHttpException exception)
            {
                new RestResponseValidator()
                    .ThrowExceptionForStatusCode(request.ToString(), exception.Call.Succeeded, exception.Call.HttpStatus, exception);
                throw;
            }
        }

        protected virtual IFlurlRequest GenerateRequest(string url)
        {
            return Configuration.ApiBaseUrl
                .AppendPathSegment(url)
                .WithTimeout(TimeSpan.FromSeconds(Configuration.RequestTimeoutInSeconds));
        }

        protected async Task<TResult> ExecutePostAsync<TResult>(IFlurlRequest request, object entity)
        {
            return await request
                .PostJsonAsync(entity)
                .ReceiveJson<TResult>();
        }

        protected async Task<TResult> ExecuteGetAsync<TResult>(IFlurlRequest request)
        {
            return await request
                .GetJsonAsync<TResult>();
        }

        protected async Task<TResult> ExecutePutAsync<TResult>(IFlurlRequest request, object entity)
        {
            return await request
                .PutJsonAsync(entity)
                .ReceiveJson<TResult>();
        }

        protected async Task<TResult> ExecuteDeleteAsync<TResult>(IFlurlRequest request)
        {
            return await request
                .DeleteAsync()
                .ReceiveJson<TResult>();
        }
    }
}