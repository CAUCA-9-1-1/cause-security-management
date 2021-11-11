using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Cauca.ApiClient.Configuration;
using Cauca.ApiClient.Services.Interfaces;
using Flurl;
using Flurl.Http;

namespace Cauca.ApiClient.Services
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
            return await ExecuteAsync(() => ExecutePostAsync<TResult>(GenerateRequest(url), entity));
        }

        public async Task PostAsync(string url, object entity)
        {
            await ExecuteAsync(() => ExecutePostAsync(GenerateRequest(url), entity));
        }

        public async Task<TResult> PutAsync<TResult>(string url, object entity)
        {
            return await ExecuteAsync(() => ExecutePutAsync<TResult>(GenerateRequest(url), entity));
        }

        public async Task<TResult> DeleteAsync<TResult>(string url)
        {
            return await ExecuteAsync(() => ExecuteDeleteAsync<TResult>(GenerateRequest(url)));
        }

        public async Task<TResult> GetAsync<TResult>(string url)
        {
            return await ExecuteAsync(() => ExecuteGetAsync<TResult>(GenerateRequest(url)));
        }

        public async Task<byte[]> GetBytesAsync(string url)
        {
            return await ExecuteAsync(() => ExecuteGetBytesAsync(GenerateRequest(url)));
        }

        public async Task<Stream> GetStreamAsync(string url)
        {
            return await ExecuteAsync(() => ExecuteGetStreamAsync(GenerateRequest(url)));
        }

        public async Task<string> GetStringAsync(string url)
        {
            return await ExecuteAsync(() => ExecuteGetStringAsync(GenerateRequest(url)));
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
                    .ThrowExceptionForStatusCode(exception.Call.Request.Url, exception.Call.Succeeded, (HttpStatusCode?)exception.Call.Response?.StatusCode, exception);
                throw;
            }
        }

        protected virtual async Task ExecuteAsync(Func<Task> request)
        {
            try
            {
                await request();
            }
            catch (FlurlHttpException exception)
            {
                new RestResponseValidator()
                    .ThrowExceptionForStatusCode(exception.Call.Request.Url, exception.Call.Succeeded, (HttpStatusCode?)exception.Call.Response?.StatusCode, exception);
                throw;
            }
        }

        protected virtual IFlurlRequest GenerateRequest(string url)
        {
            return Configuration.ApiBaseUrl
                .AppendPathSegment(url)
                .WithTimeout(TimeSpan.FromSeconds(Configuration.RequestTimeoutInSeconds));
        }

        protected async Task ExecutePostAsync(IFlurlRequest request, object entity)
        {
            await request
                .PostJsonAsync(entity);
        }

        protected async Task<TResult> ExecutePostAsync<TResult>(IFlurlRequest request, object entity)
        {
            var type = typeof(TResult);
            if (type == typeof(string))
            {
                var response = await request
                    .PostJsonAsync(entity)
                    .ReceiveString();
                return (TResult)Convert.ChangeType(response, typeof(TResult));
            }
            else if (type == typeof(bool))
            {
                var response = await request
                    .PostJsonAsync(entity)
                    .ReceiveString() == "TRUE";
                return (TResult)Convert.ChangeType(response, typeof(TResult));
            }
            else if (type == typeof(int))
            {
                var response = await request
                    .PostJsonAsync(entity)
                    .ReceiveString();
                if (int.TryParse(response, out int result))
                {
                    return (TResult)Convert.ChangeType(result, typeof(TResult));
                }

                return (TResult)Convert.ChangeType(0, typeof(TResult));
            }
            else
            {
                return await request
                    .PostJsonAsync(entity)
                    .ReceiveJson<TResult>();
            }
        }

        protected async Task<TResult> ExecuteGetAsync<TResult>(IFlurlRequest request)
        {
            var type = typeof(TResult);
            if (type == typeof(string))
            {
                var response = await request.GetStringAsync();
                return (TResult) Convert.ChangeType(response, typeof(TResult));
            }
            else if (type == typeof(bool))
            {
                var response = (await request.GetStringAsync()).ToUpper() == "TRUE";
                return (TResult)Convert.ChangeType(response, typeof(TResult));
            }
            else if (type == typeof(int))
            {
                var response = await request.GetStringAsync();
                if (int.TryParse(response, out int result))
                {
                    return (TResult)Convert.ChangeType(result, typeof(TResult));
                }

                return (TResult)Convert.ChangeType(0, typeof(TResult));
            }
            else
            {
                return await request
                    .GetJsonAsync<TResult>();
            }
        }

        protected async Task<Stream> ExecuteGetStreamAsync(IFlurlRequest request)
        {
            return await request.GetStreamAsync();
        }

        protected async Task<string> ExecuteGetStringAsync(IFlurlRequest request)
        {
            return await request.GetStringAsync();
        }

        protected async Task<byte[]> ExecuteGetBytesAsync(IFlurlRequest request)
        {
            return await request.GetBytesAsync();
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