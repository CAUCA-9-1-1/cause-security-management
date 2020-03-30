using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests
{
    public class AuthentificationServiceTests
    {
        [Test]
        public void GetAccessTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            var option = new SecurityConfiguration
            {
                AccessTokenLifeTimeInMinutes = 33
            };
            var service = new AuthenticationService<User>(null, Options.Create(option));

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
            var service = new AuthenticationService<User>(null, Options.Create(option));

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
            var service = new AuthenticationService<User>(null, Options.Create(option));

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
            var service = new AuthenticationService<User>(null, Options.Create(option));

            var result = service.GetRefreshTokenLifeTimeInMinute();

            result.Should().Be(service.DefaultRefreshTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }
    }
}