using AwesomeAssertions;
using Cause.SecurityManagement.Integration.Tests.Infrastructure;
using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;
using NUnit.Framework;

namespace Cause.SecurityManagement.Integration.Tests.Management;

/// <summary>
/// Exercises the groups OData feed the way a consuming application wires it: an EDM exposing the
/// library's <see cref="GroupListItem"/> contract and OData query options applied to a queryable
/// projected from the real PostgreSQL data. Proves filtering, ordering, paging and count.
/// </summary>
[TestFixture]
public class GroupListODataTests : IntegrationTestBase
{
    [Test]
    public void ODataQuery_ShouldFilterOrderPageAndCount()
    {
        var token = $"z{Guid.NewGuid():N}";
        SeedGroup($"{token} Charlie");
        SeedGroup($"{token} Alpha");
        SeedGroup($"{token} Bravo");

        var source = Context.Groups
            .Where(group => group.Name.StartsWith(token))
            .Select(group => new GroupListItem
            {
                Id = group.Id,
                Name = group.Name,
                AssignableByAllUsers = group.AssignableByAllUsers,
                SearchableGroup = group.Name.ToLower(),
                SearchableUsers = group.Name.ToLower(),
            });

        var (options, request) = BuildQueryOptions(
            $"$filter=contains(searchableGroup,'{token}') or contains(searchableUsers,'{token}')"
            + "&$orderby=name&$top=2&$skip=1&$count=true");

        var page = ((IQueryable<GroupListItem>)options.ApplyTo(source, new ODataQuerySettings())).ToList();

        request.ODataFeature().TotalCount.Should().Be(3);
        page.Select(item => item.Name).Should().Equal($"{token} Bravo", $"{token} Charlie");
    }

    [Test]
    public void ODataQuery_WithFilterMatchingNothing_ShouldReturnEmpty()
    {
        var token = $"z{Guid.NewGuid():N}";
        SeedGroup($"{token} Alpha");

        var source = Context.Groups
            .Where(group => group.Name.StartsWith(token))
            .Select(group => new GroupListItem
            {
                Id = group.Id,
                Name = group.Name,
                AssignableByAllUsers = group.AssignableByAllUsers,
                SearchableGroup = group.Name.ToLower(),
                SearchableUsers = group.Name.ToLower(),
            });

        var (options, _) = BuildQueryOptions("$filter=contains(searchableGroup,'nomatchatall')");

        var page = ((IQueryable<GroupListItem>)options.ApplyTo(source, new ODataQuerySettings())).ToList();

        page.Should().BeEmpty();
    }

    private void SeedGroup(string name)
    {
        Context.Groups.Add(new Group { Id = Guid.NewGuid(), Name = name, AssignableByAllUsers = false });
        Context.SaveChanges();
    }

    private static (ODataQueryOptions<GroupListItem> Options, HttpRequest Request) BuildQueryOptions(string rawQuery)
    {
        var builder = new ODataConventionModelBuilder();
        builder.EnableLowerCamelCase();
        builder.EntitySet<GroupListItem>("GroupList");
        var model = builder.GetEdmModel();

        var request = new DefaultHttpContext().Request;
        request.Method = "GET";
        request.QueryString = new QueryString("?" + rawQuery);
        request.ODataFeature().Model = model;

        var context = new ODataQueryContext(model, typeof(GroupListItem), path: null);
        return (new ODataQueryOptions<GroupListItem>(context, request), request);
    }
}
