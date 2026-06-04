using AwesomeAssertions;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Wolverine.Features.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Cause.SecurityManagement.Wolverine.Tests.Features.Authentication;

[TestFixture]
public class RefreshEndpointTests
{
    private IEntityTokenRefresher tokenRefresher = null!;
    private ILogger<RefreshEndpoint> logger = null!;
    private DefaultHttpContext httpContext = null!;

    private readonly TokenRefreshResult tokens = new() { AccessToken = "old", RefreshToken = "refresh" };

    [SetUp]
    public void SetUp()
    {
        tokenRefresher = Substitute.For<IEntityTokenRefresher>();
        logger = Substitute.For<ILogger<RefreshEndpoint>>();
        httpContext = new DefaultHttpContext();
    }

    [Test]
    public async Task WithValidTokens_WhenRefreshing_ShouldReturnNewAccessToken()
    {
        tokenRefresher.GetNewAccessTokenAsync(tokens.AccessToken, tokens.RefreshToken).Returns("newToken");

        var result = await RefreshEndpoint.HandleAsync(tokens, httpContext, tokenRefresher, logger);

        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(200);
    }

    public static IEnumerable<TestCaseData> RefreshExceptions
    {
        get
        {
            yield return new TestCaseData(new InvalidTokenException("x", new Exception()), "Token-Invalid");
            yield return new TestCaseData(new SecurityTokenExpiredException(), "Refresh-Token-Expired");
            yield return new TestCaseData(new SecurityTokenException(), "Token-Invalid");
            yield return new TestCaseData(new InvalidTokenUserException("x", "y", "z"), "Token-Invalid");
        }
    }

    [TestCaseSource(nameof(RefreshExceptions))]
    public async Task WithException_WhenRefreshing_ShouldReturnUnauthorizedAndSetHeader(Exception ex, string expectedHeader)
    {
        tokenRefresher.GetNewAccessTokenAsync(Arg.Any<string>(), Arg.Any<string>()).ThrowsAsync(ex);

        var result = await RefreshEndpoint.HandleAsync(tokens, httpContext, tokenRefresher, logger);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
        httpContext.Response.Headers.ContainsKey(expectedHeader).Should().BeTrue();
    }
}
