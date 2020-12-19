using System;
using System.Net;
using Cauca.ApiClient.Exceptions;
using Cauca.ApiClient.Services;
using NUnit.Framework;

namespace Cauca.ApiClient.Tests.Exceptions
{
    [TestFixture]
    public class RestResponseValidatorTests : BaseRestResponseTests
    {
        [Test]
        public void CorrectlyThrowsNotFoundException()
        {
            Assert.Throws<NotFoundApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", true, HttpStatusCode.NotFound, new Exception("Test")));
        }

        [Test]
        public void CorrectlyThrowsBadParameterException()
        {
            Assert.Throws<BadParameterApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", true, HttpStatusCode.BadRequest, new Exception("Test")));
        }

        [Test]
        public void CorrectlyThrowsUnauthorizedException()
        {
            Assert.Throws<UnauthorizedApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", true, HttpStatusCode.Unauthorized, new Exception("Test")));
        }

        [Test]
        public void CorrectlyThrowsForbiddenException()
        {
            Assert.Throws<ForbiddenApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", true, HttpStatusCode.Forbidden, new Exception("Test")));
        }

        [Test]
        public void CorrectlyThrowsInternalErrorException()
        {
            Assert.Throws<InternalErrorApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", true, HttpStatusCode.InternalServerError, new Exception("Test")));
        }

        [Test]
        public void CorrectlyThrowsNoResponseForZeroStatusCode()
        {
            Assert.Throws<NoResponseApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", false, 0, new Exception("Test")));
        }

        [Test]
        public void CorrectlyThrowsNoResponseForResponseStatusTimedOut()
        {
            Assert.Throws<NoResponseApiException>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", false, HttpStatusCode.RequestTimeout, new Exception("Test")));
        }

        [Test]
        public void DoesNoThrowExceptionWhenNoErrors()
        {
            Assert.Throws<Exception>(() => new RestResponseValidator().ThrowExceptionForStatusCode("test", true, HttpStatusCode.OK, new Exception("Test")));
        }
    }
}