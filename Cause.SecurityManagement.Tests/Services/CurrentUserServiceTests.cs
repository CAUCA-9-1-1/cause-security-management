using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services;

[TestFixture]
public class CurrentUserServiceTests
{
    private IHttpContextAccessor contextAccessor;
    private IUserPermissionRepository userPermissionRepository;
    private CurrentUserService service;
    private HttpContext httpContext;

    [SetUp]
    public void SetUp()
    {
        contextAccessor = Substitute.For<IHttpContextAccessor>();
        userPermissionRepository = Substitute.For<IUserPermissionRepository>();
        service = new CurrentUserService(contextAccessor, userPermissionRepository);
        httpContext = new DefaultHttpContext();
        contextAccessor.HttpContext.Returns(httpContext);
    }

    #region GetUserId Tests

    [Test]
    public void GetUserId_WithValidSidClaim_ShouldReturnUserIdAsGuid()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetUserId();

        result.Should().Be(userId);
    }

    [Test]
    public void GetUserId_WithoutSidClaim_ShouldReturnEmptyGuid()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetUserId();

        result.Should().Be(Guid.Empty);
    }

    [Test]
    public void GetUserId_WithInvalidSidClaim_ShouldReturnEmptyGuid()
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sid, "not-a-guid") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetUserId();

        result.Should().Be(Guid.Empty);
    }

    [Test]
    public void GetUserId_WithNullHttpContext_ShouldReturnEmptyGuid()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetUserId();

        result.Should().Be(Guid.Empty);
    }

    #endregion

    #region GetExternalSystemId Tests

    [Test]
    public void GetExternalSystemId_WithExternalSystemRoleAndValidSid_ShouldReturnExternalSystemId()
    {
        var externalSystemId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sid, externalSystemId.ToString()),
            new Claim(ClaimTypes.Role, SecurityRoles.ExternalSystem)
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetExternalSystemId();

        result.Should().Be(externalSystemId);
    }

    [Test]
    public void GetExternalSystemId_WithoutExternalSystemRole_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetExternalSystemId();

        result.Should().BeNull();
    }

    [Test]
    public void GetExternalSystemId_WithExternalSystemRoleButInvalidSid_ShouldReturnNull()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sid, "invalid-guid"),
            new Claim(ClaimTypes.Role, SecurityRoles.ExternalSystem)
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetExternalSystemId();

        result.Should().BeNull();
    }

    [Test]
    public void GetExternalSystemId_WithNullHttpContext_ShouldReturnNull()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetExternalSystemId();

        result.Should().BeNull();
    }

    [Test]
    public void GetExternalSystemId_WithExternalSystemRoleButNoSidClaim_ShouldReturnNull()
    {
        var claims = new[] { new Claim(ClaimTypes.Role, SecurityRoles.ExternalSystem) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetExternalSystemId();

        result.Should().BeNull();
    }

    #endregion

    #region GetUserDeviceId Tests

    [Test]
    public void GetUserDeviceId_WithValidDeviceIdClaim_ShouldReturnDeviceId()
    {
        var deviceId = Guid.NewGuid();
        var claims = new[] { new Claim(AdditionalClaimsGenerator.DeviceIdType, deviceId.ToString()) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetUserDeviceId();

        result.Should().Be(deviceId);
    }

    [Test]
    public void GetUserDeviceId_WithoutDeviceIdClaim_ShouldReturnNull()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetUserDeviceId();

        result.Should().BeNull();
    }

    [Test]
    public void GetUserDeviceId_WithInvalidDeviceIdClaim_ShouldReturnNull()
    {
        var claims = new[] { new Claim(AdditionalClaimsGenerator.DeviceIdType, "not-a-guid") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetUserDeviceId();

        result.Should().BeNull();
    }

    [Test]
    public void GetUserDeviceId_WithNullHttpContext_ShouldReturnNull()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetUserDeviceId();

        result.Should().BeNull();
    }

    #endregion

    #region GetCustomClaimValue Tests

    [Test]
    public void GetCustomClaimValue_WithExistingClaim_ShouldReturnClaimValue()
    {
        var customValue = "some-custom-value";
        var customClaimType = "CustomClaimType";
        var claims = new[] { new Claim(customClaimType, customValue) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetCustomClaimValue(customClaimType);

        result.Should().Be(customValue);
    }

    [Test]
    public void GetCustomClaimValue_WithNonExistentClaim_ShouldReturnNull()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetCustomClaimValue("NonExistentClaimType");

        result.Should().BeNull();
    }

    [Test]
    public void GetCustomClaimValue_WithNullHttpContext_ShouldReturnNull()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetCustomClaimValue("AnyClaimType");

        result.Should().BeNull();
    }

    #endregion

    #region GetUserIpAddress Tests

    [Test]
    public void GetUserIpAddress_WithValidIpAddress_ShouldReturnIpAddressAsString()
    {
        var ipAddress = System.Net.IPAddress.Parse("192.168.1.100");
        httpContext.Connection.RemoteIpAddress = ipAddress;

        var result = service.GetUserIpAddress();

        result.Should().Be("192.168.1.100");
    }

    [Test]
    public void GetUserIpAddress_WithIpv6Address_ShouldReturnMappedIpv4Address()
    {
        var ipv6Address = System.Net.IPAddress.Parse("::ffff:192.168.1.100");
        httpContext.Connection.RemoteIpAddress = ipv6Address;

        var result = service.GetUserIpAddress();

        result.Should().Be("192.168.1.100");
    }

    [Test]
    public void GetUserIpAddress_WithNullHttpContext_ShouldReturnNull()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetUserIpAddress();

        result.Should().BeNull();
    }

    [Test]
    public void GetUserIpAddress_WithNullRemoteIpAddress_ShouldReturnNull()
    {
        httpContext.Connection.RemoteIpAddress = null;

        var result = service.GetUserIpAddress();

        result.Should().BeNull();
    }

    #endregion

    #region GetAuthentifiedUserIdentifier Tests

    [Test]
    public void GetAuthentifiedUserIdentifier_WithClaimTypesNameClaim_ShouldReturnNameValue()
    {
        var userName = "testuser";
        var claims = new[] { new Claim(ClaimTypes.Name, userName) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetAuthentifiedUserIdentifier();

        result.Should().Be(userName);
    }

    [Test]
    public void GetAuthentifiedUserIdentifier_WithJwtNameClaimWhenClaimTypesNameAbsent_ShouldReturnJwtNameValue()
    {
        var jwtName = "jwt-user";
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Name, jwtName) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetAuthentifiedUserIdentifier();

        result.Should().Be(jwtName);
    }

    [Test]
    public void GetAuthentifiedUserIdentifier_WithJwtUniqueNameClaimWhenOthersAbsent_ShouldReturnUniqueNameValue()
    {
        var uniqueName = "unique-user";
        var claims = new[] { new Claim(JwtRegisteredClaimNames.UniqueName, uniqueName) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetAuthentifiedUserIdentifier();

        result.Should().Be(uniqueName);
    }

    [Test]
    public void GetAuthentifiedUserIdentifier_WithMultiplePossibleClaims_ShouldReturnClaimTypesNameFirst()
    {
        var claimTypesName = "claim-types-name";
        var jwtName = "jwt-name";
        var uniqueName = "unique-name";
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, claimTypesName),
            new Claim(JwtRegisteredClaimNames.Name, jwtName),
            new Claim(JwtRegisteredClaimNames.UniqueName, uniqueName)
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetAuthentifiedUserIdentifier();

        result.Should().Be(claimTypesName);
    }

    [Test]
    public void GetAuthentifiedUserIdentifier_WithoutRelevantClaims_ShouldReturnNull()
    {
        var claims = new[] { new Claim(ClaimTypes.Email, "test@example.com") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetAuthentifiedUserIdentifier();

        result.Should().BeNull();
    }

    [Test]
    public void GetAuthentifiedUserIdentifier_WithNullHttpContext_ShouldReturnNull()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetAuthentifiedUserIdentifier();

        result.Should().BeNull();
    }

    #endregion

    #region GetPermissionsAsync Tests

    [Test]
    public async Task GetPermissionsAsync_WhenCalled_ShouldReturnPermissionsFromRepository()
    {
        var userId = Guid.NewGuid();
        var permissions = new List<AuthenticationUserPermission>
        {
            new() { Tag = "Permission1", IsAllowed = true },
            new() { Tag = "Permission2", IsAllowed = false }
        };
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);
        userPermissionRepository.GetUserPermissionsAsync(Arg.Is(userId)).Returns(permissions);

        var result = await service.GetPermissionsAsync();

        result.Should().BeEquivalentTo(permissions);
        await userPermissionRepository.Received(1).GetUserPermissionsAsync(Arg.Is(userId));
    }

    [Test]
    public async Task GetPermissionsAsync_WithEmptyPermissions_ShouldReturnEmptyList()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sid, userId.ToString()) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);
        userPermissionRepository.GetUserPermissionsAsync(Arg.Is(userId)).Returns(new List<AuthenticationUserPermission>());

        var result = await service.GetPermissionsAsync();

        result.Should().BeEmpty();
    }

    #endregion

    #region GetRole Tests

    [Test]
    public void GetRole_WithRoleClaim_ShouldReturnRoleValue()
    {
        var role = SecurityRoles.User;
        var claims = new[] { new Claim(ClaimTypes.Role, role) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetRole();

        result.Should().Be(role);
    }

    [Test]
    public void GetRole_WithExternalSystemRole_ShouldReturnRole()
    {
        var role = SecurityRoles.ExternalSystem;
        var claims = new[] { new Claim(ClaimTypes.Role, role) };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetRole();

        result.Should().Be(role);
    }

    [Test]
    public void GetRole_WithoutRoleClaim_ShouldReturnNull()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var claimsIdentity = new ClaimsIdentity(claims);
        httpContext.User = new ClaimsPrincipal(claimsIdentity);

        var result = service.GetRole();

        result.Should().BeNull();
    }

    [Test]
    public void GetRole_WithNullHttpContext_ShouldReturnNull()
    {
        contextAccessor.HttpContext.Returns((HttpContext)null);

        var result = service.GetRole();

        result.Should().BeNull();
    }

    #endregion
}
