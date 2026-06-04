using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Controllers.Management;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Controllers.Management
{
    [TestFixture]
    public class UserSearchControllerTests
    {
        private IUserSearchService userSearchService;
        private TestableUserSearchController controller;

        [SetUp]
        public void SetUp()
        {
            userSearchService = Substitute.For<IUserSearchService>();
            controller = new TestableUserSearchController(userSearchService);
        }

        [Test]
        public async Task WhenSearching_SearchUsers_ShouldReturnOkWithResult()
        {
            var request = new UserSearchRequestDto { Query = "ada", Skip = 0, Top = 10 };
            var searchResult = new UserSearchResultDto { Items = new List<UserForGroupDto>(), TotalCount = 0 };
            userSearchService.SearchUsersAsync(request, Arg.Any<CancellationToken>()).Returns(searchResult);

            var result = await controller.SearchUsersAsync(request, CancellationToken.None);

            (result.Result as OkObjectResult)?.Value.Should().Be(searchResult);
        }

        private sealed class TestableUserSearchController(IUserSearchService userSearchService)
            : BaseUserSearchController(userSearchService);
    }
}
