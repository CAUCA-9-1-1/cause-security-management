using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NUnit.Framework;
using System;

namespace Cause.SecurityManagement.Tests
{
	public class AuthenticationServiceTests
    {
        private readonly ICurrentUserService userService;
        public AuthenticationServiceTests()
        {
            userService = Substitute.For<ICurrentUserService>();
        }

        [Test]
        public void GetAccessTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            var option = new SecurityConfiguration
            {
                AccessTokenLifeTimeInMinutes = 33
            };
            var service = new AuthenticationService<User>(userService, null, Options.Create(option));

            var result = service.GetAccessTokenLifeTimeInMinute();

            result.Should().Be(option.AccessTokenLifeTimeInMinutes, "it was provided by the configuration");
        }

        [Test]
        public void GetAccessTokenLifeTimeInMinute_ShouldReturnDefaultValueWhenItIsNotSet()
        {
            var option = new SecurityConfiguration
            {
                AccessTokenLifeTimeInMinutes = null
            };
            var service = new AuthenticationService<User>(userService, null, Options.Create(option));

            var result = service.GetAccessTokenLifeTimeInMinute();

            result.Should().Be(service.DefaultAccessTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }

        [Test]
        public void GetRefreshTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            var option = new SecurityConfiguration
            {
                RefreshTokenLifeTimeInMinutes = 33
            };
            var service = new AuthenticationService<User>(userService, null, Options.Create(option));

            var result = service.GetRefreshTokenLifeTimeInMinute();

            result.Should().Be(option.RefreshTokenLifeTimeInMinutes, "it was provided by the configuration");
        }

        [Test]
        public void GetRefreshTokenLifeTimeInMinute_ShouldReturnDefaultValueWhenItIsNotSet()
        {
            var option = new SecurityConfiguration
            {
                RefreshTokenLifeTimeInMinutes = null
            };
            var service = new AuthenticationService<User>(userService, null, Options.Create(option));

            var result = service.GetRefreshTokenLifeTimeInMinute();

            result.Should().Be(service.DefaultRefreshTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }

        [Test]
        public void GetTemporaryTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            var option = new SecurityConfiguration
            {
                TemporaryAccessTokenLifeTimeInMinutes = 33
            };
            var service = new AuthenticationService<User>(userService, null, Options.Create(option));

            var result = service.GetTemporaryAccessTokenLifeTimeInMinute();

            result.Should().Be(option.TemporaryAccessTokenLifeTimeInMinutes, "it was provided by the configuration");
        }

        [Test]
        public void GetTemporaryTokenLifeTimeInMinute_ShouldReturnDefaultValueWhenItIsNotSet()
        {
            var option = new SecurityConfiguration
            {
                TemporaryAccessTokenLifeTimeInMinutes = null
            };
            var service = new AuthenticationService<User>(userService, null, Options.Create(option));

            var result = service.GetTemporaryAccessTokenLifeTimeInMinute();

            result.Should().Be(service.DefaultTemporaryAccessTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }

        [Test]
        public void RefreshTokenCanExpire_WhenRefreshToken_ShouldNotReturnTokenExpired()
        {
			var option = new SecurityConfiguration
			{
                RefreshTokenCanExpire = false
			};
			var token = new UserToken { AccessToken = "anAccessToken", ExpiresOn = DateTime.Now, RefreshToken = "aRefreshToken"};
			var service = new AuthenticationService<User>(userService, null, Options.Create(option));

			Action result = () => service.ThrowExceptionWhenTokenIsNotValid("aRefreshToken", token);


            result.Should().NotThrow<SecurityTokenException>();
        }

        [Test]
        public void RefreshTokenCanTExpire_WhenRefreshToken_ShouldThrowTokenExpired()
        {
	        var option = new SecurityConfiguration
	        {
		        AccessTokenLifeTimeInMinutes = null,
                RefreshTokenLifeTimeInMinutes = null
	        };
	        var token = new UserToken { AccessToken = "anAccessToken", ExpiresOn = DateTime.Now, RefreshToken = "aRefreshToken" };
	        var service = new AuthenticationService<User>(userService, null, Options.Create(option));

	        Action result = () => service.ThrowExceptionWhenTokenIsNotValid("aRefreshToken", token);


	        result.Should().Throw<SecurityTokenException>();
        }
    }
}