using System;

namespace Cauca.ApiClient.Exceptions
{
	public class NotFoundApiException : ApiClientException
	{
		public NotFoundApiException(string url, Exception innerException = null) : base($"API returned a 404 (not found) response for url '{url}'.", innerException)
		{
		}
	}
}