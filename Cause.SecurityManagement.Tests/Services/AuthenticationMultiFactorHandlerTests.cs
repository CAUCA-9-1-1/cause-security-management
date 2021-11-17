using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Repositories;
using Cause.SecurityManagement.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;

namespace Cause.SecurityManagement.Tests.Services
{

    [TestFixture]
    public class AuthenticationMultiFactorHandlerTests
    {
        private readonly Guid someUserId = Guid.NewGuid();
        private readonly User someUser = new();

        private AuthenticationMultiFactorHandler<User> handler;
        private IUserValidationCodeRepository repository;
        private IAuthenticationValidationCodeSender<User> sender;

        [SetUp]
        public void SetUpTest()
        {
            repository = Substitute.For<IUserValidationCodeRepository>();
            sender = Substitute.For<IAuthenticationValidationCodeSender<User>>();
            handler = new AuthenticationMultiFactorHandler<User>(repository, sender);            
        }

        [Test]
        public void ValidCode_WhenCheckingValidity_ShouldBeConsideredValid()
        {
            var someCode = "2349903";
            var someValidationType = ValidationCodeType.MultiFactorLogin;
            var someValidationCode = new UserValidationCode();
            repository.GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType))
                .Returns(someValidationCode);

            var result = handler.CodeIsValid(someUserId, someCode, someValidationType);

            repository.Received(1).GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType));
            repository.Received(1).DeleteCode(Arg.Is(someValidationCode));
            result.Should().BeTrue();
        }

        [Test]
        public void InvalidCode_WhenCheckingValidity_ShouldBeConsideredInvalid()
        {
            var someCode = "2349903";
            var someValidationType = ValidationCodeType.MultiFactorLogin;
            repository.GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType))
                .Returns((UserValidationCode)null);

            var result = handler.CodeIsValid(someUserId, someCode, someValidationType);

            repository.Received(1).GetExistingValidCode(Arg.Is(someUserId), Arg.Is(someCode), Arg.Is(someValidationType));
            repository.DidNotReceive().DeleteCode(Arg.Any<UserValidationCode>());
            result.Should().BeFalse();
        }

        [Test]
        public void NotFoundUser_WhenSendingValidationCode_ShouldNotSendAnything()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            handler.SendValidationCodeWhenNeeded(null);

            repository.DidNotReceive().DeleteExistingValidationCode(Arg.Any<Guid>(), Arg.Any<ValidationCodeType>());
            repository.DidNotReceive().SaveNewValidationCode(Arg.Any<UserValidationCode>());
            sender.DidNotReceive().SendCode(Arg.Any<User>(), Arg.Any<string>());
        }

        [Test]
        public void MultiFactorNotActivated_WhenSendingValidationCode_ShouldNotSendAnything()
        {
            var _ = new SecurityManagementOptions();
            handler.SendValidationCodeWhenNeeded(someUser);

            repository.DidNotReceive().DeleteExistingValidationCode(Arg.Any<Guid>(), Arg.Any<ValidationCodeType>());
            repository.DidNotReceive().SaveNewValidationCode(Arg.Any<UserValidationCode>());
            sender.DidNotReceive().SendCode(Arg.Any<User>(), Arg.Any<string>());
        }

        [Test]
        public void UserMustResetPassword_WhenSendingValidationCode_ShouldNotSendAnything()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            someUser.PasswordMustBeResetAfterLogin = true;

            handler.SendValidationCodeWhenNeeded(someUser);

            repository.DidNotReceive().DeleteExistingValidationCode(Arg.Any<Guid>(), Arg.Any<ValidationCodeType>());
            repository.DidNotReceive().SaveNewValidationCode(Arg.Any<UserValidationCode>());
            sender.DidNotReceive().SendCode(Arg.Any<User>(), Arg.Any<string>());
        }

        [Test]
        public void ExistingUser_WhenSendingValidationCode_ShouldSendCode()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();
            handler.SendValidationCodeWhenNeeded(someUser);

            repository.Received(1).DeleteExistingValidationCode(Arg.Is(someUser.Id), Arg.Is(ValidationCodeType.MultiFactorLogin));
            repository.Received(1).SaveNewValidationCode(Arg.Is<UserValidationCode>(code => code.IdUser == someUser.Id));
            sender.Received(1).SendCode(Arg.Is(someUser), Arg.Any<string>());
        }
    }
}