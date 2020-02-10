using System;
using System.Net;
using Cauca.ApiClient.Exceptions;

namespace Cauca.ApiClient.Services
{
    public class RestResponseValidator
    {
        public void ThrowExceptionForStatusCode(string url, bool answerReceived, HttpStatusCode? code, Exception exception)
        {
            if (code == HttpStatusCode.NotFound)
                throw new NotFoundApiException(url, exception);
            if (code == HttpStatusCode.BadRequest)
                throw new BadParameterApiException(url, exception);
            if (code == HttpStatusCode.Unauthorized)
                throw new UnauthorizedApiException(url);
            if (code == HttpStatusCode.Forbidden)
                throw new ForbiddenApiException(url);
            if (code == HttpStatusCode.InternalServerError)
                throw new InternalErrorApiException(url);
            if (!answerReceived)
                throw new NoResponseApiException(exception);
            throw exception;
        }        
    }
}