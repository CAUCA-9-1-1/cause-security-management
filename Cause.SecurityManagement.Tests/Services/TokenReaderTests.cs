using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using System;
using Cause.SecurityManagement.Services;
using TokenReader = Cause.SecurityManagement.Services.TokenReader;

namespace Cause.SecurityManagement.Tests.Services
{
    public class TokenReaderTests
    {
        private SecurityConfiguration configuration;
        private TokenReader reader;

        [SetUp]
        public void SetUpTest()
        {
            configuration = new SecurityConfiguration
            {
                Issuer = "http://mytest.ca",
                PackageName = "CauseSecurityManagement",
                SecretKey = "RHzb3Z68KW9LanvjBoev2fupPzn94A3r"
            };
            reader = new TokenReader(Options.Create(configuration));
        }

        [Test]
        public void TokenGeneratedWithSecret_WhenGettingSid_ShouldBeAbleToReadIt()
        {
            var someUserId = "someUserId";
            var generator = new TokenGenerator(Options.Create(configuration));
            var token = generator.GenerateAccessToken(someUserId,"user", "test");
            
            var result = reader.GetSidFromExpiredToken(token);

            result.Should().Be(someUserId);
        }

        [Test]
        public void TokenGeneratedWithPreviousSecret_WhenGettingSid_ShouldBeAbleToReadIt()
        {
            var someUserId = "someUserId";
            var generator = new TokenGenerator(Options.Create(configuration));
            var token = generator.GenerateAccessToken(someUserId, "user", "test");
            configuration.AllowTokenRefreshWithPreviousSecretKey = true;
            configuration.PreviousSecretKey = configuration.SecretKey;
            configuration.SecretKey = "fIgA12S6y7JlfsX6iwizpAdlFDbrQFGs";

            var result = reader.GetSidFromExpiredToken(token);
            
            result.Should().Be(someUserId);
        }

        [Test]
        public void TokenGeneratedWithSecret_WhenGettingClaimValue_ShouldBeAbleToReadIt()
        {
            var someUserId = "someUserId";
            var someRole = new CustomClaims("some", "role");
            var generator = new TokenGenerator(Options.Create(configuration));
            var token = generator.GenerateAccessToken(someUserId, "user", "test", someRole);

            var result = reader.GetClaimValueFromExpiredToken(token, someRole.Type);

            result.Should().Be(someRole.Value);
        }

        [Test]
        public void TokenGeneratedWithPreviousSecret_WhenGettingClaimValue_ShouldBeAbleToReadIt()
        {
            var someUserId = "someUserId";
            var someRole = new CustomClaims("some", "role");
            var generator = new TokenGenerator(Options.Create(configuration));
            var token = generator.GenerateAccessToken(someUserId, "user", "test", someRole);
            configuration.AllowTokenRefreshWithPreviousSecretKey = true;
            configuration.PreviousSecretKey = configuration.SecretKey;
            configuration.SecretKey = "fIgA12S6y7JlfsX6iwizpAdlFDbrQFGs";

            var result = reader.GetClaimValueFromExpiredToken(token, someRole.Type);

            result.Should().Be(someRole.Value);
        }

        [Test]
        public void RefreshTokenCanExpire_WhenRefreshToken_ShouldNotReturnTokenExpired()
        {            
            configuration.RefreshTokenCanExpire = false;
            var token = new UserToken { AccessToken = "anAccessToken", ExpiresOn = DateTime.Now, RefreshToken = "aRefreshToken" };

            Action result = () => reader.ThrowExceptionWhenTokenIsNotValid("aRefreshToken", token);

            result.Should().NotThrow<SecurityTokenException>();
        }

        [Test]
        public void RefreshTokenCanTExpire_WhenRefreshToken_ShouldThrowTokenExpired()
        {
            configuration.AccessTokenLifeTimeInMinutes = null;
            configuration.RefreshTokenLifeTimeInMinutes = null;
            var token = new UserToken { AccessToken = "anAccessToken", ExpiresOn = DateTime.Now, RefreshToken = "aRefreshToken" };

            Action result = () => reader.ThrowExceptionWhenTokenIsNotValid("aRefreshToken", token);

            result.Should().Throw<SecurityTokenException>();
        }
    }
}