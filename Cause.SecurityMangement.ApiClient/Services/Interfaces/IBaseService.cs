using System.Threading.Tasks;

namespace Cause.SecurityMangement.ApiClient.Services.Interfaces
{
    public interface IBaseService
    {
        Task<TResult> PostAsync<TResult>(string url, object entity);
        Task<TResult> PutAsync<TResult>(string url, object entity);
        Task<TResult> DeleteAsync<TResult>(string url);
        Task<TResult> GetAsync<TResult>(string url);
    }
}