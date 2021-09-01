using System.Net.Http;
using Flurl.Http;

namespace Cauca.ApiClient.Tests.Exceptions
{
    public abstract class BaseRestResponseTests
    {
        protected static FlurlCall GetResponse(System.Net.HttpStatusCode code, string headerName)
        {
            var response = new HttpResponseMessage();
            response.Headers.Add(headerName, "True");
            response.StatusCode = code;
            return new FlurlCall { Response = new FlurlResponse(response) };
        }
    }
}