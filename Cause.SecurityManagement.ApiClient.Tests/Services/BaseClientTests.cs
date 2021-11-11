using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cauca.ApiClient.Exceptions;
using Cauca.ApiClient.Tests.Mocks;
using FluentAssertions;
using Flurl.Http.Testing;
using NUnit.Framework;

namespace Cauca.ApiClient.Tests.Services
{
    public class BaseClientTests
    {
        private MockConfiguration configuration;
        private MockConfiguration configurationWithNoTrailingSlash;

        [SetUp]
        public void SetupTest()
        {
            configuration = new MockConfiguration
            {
                ApiBaseUrl = "http://test/",
                AccessToken = "Token",
                RefreshToken = "RefreshToken",
                AuthorizationType = "Mock"
            };

            configurationWithNoTrailingSlash = new MockConfiguration
            {
                ApiBaseUrl = "http://test",
                AccessToken = "Token",
                RefreshToken = "RefreshToken",
                AuthorizationType = "Mock"
            };
        }

        [TestCase]
        public async Task PostRequestAreCorrectlyExecuted()
        {
            using var httpTest = new HttpTest();
            var entity = new MockEntity();
            var repo = new MockRepository(configuration);
            await repo.PostAsync<MockResponse>("mock", entity);

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithRequestJson(entity)
                .WithVerb(HttpMethod.Post)
                .Times(1);
        }

        [TestCase]
        public async Task PostRequestAreCorrectlyExecutedWhenBaseUrlDoesntEndWithSlash()
        {
            using var httpTest = new HttpTest();
            var entity = new MockEntity();
            var repo = new MockRepository(configurationWithNoTrailingSlash);
            await repo.PostAsync<MockResponse>("mock", entity);

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithRequestJson(entity)
                .WithVerb(HttpMethod.Post)
                .Times(1);
        }

        [TestCase]
        public void RequestIsThrowingErrorWhenUrlIsNotFound()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MockResponse(), 404);
            var entity = new MockEntity();
            var repo = new MockRepository(configuration);
            Assert.ThrowsAsync<NotFoundApiException>(async () => await repo.PostAsync<MockResponse>("mock", entity));
        }

        [TestCase]
        public void RequestIsThrowingErrorWhenGettingBadParameters()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MockResponse(), 400);
            var entity = new MockEntity();
            var repo = new MockRepository(configuration);
            Assert.ThrowsAsync<BadParameterApiException>(async () =>
                await repo.PostAsync<MockResponse>("mock", entity));
        }

        [TestCase]
        public void RequestIsThrowingErrorWhenGettingForbidden()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MockResponse(), 403);
            var entity = new MockEntity();
            var repo = new MockRepository(configuration);
            Assert.ThrowsAsync<ForbiddenApiException>(async () => await repo.PostAsync<MockResponse>("mock", entity));
        }

        [TestCase]
        public void RequestIsThrowingErrorWhenGettingInternalError()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MockResponse(), 500);
            var entity = new MockEntity();
            var repo = new MockRepository(configuration);
            Assert.ThrowsAsync<InternalErrorApiException>(
                async () => await repo.PostAsync<MockResponse>("mock", entity));
        }

        [TestCase]
        public void RequestIsThrowingErrorWhenNotGettingAnAnswer()
        {
            using var httpTest = new HttpTest();
            httpTest.SimulateTimeout();
            var entity = new MockEntity();
            var repo = new MockRepository(configuration);
            Assert.ThrowsAsync<NoResponseApiException>(async () => await repo.PostAsync<MockResponse>("mock", entity));
        }

        [TestCase]
        public async Task BooleanAreCorrectlyReceived()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWith(true.ToString());
            var repo = new MockRepository(configuration);
            var response = await repo.GetAsync<bool>("mock");

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithVerb(HttpMethod.Get)
                .Times(1);

            Assert.AreEqual(response, true);
        }

        [TestCase]
        public async Task FalseBooleanAreCorrectlyReceived()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWith(false.ToString());
            var repo = new MockRepository(configuration);
            var response = await repo.GetAsync<bool>("mock");

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithVerb(HttpMethod.Get)
                .Times(1);

            Assert.AreEqual(response, false);
        }

        [TestCase]
        public async Task StringAreCorrectlyReceived()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWith("Allo");
            var repo = new MockRepository(configuration);
            var response = await repo.GetAsync<string>("mock");

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithVerb(HttpMethod.Get)
                .Times(1);

            Assert.AreEqual(response, "Allo");
        }

        [TestCase]
        public async Task StringAreCorrectlyReceivedWhenUsingGetAsyncString()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWith("Allo");
            var repo = new MockRepository(configuration);
            var response = await repo.GetStringAsync("mock");

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithVerb(HttpMethod.Get)
                .Times(1);

            Assert.AreEqual(response, "Allo");
        }

        [TestCase]
        public async Task IntAreCorrectlyReceived()
        {
            using var httpTest = new HttpTest();
            httpTest.RespondWith(33.ToString());
            var repo = new MockRepository(configuration);
            var response = await repo.GetAsync<int>("mock");

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithVerb(HttpMethod.Get)
                .Times(1);

            Assert.AreEqual(response, 33);
        }

        [TestCase]
        public async Task BytesArrayAreCorrectlyReceived()
        {
            var text = "Ceci est mon test";
            using var httpTest = new HttpTest();
            httpTest.RespondWith(text);
            var repo = new MockRepository(configuration);
            var response = await repo.GetBytesAsync("mock");

            httpTest.ShouldHaveCalled("http://test/mock")
                .WithVerb(HttpMethod.Get)
                .Times(1);

            Assert.AreEqual(text, Encoding.UTF8.GetString(response));
        }
    }
}
