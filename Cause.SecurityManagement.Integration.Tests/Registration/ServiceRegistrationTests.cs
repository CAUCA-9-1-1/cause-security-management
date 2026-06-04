using AwesomeAssertions;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Core.Repositories;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using FluentValidation;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Registration;

[TestFixture]
public class ServiceRegistrationTests : IntegrationTestBase
{
    [Test]
    public void InjectSecurityServices_ResolvesUserAuthenticator()
        => Resolve<IUserAuthenticator>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesExternalSystemAuthenticationService()
        => Resolve<IExternalSystemAuthenticationService>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesCurrentUserService()
        => Resolve<ICurrentUserService>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesUserRepository()
        => Resolve<IUserRepository<TestUser>>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesUserValidationCodeRepository()
        => Resolve<IUserValidationCodeRepository>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesUserManagementService()
        => Resolve<IUserManagementService<TestUser>>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesGroupManagementService()
        => Resolve<IGroupManagementService>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesPermissionManagementService()
        => Resolve<IPermissionManagementService>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesGroupManagementApiService()
        => Resolve<IGroupManagementApiService>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesPermissionCatalogService()
        => Resolve<IPermissionCatalogService>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesGroupDtoValidator()
        => Resolve<IValidator<GroupDto>>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_ResolvesTokenReader()
        => Resolve<ITokenReader>().Should().NotBeNull();

    [Test]
    public void InjectSecurityServices_CurrentUserService_IsTestDouble()
    {
        // The last registration must win — TestCurrentUserService overrides the real one
        var service = Resolve<ICurrentUserService>();
        service.Should().BeOfType<TestCurrentUserService>();
    }
}
