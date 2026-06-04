namespace Cause.SecurityManagement.Models
{
    /// <summary>
    /// Read-only contract shape powering the groups OData feed (<c>GET odata/GroupList</c>). The
    /// consuming application owns the feed entirely: its OData controller, <c>[EnableQuery]</c>, EDM
    /// registration and route configuration. It must expose a queryable of this shape (typically from
    /// a database view or projection) and fill <see cref="SearchableGroup"/> and
    /// <see cref="SearchableUsers"/> already normalized (lower-cased and diacritic-free), so the
    /// client's OData <c>contains()</c> filter matches as an exact substring.
    /// </summary>
    public class GroupListItem : BaseModel
    {
        public string Name { get; set; }
        public bool AssignableByAllUsers { get; set; }
        public string SearchableGroup { get; set; }
        public string SearchableUsers { get; set; }
    }
}
