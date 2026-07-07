using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Repositories;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Repositories
{
    [TestFixture]
    public class ExternalSystemRepositoryTests
    {
        private TestExternalSystemContext context;
        private ExternalSystemRepository<TestExternalSystemUser> repository;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestExternalSystemContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new TestExternalSystemContext(options);
            repository = new ExternalSystemRepository<TestExternalSystemUser>(new TestScopedDbContextProvider(context));
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await context.DisposeAsync();
        }

        [Test]
        public void TokenBoundSystem_GetByApiKey_ShouldReturnSystem()
        {
            var apiKey = "some-api-key";
            context.ExternalSystems.Add(new ExternalSystem
            {
                ApiKey = apiKey,
                IsActive = true,
                AuthenticationType = ExternalSystemAuthenticationType.Token
            });
            context.SaveChanges();

            var externalSystem = repository.GetByApiKey(apiKey);

            externalSystem.Should().NotBeNull();
        }

        [Test]
        public void CertificateBoundSystem_GetByApiKeyWithMatchingApiKey_ShouldReturnNull()
        {
            var apiKey = "some-api-key";
            context.ExternalSystems.Add(new ExternalSystem
            {
                ApiKey = apiKey,
                IsActive = true,
                AuthenticationType = ExternalSystemAuthenticationType.Certificate
            });
            context.SaveChanges();

            var externalSystem = repository.GetByApiKey(apiKey);

            externalSystem.Should().BeNull();
        }

        [Test]
        public void CertificateBoundSystem_GetByCertificateSubject_ShouldReturnSystem()
        {
            var subject = "CN=some-system";
            context.ExternalSystems.Add(new ExternalSystem
            {
                CertificateSubjectDn = subject,
                IsActive = true,
                AuthenticationType = ExternalSystemAuthenticationType.Certificate
            });
            context.SaveChanges();

            var externalSystem = repository.GetByCertificateSubject(subject);

            externalSystem.Should().NotBeNull();
        }

        [Test]
        public void TokenBoundSystem_GetByCertificateSubjectWithMatchingSubject_ShouldReturnNull()
        {
            var subject = "CN=some-system";
            context.ExternalSystems.Add(new ExternalSystem
            {
                CertificateSubjectDn = subject,
                IsActive = true,
                AuthenticationType = ExternalSystemAuthenticationType.Token
            });
            context.SaveChanges();

            var externalSystem = repository.GetByCertificateSubject(subject);

            externalSystem.Should().BeNull();
        }

        private sealed class TestExternalSystemUser : User { }

        private sealed class TestExternalSystemContext(DbContextOptions<TestExternalSystemContext> options)
            : BaseSecurityContext<TestExternalSystemUser>(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                AddSecurityManagementMappings(modelBuilder);
            }
        }

        private sealed class TestScopedDbContextProvider(TestExternalSystemContext context)
            : IScopedDbContextProvider<TestExternalSystemUser>
        {
            public ISecurityContext<TestExternalSystemUser> GetContext() => context;
        }
    }
}
