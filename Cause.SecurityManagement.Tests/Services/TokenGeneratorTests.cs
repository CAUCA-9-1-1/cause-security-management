using Cause.SecurityManagement.Models.Configuration;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Services
{
    public class TokenGeneratorTests
    {
        private SecurityConfiguration configuration;
        private TokenGenerator generator;

        [SetUp]
        public void SetUpTest()
        {
            configuration = new SecurityConfiguration();
            generator = new TokenGenerator(Options.Create(configuration));
        }

        [Test]
        public void GetAccessTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            configuration.AccessTokenLifeTimeInMinutes = 33;

            var result = generator.GetAccessTokenLifeTimeInMinute();

            result.Should().Be(configuration.AccessTokenLifeTimeInMinutes, "it was provided by the configuration");
        }

        [Test]
        public void GetAccessTokenLifeTimeInMinute_ShouldReturnDefaultValueWhenItIsNotSet()
        {
            configuration.AccessTokenLifeTimeInMinutes = null;

            var result = generator.GetAccessTokenLifeTimeInMinute();

            result.Should().Be(generator.DefaultAccessTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }

        [Test]
        public void GetRefreshTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            configuration.RefreshTokenLifeTimeInMinutes = 33;

            var result = generator.GetRefreshTokenLifeTimeInMinute();

            result.Should().Be(configuration.RefreshTokenLifeTimeInMinutes, "it was provided by the configuration");
        }

        [Test]
        public void GetRefreshTokenLifeTimeInMinute_ShouldReturnDefaultValueWhenItIsNotSet()
        {
            configuration.RefreshTokenLifeTimeInMinutes = null;            

            var result = generator.GetRefreshTokenLifeTimeInMinute();

            result.Should().Be(generator.DefaultRefreshTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }

        [Test]
        public void GetTemporaryTokenLifeTimeInMinute_ShouldReturnValueFromOptionWhenItIsSet()
        {
            configuration.TemporaryAccessTokenLifeTimeInMinutes = 33;
            
            var result = generator.GetTemporaryAccessTokenLifeTimeInMinute();

            result.Should().Be(configuration.TemporaryAccessTokenLifeTimeInMinutes, "it was provided by the configuration");
        }

        [Test]
        public void GetTemporaryTokenLifeTimeInMinute_ShouldReturnDefaultValueWhenItIsNotSet()
        {
            configuration.TemporaryAccessTokenLifeTimeInMinutes = null;

            var result = generator.GetTemporaryAccessTokenLifeTimeInMinute();

            result.Should().Be(generator.DefaultTemporaryAccessTokenLifetimeInMinutes, "it should use the default value when no value is provided by the configuration");
        }       
    }
}