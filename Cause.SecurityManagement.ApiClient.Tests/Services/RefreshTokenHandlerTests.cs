using System.Threading.Tasks;
using Cauca.ApiClient.Configuration;
using Cauca.ApiClient.Services;
using Cauca.ApiClient.Tests.Mocks;
using Flurl.Http.Testing;
using NUnit.Framework;

namespace Cauca.ApiClient.Tests.Services
{
    [TestFixture]
    public class RefreshTokenHandlerTests
    {
        private IConfiguration configuration;

        [SetUp]
        public void SetupTest()
        {
            configuration = new MockConfiguration
            {
                ApiBaseUrl = "http://test",
                AccessToken = "accesstoken",
                RefreshToken = "refreshtoken",
                AuthorizationType = "bearer",
                UseExternalSystemLogin = false
            };
        }

        [TestCase(true, "http://test/Authentication/refreshforexternalsystem")]
        [TestCase(false, "http://test/Authentication/refresh")]
        public async Task UrlIsCorrectlyGeneratedForExternalSystemAndNormalUserRefresh(bool useExternalSystem, string urlThatShouldHaveBeenCalled)
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new TokenRefreshResult());
            configuration.UseExternalSystemLogin = useExternalSystem;
            var tokenHandler = new RefreshTokenHandler(configuration);
            await tokenHandler.RefreshToken();

            httpTest.ShouldHaveCalled(urlThatShouldHaveBeenCalled);
        }

        [Test]
        public async Task AuthenticationUrlIsSet_WhenLoggingIn_ShouldUseBaseUrl()
        {
            var loginResult = new LoginResult { AuthorizationType = "Bearer", RefreshToken = "NewRefreshToken", AccessToken = "NewAccessToken" };
            configuration.ApiBaseUrlForAuthentication = null;
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(loginResult);
            var tokenHandler = new RefreshTokenHandler(configuration);
            
            await tokenHandler.Login();

            httpTest.ShouldHaveCalled($"{configuration.ApiBaseUrl}/Authentication/logon");
        }

        [Test]
        public async Task AuthenticationUrlIsSet_WhenLoggingIn_ShouldUseBaseAuthenticationUrl()
        {
            var loginResult = new LoginResult { AuthorizationType = "Bearer", RefreshToken = "NewRefreshToken", AccessToken = "NewAccessToken" };
            configuration.ApiBaseUrlForAuthentication = "http://test/secureApi";
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(loginResult);

            var tokenHandler = new RefreshTokenHandler(configuration);
            await tokenHandler.Login();

            httpTest.ShouldHaveCalled($"{configuration.ApiBaseUrlForAuthentication}/Authentication/logon");
        }

        [Test]
        public async Task NewAccessTokenIsCorrectlyCopiedInTheCurrentConfiguration()
        {
            var newToken = "newtoken";

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new TokenRefreshResult { AccessToken = newToken });
            var tokenHandler = new RefreshTokenHandler(configuration);
            await tokenHandler.RefreshToken();

            Assert.AreEqual(newToken, configuration.AccessToken);
        }

        [Test]
        public async Task NullIsCorrectlyReturnedForAnyOtherReason()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new TokenRefreshResult(), 404);
            var tokenHandler = new RefreshTokenHandler(configuration);
            await tokenHandler.RefreshToken();

            Assert.IsNull(configuration.AccessToken);
        }
    }
}
