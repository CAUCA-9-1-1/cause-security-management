using System.Net.Http;
using Flurl.Http;

namespace Cause.SecurityManagement.ApiClient.Tests.Exceptions
{
    public abstract class BaseRestResponseTests
    {
        protected static HttpCall GetResponse(System.Net.HttpStatusCode code, string headerName)
        {
            var response = new HttpResponseMessage();
            response.Headers.Add(headerName, "True");
            response.StatusCode = code;
            return new HttpCall { Response = response };
        }
    }
}