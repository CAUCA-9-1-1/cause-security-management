using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Controllers.Management;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Controllers.Management
{
    [TestFixture]
    public class BaseUserEditionControllerTests
    {
        private IUserNameAvailabilityReader userNameAvailability;
        private IValidator<UserForEdition> userValidator;
        private TestableUserEditionController controller;

        [SetUp]
        public void SetUp()
        {
            userNameAvailability = Substitute.For<IUserNameAvailabilityReader>();
            userValidator = Substitute.For<IValidator<UserForEdition>>();
            userValidator.ValidateAsync(Arg.Any<UserForEdition>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            controller = new TestableUserEditionController(userNameAvailability, userValidator)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                ProblemDetailsFactory = new TestProblemDetailsFactory(),
            };
        }

        [Test]
        public async Task WhenTheCallerMayNotSeeTheUser_GetAsync_ShouldReturnForbiddenWithoutReading()
        {
            controller.CanEdit = false;

            var result = await controller.GetAsync(Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<StatusCodeResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            controller.ReadForEditionCalled.Should().BeFalse();
        }

        [Test]
        public async Task WhenTheUserDoesNotExist_GetAsync_ShouldReturnNotFound()
        {
            controller.CanEdit = true;
            controller.UserToReturn = null;

            var result = await controller.GetAsync(Guid.NewGuid(), CancellationToken.None);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task WhenTheUserExists_GetAsync_ShouldReturnIt()
        {
            var user = new UserForEdition { Id = Guid.NewGuid() };
            controller.CanEdit = true;
            controller.UserToReturn = user;

            var result = await controller.GetAsync(user.Id, CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeSameAs(user);
        }

        [Test]
        public async Task WhenTheBodyIsNull_SaveAsync_ShouldReturnBadRequestWithoutValidatingOrWriting()
        {
            var result = await controller.SaveAsync(null, CancellationToken.None);

            (result as ObjectResult)?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            await userValidator.DidNotReceive().ValidateAsync(Arg.Any<UserForEdition>(), Arg.Any<CancellationToken>());
            controller.WriteForEditionCalled.Should().BeFalse();
        }

        [Test]
        public async Task WhenTheBodyFailsValidation_SaveAsync_ShouldReturnBadRequestWithoutCheckingScope()
        {
            userValidator.ValidateAsync(Arg.Any<UserForEdition>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult(new[] { new ValidationFailure("Login.UserName", "must not be empty") }));
            var user = new UserForEdition { Id = Guid.NewGuid() };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            (result as ObjectResult)?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            controller.CanEditCalled.Should().BeFalse();
            controller.WriteForEditionCalled.Should().BeFalse();
        }

        [Test]
        public async Task WhenTheCallerMayNotSeeTheUser_SaveAsync_ShouldReturnForbiddenWithoutWriting()
        {
            controller.CanEdit = false;
            var user = new UserForEdition { Id = Guid.NewGuid() };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            result.Should().BeOfType<StatusCodeResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            controller.WriteForEditionCalled.Should().BeFalse();
        }

        [Test]
        public async Task WhenTheCallerMayNotGrantTheRequestedGroups_SaveAsync_ShouldReturnForbiddenWithoutWriting()
        {
            controller.CanEdit = true;
            controller.CanGrantGroups = false;
            var user = new UserForEdition { Id = Guid.NewGuid(), GroupIds = [Guid.NewGuid()] };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            result.Should().BeOfType<StatusCodeResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            controller.WriteForEditionCalled.Should().BeFalse();
        }

        [Test]
        public async Task WhenTheCallerMayGrantTheRequestedGroups_SaveAsync_ShouldWrite()
        {
            controller.CanEdit = true;
            controller.CanGrantGroups = true;
            controller.Outcome = UserSaveOutcome.Done;
            var user = new UserForEdition { Id = Guid.NewGuid(), GroupIds = [Guid.NewGuid()] };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
            controller.WriteForEditionCalled.Should().BeTrue();
        }

        [Test]
        public async Task WhenTheWriteSucceeds_SaveAsync_ShouldReturnNoContent()
        {
            controller.CanEdit = true;
            controller.Outcome = UserSaveOutcome.Done;
            var user = new UserForEdition { Id = Guid.NewGuid() };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
        }

        [Test]
        public async Task WhenTheWriteReportsNotFound_SaveAsync_ShouldReturnNotFound()
        {
            controller.CanEdit = true;
            controller.Outcome = UserSaveOutcome.NotFound;
            var user = new UserForEdition { Id = Guid.NewGuid() };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task WhenTheWriteFails_SaveAsync_ShouldReturnProblemWithATranslationKey()
        {
            controller.CanEdit = true;
            controller.Outcome = UserSaveOutcome.Failed;
            var user = new UserForEdition { Id = Guid.NewGuid() };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            objectResult.Value.Should().BeOfType<ProblemDetails>()
                .Which.Detail.Should().Be("users.edition.saveFailed");
        }

        [Test]
        public async Task WhenCreatingAndTheGateRefuses_SaveAsync_ShouldReturnForbiddenWithoutWriting()
        {
            controller.CanEdit = false;
            controller.Outcome = UserSaveOutcome.Done;
            var user = new UserForEdition { Id = Guid.Empty };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            result.Should().BeOfType<StatusCodeResult>()
                .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            controller.WriteForEditionCalled.Should().BeFalse();
        }

        [Test]
        public async Task WhenCreatingAndTheGateAllows_SaveAsync_ShouldWriteAndReturnTheNewIdentifier()
        {
            controller.CanEdit = true;
            controller.Outcome = UserSaveOutcome.Done;
            controller.GeneratedId = Guid.NewGuid();
            var user = new UserForEdition { Id = Guid.Empty };

            var result = await controller.SaveAsync(user, CancellationToken.None);

            controller.WriteForEditionCalled.Should().BeTrue();
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(controller.GeneratedId);
        }

        [Test]
        public async Task WhenTheNameIsFree_IsUserNameAvailableAsync_ShouldReturnTrue()
        {
            userNameAvailability.IsAvailableAsync("dispatcher", Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var result = await controller.IsUserNameAvailableAsync("dispatcher", null, CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(true);
        }

        [Test]
        public async Task WhenTheNameIsTaken_IsUserNameAvailableAsync_ShouldReturnFalse()
        {
            userNameAvailability.IsAvailableAsync("dispatcher", Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var result = await controller.IsUserNameAvailableAsync("dispatcher", null, CancellationToken.None);

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(false);
        }

        [Test]
        public async Task WhenNoUserIdIsProvided_IsUserNameAvailableAsync_ShouldPassGuidEmptyToTheReader()
        {
            await controller.IsUserNameAvailableAsync("dispatcher", null, CancellationToken.None);

            await userNameAvailability.Received().IsAvailableAsync("dispatcher", Guid.Empty, Arg.Any<CancellationToken>());
        }

        private sealed class TestableUserEditionController(
            IUserNameAvailabilityReader userNameAvailability,
            IValidator<UserForEdition> userValidator)
            : BaseUserEditionController<User, UserForEdition>(userNameAvailability, userValidator)
        {
            public bool CanEdit { get; set; }
            public bool CanEditCalled { get; private set; }
            public bool CanGrantGroups { get; set; } = true;
            public UserForEdition UserToReturn { get; set; }
            public UserSaveOutcome Outcome { get; set; }
            public Guid GeneratedId { get; set; } = Guid.NewGuid();
            public bool ReadForEditionCalled { get; private set; }
            public bool WriteForEditionCalled { get; private set; }

            protected override Task<bool> CanEditUserAsync(Guid userId, CancellationToken cancellationToken)
            {
                CanEditCalled = true;
                return Task.FromResult(CanEdit);
            }

            protected override Task<bool> CanGrantGroupsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken)
                => Task.FromResult(CanGrantGroups);

            protected override Task<UserForEdition> ReadForEditionAsync(Guid userId, CancellationToken cancellationToken)
            {
                ReadForEditionCalled = true;
                return Task.FromResult(UserToReturn);
            }

            protected override Task<UserSaveResult> WriteForEditionAsync(UserForEdition user, CancellationToken cancellationToken)
            {
                WriteForEditionCalled = true;
                return Task.FromResult(Outcome switch
                {
                    UserSaveOutcome.Done => UserSaveResult.Done(GeneratedId),
                    UserSaveOutcome.NotFound => UserSaveResult.NotFound(),
                    _ => UserSaveResult.Failed(),
                });
            }
        }

        private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
        {
            public override ProblemDetails CreateProblemDetails(
                HttpContext httpContext, int? statusCode = null, string title = null,
                string type = null, string detail = null, string instance = null)
                => new() { Status = statusCode, Detail = detail };

            public override ValidationProblemDetails CreateValidationProblemDetails(
                HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null,
                string title = null, string type = null, string detail = null, string instance = null)
                => new(modelStateDictionary) { Status = statusCode ?? StatusCodes.Status400BadRequest, Detail = detail };
        }
    }
}
