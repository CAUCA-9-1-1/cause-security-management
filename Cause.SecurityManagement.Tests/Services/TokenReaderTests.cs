using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using System;
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
            configuration = new SecurityConfiguration();
            reader = new TokenReader(Options.Create(configuration));            
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