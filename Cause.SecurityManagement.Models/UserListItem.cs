namespace Cause.SecurityManagement.Models
{
    /// <summary>
    /// Read-only contract shape powering the users OData feed (<c>GET odata/UserList</c>). It carries
    /// only what every consuming application shows; a site adding columns of its own derives from this
    /// type and registers the derived type in its EDM, which is what
    /// <c>BaseUserListController&lt;TUserListItem&gt;</c> is generic for.
    ///
    /// The consuming application owns the feed entirely: its OData controller, <c>[EnableQuery]</c>,
    /// EDM registration and route configuration. It must expose a queryable of this shape (typically
    /// from a database view or projection) and fill every <c>Searchable*</c> value already normalized
    /// (lower-cased and diacritic-free), so the client's OData <c>contains()</c> filter matches as an
    /// exact substring.
    /// </summary>
    public class UserListItem : BaseModel
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SearchableUserName { get; set; }
        public string SearchableFirstName { get; set; }
        public string SearchableLastName { get; set; }
    }
}
