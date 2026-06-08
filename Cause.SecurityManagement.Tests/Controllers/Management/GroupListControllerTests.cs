using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Cause.SecurityManagement.Controllers.Management;
using Cause.SecurityManagement.Models;
using NUnit.Framework;

namespace Cause.SecurityManagement.Tests.Controllers.Management
{
    [TestFixture]
    public class GroupListControllerTests
    {
        [Test]
        public void Get_ShouldExposeTheImplementedQueryable()
        {
            var rows = new List<GroupListItem>
            {
                new() { Id = Guid.NewGuid(), Name = "Dispatchers", SearchableGroup = "dispatchers", SearchableUsers = "ada lovelace" }
            }.AsQueryable();
            var controller = new TestableGroupListController(rows);

            var result = controller.Get();

            result.Should().BeSameAs(rows);
        }

        private sealed class TestableGroupListController(IQueryable<GroupListItem> source) : BaseGroupListController
        {
            public override IQueryable<GroupListItem> Get() => source;
        }
    }
}
