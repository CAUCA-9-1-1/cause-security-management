using Cause.SecurityManagement.Authentication.Antiforgery;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Authentication.Antiforgery
{
    public class BaseAntiforgeryTests
    {
        private static bool IsFromMobile(string userAgent = "", string clientHintPlatform = "")
        {
            var request = Substitute.For<HttpRequest>();
            request.Headers.UserAgent = userAgent;
            if (!string.IsNullOrEmpty(clientHintPlatform))
                request.Headers["Sec-CH-UA-Platform"] = clientHintPlatform;
            return TestableBaseAntiforgery.ExposedRequestIsFromMobile(request);
        }

        [Test]
        public void WithIphoneUserAgent_ShouldBeDetectedAsMobile()
            => IsFromMobile("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)").Should().BeTrue();

        [Test]
        public void WithIpadUserAgent_ShouldBeDetectedAsMobile()
            => IsFromMobile("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)").Should().BeTrue();

        [Test]
        public void WithAndroidUserAgent_ShouldBeDetectedAsMobile()
            => IsFromMobile("Mozilla/5.0 (Linux; Android 14; Pixel 8)").Should().BeTrue();

        [Test]
        public void WithIpadDesktopModeUaAndIosClientHint_ShouldBeDetectedAsMobile()
            => IsFromMobile(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/604.1",
                "\"iOS\"").Should().BeTrue();

        [Test]
        public void WithNoUaAndAndroidClientHint_ShouldBeDetectedAsMobile()
            => IsFromMobile(clientHintPlatform: "\"Android\"").Should().BeTrue();

        [Test]
        public void WithMacDesktopUaAndNoClientHint_ShouldNotBeDetectedAsMobile()
            => IsFromMobile("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/604.1")
                .Should().BeFalse();
    }

    internal class TestableBaseAntiforgery : BaseAntiforgery
    {
        internal static bool ExposedRequestIsFromMobile(HttpRequest request)
            => RequestIsFromMobile(request);
    }
}
