using System.Net.Http;
using System.Threading.Tasks;
using Cauca.ApiClient;
using Cauca.ApiClient.Tests.Mocks;
using Flurl.Http.Testing;
using NUnit.Framework;

namespace Cauca.ApiClient.Tests.Services
{
    [TestFixture]
    public class BaseSecureClientTests
    {
        private MockConfiguration configuration;

        [SetUp]
        public void SetupTest()
        {
            configuration = new MockConfiguration
            {
                ApiBaseUrl = "http://test",
                AccessToken = "Token",
                RefreshToken = "RefreshToken",
                AuthorizationType = "Mock"
            };
        }

        [TestCase]
        public async Task RequestIsCorrectlyExecuted()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new MockResponse());

                var country = new MockEntity();
                var repo = new MockSecureRepository(configuration);
                await repo.PostAsync<MockResponse>("mock", country);

                httpTest.ShouldHaveCalled("http://test/mock")
                    .WithRequestJson(country)
                    .WithVerb(HttpMethod.Post)
                    .Times(1);
            }
        }

        [TestCase]
        public async Task RequestLoginBeforeExecutingWhenNotLoggedIn()
        {
            configuration.AccessToken = null;
            configuration.RefreshToken = null;

            using (var httpTest = new HttpTest())
            {
                httpTest
                    .RespondWithJson(new LoginResult { AuthorizationType = "Bearer", RefreshToken = "NewRefreshToken", AccessToken = "NewAccessToken" })
                    .RespondWithJson(new MockResponse());

                var country = new MockEntity();
                var repo = new MockSecureRepository(configuration);
                await repo.PostAsync<MockResponse>("mock", country);

                httpTest.ShouldHaveCalled("http://test/Authentication/logon")
                    .WithVerb(HttpMethod.Post)
                    .Times(1);

                httpTest.ShouldHaveCalled("http://test/mock")
                    .WithRequestJson(country)
                    .WithVerb(HttpMethod.Post)
                    .WithHeader("Authorization", "Bearer NewAccessToken")
                    .With(call => call.Response.StatusCode == System.Net.HttpStatusCode.OK)
                    .Times(1);
            }
        }

        [TestCase]
        public async Task LoginCorrectlySetAccessAndRefreshToken()
        {
            configuration.AccessToken = null;
            configuration.RefreshToken = null;

            using (var httpTest = new HttpTest())
            {
                httpTest
                    .RespondWithJson(new LoginResult { AuthorizationType = "Bearer", RefreshToken = "NewRefreshToken", AccessToken = "NewAccessToken" })
                    .RespondWithJson(new MockResponse());

                var country = new MockEntity();
                var repo = new MockSecureRepository(configuration);
                await repo.PostAsync<MockResponse>("mock", country);

                Assert.AreEqual("NewRefreshToken", configuration.RefreshToken);
                Assert.AreEqual("NewAccessToken", configuration.AccessToken);
            }
        }


        [TestCase]
        public async Task RequestRefreshTokenThenRetryWhenItsExpired()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest
                    .RespondWithJson(new MockResponse(), 401, new { Token_Expired = "True" })
                    .RespondWithJson(new TokenRefreshResult { AccessToken = "NewToken" })
                    .RespondWithJson(new MockResponse());

                var country = new MockEntity();
                var repo = new MockSecureRepository(configuration);
                await repo.PostAsync<MockResponse>("mock", country);

                httpTest.ShouldHaveCalled("http://test/mock")
                    .WithRequestJson(country)
                    .WithVerb(HttpMethod.Post)
                    .With(call => call.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    .Times(1);

                httpTest.ShouldHaveCalled("http://test/Authentication/refresh")
                    .WithVerb(HttpMethod.Post)
                    .Times(1);

                httpTest.ShouldHaveCalled("http://test/mock")
                    .WithRequestJson(country)
                    .WithVerb(HttpMethod.Post)
                    .With(call => call.Response.StatusCode == System.Net.HttpStatusCode.OK)
                    .Times(1);
            }
        }

        [TestCase]
        public async Task RequestLogBackInWhenRefreshTokenAndAccessTokenAreExpired()
        {
            var loginResult = new LoginResult { AuthorizationType = "Bearer", RefreshToken = "NewRefreshToken", AccessToken = "NewAccessToken" };
            using (var httpTest = new HttpTest())
            {
                httpTest
                    .RespondWithJson(new MockResponse(), 401, new { Token_Expired = "True" })
                    .RespondWithJson(new TokenRefreshResult(), 401, new { Refresh_Token_Expired = true })
                    .RespondWithJson(loginResult)
                    .RespondWithJson(new MockResponse());

                var country = new MockEntity();
                var repo = new MockSecureRepository(configuration);
                await repo.PostAsync<MockResponse>("mock", country);

                httpTest.ShouldHaveCalled("http://test/mock")
                    .WithRequestJson(country)
                    .WithVerb(HttpMethod.Post)
                    .With(call => call.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    .Times(1);

                httpTest.ShouldHaveCalled("http://test/Authentication/refresh")
                    .WithVerb(HttpMethod.Post)
                    .With(call => call.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    .Times(1);

                httpTest.ShouldHaveCalled("http://test/Authentication/logon")
                    .WithVerb(HttpMethod.Post)
                    .Times(1);

                httpTest.ShouldHaveCalled("http://test/mock")
                    .WithRequestJson(country)
                    .WithVerb(HttpMethod.Post)
                    .WithHeader("Authorization", $"{loginResult.AuthorizationType} {loginResult.AccessToken}")
                    .With(call => call.Response.StatusCode == System.Net.HttpStatusCode.OK)
                    .Times(1);
            }
        }
    }
}
