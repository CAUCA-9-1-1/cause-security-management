using Cause.SecurityManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cause.SecurityManagement.Controllers.Management;

/// <summary>
/// Abstract users feed backing the OData data grid (<c>GET odata/UserList</c>). The library stays
/// OData-agnostic and references no OData package: the host application subclasses this controller —
/// naming the subclass <c>UserListController</c> so it matches the <c>UserList</c> entity set —
/// implements <see cref="Get"/> to return the source projection, and owns every OData concern itself
/// (the <c>[EnableQuery]</c> attribute, the EDM model and <c>AddOData</c> routing).
/// </summary>
/// <typeparam name="TUserListItem">
/// The row shape the host exposes. Generic rather than fixed to <see cref="UserListItem"/> because the
/// per-site columns differ far more for users than for groups — fire safety departments in one
/// application, cities and alert flags in another — so each host derives its own row type and builds
/// its EDM on that concrete type.
/// </typeparam>
/// <remarks>
/// The returned rows must carry every <c>Searchable*</c> value already normalized (lower-cased and
/// diacritic-free), typically produced by a database view, so the client's OData <c>contains()</c>
/// filter matches as an exact substring.
///
/// Restricting which users a caller may see is the host's business: it depends on data the library
/// does not model. Implement that filter inside <see cref="Get"/>.
/// </remarks>
/// <example>
/// <code>
/// public class UserListController(MyDbContext context) : BaseUserListController&lt;MyUserListItem&gt;
/// {
///     [EnableQuery]
///     public override IQueryable&lt;MyUserListItem&gt; Get() => context.UserListView.AsNoTracking();
/// }
/// </code>
/// </example>
public abstract class BaseUserListController<TUserListItem> : ControllerBase
    where TUserListItem : UserListItem
{
    public abstract IQueryable<TUserListItem> Get();
}
