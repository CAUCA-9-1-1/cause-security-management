using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract groups feed backing the OData data grid (<c>GET odata/GroupList</c>). The library stays
/// OData-agnostic and references no OData package: the host application subclasses this controller —
/// naming the subclass <c>GroupListController</c> so it matches the <c>GroupList</c> entity set —
/// implements <see cref="Get"/> to return the source projection, and owns every OData concern itself
/// (the <c>[EnableQuery]</c> attribute, the EDM model and <c>AddOData</c> routing).
/// </summary>
/// <remarks>
/// The returned <see cref="GroupListItem"/> rows must carry <c>SearchableGroup</c> and
/// <c>SearchableUsers</c> already normalized (lower-cased and diacritic-free), typically produced by a
/// database view, so the client's OData <c>contains()</c> filter matches as an exact substring.
/// </remarks>
/// <example>
/// <code>
/// public class GroupListController(MyDbContext context) : BaseGroupListController
/// {
///     [EnableQuery]
///     public override IQueryable&lt;GroupListItem&gt; Get() => context.GroupListView.AsNoTracking();
/// }
/// </code>
/// </example>
public abstract class BaseGroupListController : ControllerBase
{
    public abstract IQueryable<GroupListItem> Get();
}
