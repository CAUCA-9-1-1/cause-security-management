﻿using System.Net.Http;
using System.Threading.Tasks;
using Cause.SecurityManagement.ApiClient.Tests.Mocks;
using Cause.SecurityMangement.ApiClient.Exceptions;
using Flurl.Http.Testing;
using NUnit.Framework;

namespace Cause.SecurityManagement.ApiClient.Tests.Services
{
    public class BaseClientTests
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
            Assert.ThrowsAsync<BadParameterApiException>(async () => await repo.PostAsync<MockResponse>("mock", entity));
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
            Assert.ThrowsAsync<InternalErrorApiException>(async () => await repo.PostAsync<MockResponse>("mock", entity));
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
    }
}
