using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;

namespace Cause.SecurityManagement.Tests.Services
{
    [TestFixture]
    public class ExternalSystemManagementServiceTests
    {
        private IExternalSystemRepository repository;
        private ExternalSystemManagementService service;

        [SetUp]
        public void SetUpTest()
        {
            repository = Substitute.For<IExternalSystemRepository>();
            service = new ExternalSystemManagementService(repository);
        }

        [Test]
        public void SomeExternalSystemId_WhenGetExternalSystem_ShouldGetWithRepository()
        {
            var someExternalSystemId = Guid.NewGuid();
            repository.GetById(Arg.Is(someExternalSystemId)).Returns((ExternalSystem)null);

            var systemExternal = service.GetById(someExternalSystemId);

            systemExternal.Should().BeNull();
            repository.Received().GetById(Arg.Is(someExternalSystemId));
        }

        [Test]
        public void NewExternalSystem_WhenUpdateExternalSystem_ShouldAddWithRepository()
        {
            var someExternalSystem = new ExternalSystem
            {
                Id = Guid.NewGuid(),
            };
            repository.Any(Arg.Is(someExternalSystem.Id)).Returns(false);

            var result = service.Update(someExternalSystem);

            result.Should().BeTrue();
            repository.Received().Add(Arg.Is(someExternalSystem));
        }

        [Test]
        public void ExistingExternalSystem_WhenUpdateExternalSystem_ShouldUpdateWithRepository()
        {
            var someExternalSystem = new ExternalSystem
            {
                Id = Guid.NewGuid(),
            };
            repository.Any(Arg.Is(someExternalSystem.Id)).Returns(true);

            var result = service.Update(someExternalSystem);

            result.Should().BeTrue();
            repository.Received().Update(Arg.Is(someExternalSystem));
        }
    }
}