using Cauca.ApiClient.Exceptions;
using NUnit.Framework;

namespace Cauca.ApiClient.Tests.Exceptions
{
    [TestFixture]
    public class ExceptionsTests
    {
        protected string Url = "http://www.test.com/";

        [TestCase]
        public void BadParameterApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("API returned a 400 (bad request) response for url 'http://www.test.com/'.", new BadParameterApiException(Url).Message);
        }

        [TestCase]
        public void ExpiredRefreshTokenExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("The refresh token is expired.", new ExpiredRefreshTokenException().Message);
        }

        [TestCase]
        public void ForbiddenApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("API returned a 403 (forbidden) response for url 'http://www.test.com/'.", new ForbiddenApiException(Url).Message);
        }

        [TestCase]
        public void InternalErrorApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("API returned a 500 (internal error) response for url 'http://www.test.com/'.", new InternalErrorApiException(Url).Message);
        }

        [TestCase]
        public void InvalidRefreshTokenApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("The refresh token is invalid.", new InvalidRefreshTokenException().Message);
        }

        [TestCase]
        public void NoResponseApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("API didn't return an answer in a timely manner.", new NoResponseApiException().Message);
        }

        [TestCase]
        public void NotFoundApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("API returned a 404 (not found) response for url 'http://www.test.com/'.", new NotFoundApiException(Url).Message);
        }

        [TestCase]
        public void UnauthorizedApiExceptionMessageIsCorrectlyGenerated()
        {
            Assert.AreEqual("API returned a 401 (unauthorized) response for url 'http://www.test.com/'.", new UnauthorizedApiException(Url).Message);
        }
    }
}
