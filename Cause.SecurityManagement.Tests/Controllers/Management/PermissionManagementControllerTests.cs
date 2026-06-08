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
    public class PermissionManagementControllerTests
    {
        private IPermissionCatalogService permissionService;
        private TestablePermissionManagementController controller;

        [SetUp]
        public void SetUp()
        {
            permissionService = Substitute.For<IPermissionCatalogService>();
            controller = new TestablePermissionManagementController(permissionService);
        }

        [Test]
        public async Task WhenCatalogRequested_GetPermissions_ShouldReturnOkWithCatalog()
        {
            var catalog = new List<PermissionDto>
            {
                new() { Id = Guid.NewGuid(), IdModulePermission = Guid.NewGuid(), Tag = "module.access", Name = "Access the module" }
            };
            permissionService.GetPermissionsAsync(Arg.Any<CancellationToken>()).Returns(catalog);

            var result = await controller.GetPermissionsAsync(CancellationToken.None);

            (result.Result as OkObjectResult)?.Value.Should().BeEquivalentTo(catalog);
        }

        private sealed class TestablePermissionManagementController(IPermissionCatalogService permissionService)
            : BasePermissionManagementController(permissionService);
    }
}
