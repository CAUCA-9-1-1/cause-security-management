using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication.Exceptions;
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

        [Test]
        public void TwoActiveCertificateBoundSystemsWithSameSubject_GetByCertificateSubject_ShouldThrowDuplicateCertificateSubjectException()
        {
            var subject = "CN=some-system";
            context.ExternalSystems.AddRange(
                new ExternalSystem
                {
                    CertificateSubjectDn = subject,
                    IsActive = true,
                    AuthenticationType = ExternalSystemAuthenticationType.Certificate
                },
                new ExternalSystem
                {
                    CertificateSubjectDn = subject,
                    IsActive = true,
                    AuthenticationType = ExternalSystemAuthenticationType.Certificate
                });
            context.SaveChanges();

            var act = () => repository.GetByCertificateSubject(subject);

            act.Should().Throw<DuplicateCertificateSubjectException>()
                .Which.CertificateSubjectDn.Should().Be(subject);
        }

        [Test]
        public void OneActiveAndOneInactiveCertificateBoundSystemWithSameSubject_GetByCertificateSubject_ShouldReturnActiveSystem()
        {
            var subject = "CN=some-system";
            var activeExternalSystem = new ExternalSystem
            {
                CertificateSubjectDn = subject,
                IsActive = true,
                AuthenticationType = ExternalSystemAuthenticationType.Certificate
            };
            context.ExternalSystems.AddRange(
                activeExternalSystem,
                new ExternalSystem
                {
                    CertificateSubjectDn = subject,
                    IsActive = false,
                    AuthenticationType = ExternalSystemAuthenticationType.Certificate
                });
            context.SaveChanges();

            var externalSystem = repository.GetByCertificateSubject(subject);

            externalSystem.Should().Be(activeExternalSystem);
        }

        [Test]
        public void CertificateAndTokenBoundSystemsWithSameSubject_GetByCertificateSubject_ShouldReturnCertificateBoundSystem()
        {
            var subject = "CN=some-system";
            var certificateBoundExternalSystem = new ExternalSystem
            {
                CertificateSubjectDn = subject,
                IsActive = true,
                AuthenticationType = ExternalSystemAuthenticationType.Certificate
            };
            context.ExternalSystems.AddRange(
                certificateBoundExternalSystem,
                new ExternalSystem
                {
                    CertificateSubjectDn = subject,
                    IsActive = true,
                    AuthenticationType = ExternalSystemAuthenticationType.Token
                });
            context.SaveChanges();

            var externalSystem = repository.GetByCertificateSubject(subject);

            externalSystem.Should().Be(certificateBoundExternalSystem);
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
