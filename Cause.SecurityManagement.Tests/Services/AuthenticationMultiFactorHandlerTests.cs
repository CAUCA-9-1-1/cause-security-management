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
    public class AuthenticationMultiFactorHandlerTests
    {
        private readonly Guid someUserId = Guid.NewGuid();
        private User someUser;

        private AuthenticationMultiFactorHandler<User> handler;
        private IUserValidationCodeRepository repository;
        private IAuthenticationValidationCodeSender<User> sender;

        [SetUp]
        public void SetUpTest()
        {
            someUser = new User { Id = someUserId };
            repository = Substitute.For<IUserValidationCodeRepository>();
            sender = Substitute.For<IAuthenticationValidationCodeSender<User>>();
            handler = new AuthenticationMultiFactorHandler<User>(repository, sender);
        }

        [Test]
        public async Task ValidCode_WhenCheckingValidity_ShouldBeConsideredValid()
        {
            var someCode = "2349903";
            var someValidationType = ValidationCodeType.MultiFactorLogin;
            var someValidationCode = new UserValidationCode();
            repository.GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType))
                .Returns(someValidationCode);

            var result = await handler.CodeIsValidAsync(someUser, someCode, someValidationType);

            repository.Received(1).GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType));
            repository.Received(1).DeleteCode(Arg.Is(someValidationCode));
            result.Should().BeTrue();
        }

        [Test]
        public async Task InvalidCode_WhenCheckingValidity_ShouldBeConsideredInvalid()
        {
            var someCode = "2349903";
            var someValidationType = ValidationCodeType.MultiFactorLogin;
            repository.GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType))
                .Returns((UserValidationCode)null);

            var result = await handler.CodeIsValidAsync(someUser, someCode, someValidationType);

            repository.Received(1).GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType));
            repository.DidNotReceive().DeleteCode(Arg.Any<UserValidationCode>());
            result.Should().BeFalse();
        }

        [Test]
        public async Task NotFoundUser_WhenSendingValidationCode_ShouldNotSendAnything()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            await handler.SendValidationCodeWhenNeededAsync(null);

            repository.DidNotReceive().DeleteExistingValidationCode(Arg.Any<Guid>());
            repository.DidNotReceive().SaveNewValidationCode(Arg.Any<UserValidationCode>());
            await sender.DidNotReceive().SendCodeAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Test]
        public async Task ExistingUser_WhenSendingValidationCode_ShouldSendCode()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            repository.GetLastCode(Arg.Is(someUser.Id)).Returns(new UserValidationCode());

            await handler.SendNewValidationCodeAsync(someUser);

            repository.Received(1).DeleteExistingValidationCode(Arg.Is(someUser.Id));
            repository.Received(1).SaveNewValidationCode(Arg.Is<UserValidationCode>(code => code.IdUser == someUser.Id));
            await sender.Received(1).SendCodeAsync(Arg.Is(someUser), Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Test]
        public async Task KnownUserWithExistingCode_WhenRequestingNewCode_ShouldDeactivateAllPreviousCodeAndSendNewOne()
        {
            var someCode = new UserValidationCode { IdUser = someUserId, Type = ValidationCodeType.MultiFactorLogin };
            repository.GetLastCode(Arg.Is(someUserId)).Returns(someCode);

            await handler.SendNewValidationCodeAsync(someUser);

            repository.Received(1).GetLastCode(Arg.Is(someUserId));
            repository.Received(1).DeleteExistingValidationCode(Arg.Is(someUserId));
            repository.Received(1).SaveNewValidationCode(Arg.Is<UserValidationCode>(code => code.IdUser == someUser.Id && code.Type == someCode.Type));
            await sender.Received(1).SendCodeAsync(Arg.Is(someUser), Arg.Any<string>(), Arg.Any<DateTime>());
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