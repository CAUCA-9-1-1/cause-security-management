using Flurl.Http;

namespace Cauca.ApiClient.Exceptions
{
	public class InternalErrorApiException : ApiClientException
	{
		public InternalErrorApiException(string url) : base($"API returned a 500 (internal error) response for url '{url}'.")
		{
		}

	    public InternalErrorApiException(string url, FlurlHttpException innerException) : base($"API returned a {innerException.Call.Response.StatusCode} error code response for url '{url}'.", innerException)
	    {
	    }
    }
}