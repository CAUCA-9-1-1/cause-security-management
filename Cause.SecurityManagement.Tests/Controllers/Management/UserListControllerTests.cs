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
    public class UserListControllerTests
    {
        [Test]
        public void Get_ShouldExposeTheImplementedQueryable()
        {
            var rows = new List<TestUserListItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserName = "alovelace",
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    SearchableUserName = "alovelace",
                    SearchableFirstName = "ada",
                    SearchableLastName = "lovelace",
                    FireSafetyDepartments = "Sainte-Marie",
                }
            }.AsQueryable();
            var controller = new TestableUserListController(rows);

            var result = controller.Get();

            result.Should().BeSameAs(rows);
        }

        [Test]
        public void Get_ShouldCarryTheHostSpecificColumns()
        {
            var rows = new List<TestUserListItem>
            {
                new() { Id = Guid.NewGuid(), UserName = "alovelace", FireSafetyDepartments = "ALL" }
            }.AsQueryable();
            var controller = new TestableUserListController(rows);

            var result = controller.Get();

            result.Single().FireSafetyDepartments.Should().Be("ALL");
        }

        private sealed class TestUserListItem : UserListItem
        {
            public string FireSafetyDepartments { get; set; }
        }

        private sealed class TestableUserListController(IQueryable<TestUserListItem> source)
            : BaseUserListController<TestUserListItem>
        {
            public override IQueryable<TestUserListItem> Get() => source;
        }
    }
}
