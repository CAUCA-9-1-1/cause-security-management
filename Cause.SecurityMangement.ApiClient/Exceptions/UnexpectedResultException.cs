using System;

namespace Cauca.ApiClient.Exceptions
{
    public class UnexpectedResultException : ApiClientException
    {
        public UnexpectedResultException(string url, string content, Exception innerException = null) 
            : base($"API didn't return the expected result for '{url}'. Body content was: {content}.", innerException)
        {
        }
    }
}