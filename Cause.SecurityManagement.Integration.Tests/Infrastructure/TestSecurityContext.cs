using Cause.SecurityManagement.Core.Mapping;
using Cause.SecurityManagement.Core;

using Microsoft.EntityFrameworkCore;

namespace Cause.SecurityManagement.Integration.Tests.Infrastructure;

public class TestSecurityContext(DbContextOptions<TestSecurityContext> options)
    : BaseSecurityContext<TestUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use explicit mappings instead of auto-detection to keep tests self-contained
        AddSecurityManagementMappings(modelBuilder);
    }
}
