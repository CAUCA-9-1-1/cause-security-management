using Cauca.ApiClient.Tests.Mocks;
using NUnit.Framework;

namespace Cauca.ApiClient.Tests.Services
{
    [TestFixture]
    public class BaseSecureClientInternalTests : MockSecureRepository
    {
        public BaseSecureClientInternalTests() : base(new MockConfiguration
        {
            ApiBaseUrl = "http://test",
            AccessToken = "Token",
            RefreshToken = "RefreshToken",
            AuthorizationType = "Mock"
        })
        {
        }

        [TestCase]
        public void AuthorizationHeaderIsCorrectlyGenerated()
        {
            Assert.AreEqual("Mock Token", GetAuthorizationHeaderValue());
        }
    }
}