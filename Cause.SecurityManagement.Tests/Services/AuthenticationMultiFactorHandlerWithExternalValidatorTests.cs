using Cause.SecurityManagement.Authentication.MultiFactor;
using Cause.SecurityManagement.Interfaces.Repositories;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.ValidationCode;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Cause.SecurityManagement.Tests.Services
{

    [TestFixture]
    public class AuthenticationMultiFactorHandlerWithExternalValidatorTests
    {
        private readonly Guid someUserId = Guid.NewGuid();
        private User someUser;

        private AuthenticationMultiFactorHandler<User> handler;
        private IUserValidationCodeRepository repository;
        private IAuthenticationValidationCodeSender<User> sender;
        private IAuthenticationValidationCodeValidator<User> validator;

        [SetUp]
        public void SetUpTest()
        {
            someUser = new User { Id = someUserId };
            repository = Substitute.For<IUserValidationCodeRepository>();
            sender = Substitute.For<IAuthenticationValidationCodeSender<User>>();
            validator = Substitute.For<IAuthenticationValidationCodeValidator<User>>();
            handler = new AuthenticationMultiFactorHandler<User>(repository, sender, validator);
        }

        [Test]
        public async Task NotFoundUser_WhenSendingValidationCode_ShouldNotSendAnything()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>>();

            await handler.SendValidationCodeWhenNeededAsync(null);

            await sender.DidNotReceive().SendCodeAsync(Arg.Any<User>());
        }

        [Test]
        public async Task ExistingUser_WhenSendingValidationCode_ShouldSendCode()
        {
            var options = new SecurityManagementOptions();
            options.UseMultiFactorAuthentication<IAuthenticationValidationCodeSender<User>, IAuthenticationValidationCodeValidator<User>>();
            repository.GetLastCode(Arg.Is(someUser.Id)).Returns(new UserValidationCode());

            await handler.SendNewValidationCodeAsync(someUser);

            repository.DidNotReceive().DeleteExistingValidationCode(Arg.Any<Guid>());
            repository.DidNotReceive().SaveNewValidationCode(Arg.Any<UserValidationCode>());
            await sender.Received(1).SendCodeAsync(Arg.Is(someUser));
        }

        [Test]
        public async Task KnownUserWithExistingCode_WhenRequestingNewCode_ShouldDeactivateAllPreviousCodeAndSendNewOne()
        {
            var someCode = new UserValidationCode { IdUser = someUserId, Type = ValidationCodeType.MultiFactorLogin };
            repository.GetLastCode(Arg.Is(someUserId)).Returns(someCode);

            await handler.SendNewValidationCodeAsync(someUser);

            repository.DidNotReceive().GetLastCode(Arg.Any<Guid>());
            repository.DidNotReceive().DeleteExistingValidationCode(Arg.Any<Guid>());
            repository.DidNotReceive().SaveNewValidationCode(Arg.Any<UserValidationCode>());
            await sender.Received(1).SendCodeAsync(Arg.Is(someUser));
        }

        [Test]
        public async Task KnownUserWithoutExistingCode_WhenRequestingNewCode_ShouldThrowException()
        {
            repository.GetLastCode(Arg.Is(someUserId)).Returns((UserValidationCode)null);

            await handler.SendNewValidationCodeAsync(someUser);

            await sender.Received(1).SendCodeAsync(Arg.Is(someUser));
        }
    }
}