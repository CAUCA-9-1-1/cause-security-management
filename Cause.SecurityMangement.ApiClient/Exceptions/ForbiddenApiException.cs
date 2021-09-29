using Flurl.Http;

namespace Cauca.ApiClient.Exceptions
{
	public class ForbiddenApiException : ApiClientException
	{
		public ForbiddenApiException(string url) : base($"API returned a 403 (forbidden) response for url '{url}'.")
		{
		}

		public ForbiddenApiException(string url, FlurlHttpException innerException) : base($"API returned a 403 (forbidden) response for url '{url}'.", innerException)
		{
		}
	}
}