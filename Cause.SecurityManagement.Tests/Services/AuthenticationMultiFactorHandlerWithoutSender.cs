using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using Cause.SecurityManagement.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Tests.Services
{

    [TestFixture]
    public class AuthenticationMultiFactorHandlerWithoutSenderTests
    {
        private readonly Guid someUserId = Guid.NewGuid();
        private User someUser;

        private AuthenticationMultiFactorHandler<User> handler;
        private IUserValidationCodeRepository repository;

        [SetUp]
        public void SetUpTest()
        {
            someUser = new User { Id = someUserId };
            repository = Substitute.For<IUserValidationCodeRepository>();
            handler = new AuthenticationMultiFactorHandler<User>(repository);
        }

        [Test]
        public async Task NotFoundUser_WhenSendingValidationCode_ShouldNotSendAnything()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();

            var action = () => handler.SendNewValidationCodeAsync(null);

            await action.Should().ThrowAsync<UserValidationCodeNotFoundException>();
        }

        [Test]
        public async Task ExistingUser_WhenSendingValidationCode_ShouldSendCode()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>, IAuthenticationValidationCodeValidator<User>>();
            repository.GetLastCode(Arg.Is(someUser.Id)).Returns(new UserValidationCode());

            var action = () => handler.SendNewValidationCodeAsync(someUser);

            await action.Should().ThrowAsync<AuthenticationValidationCodeSenderNotFoundException>();
        }

        [Test]
        public async Task KnownUserWithExistingCode_WhenRequestingNewCode_ShouldDeactivateAllPreviousCodeAndSendNewOne()
        {
            var someCode = new UserValidationCode { IdUser = someUserId, Type = ValidationCodeType.MultiFactorLogin };
            repository.GetLastCode(Arg.Is(someUserId)).Returns(someCode);

            var action = () => handler.SendNewValidationCodeAsync(someUser);

            await action.Should().ThrowAsync<AuthenticationValidationCodeSenderNotFoundException>();
        }

        [Test]
        public async Task KnownUserWithoutExistingCode_WhenRequestingNewCode_ShouldThrowException()
        {
            repository.GetLastCode(Arg.Is(someUserId)).Returns((UserValidationCode)null);

            var action = () => handler.SendNewValidationCodeAsync(someUser);

            await action.Should().ThrowAsync<UserValidationCodeNotFoundException>();
        }
    }
}