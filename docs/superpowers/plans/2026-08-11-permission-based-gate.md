# Permission-Based Authorization Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `[AdministratorOrUserWithPermission(tag)]` and `[UserWithPermission(tag)]` endpoint attributes that gate `RegularUser` principals on a named permission, leaving every other principal type behaving as it does today.

**Architecture:** A dynamic `IAuthorizationPolicyProvider` turns the policy name `Permission:<mode>:<tag>` into a policy on demand, so consuming applications never enumerate their tags in startup code. A single `AuthorizationHandler<PermissionRequirement>` applies the role rules, differing between the two attributes only by an `AllowAdministrator` flag. The handler is a singleton that reaches the request-scoped `IUserPermissionService` through `HttpContext.RequestServices`, memoizing the merged permission set per request.

**Tech Stack:** .NET 10 (`net10.0`), ASP.NET Core authorization, EF Core, NUnit 4.6.1, NSubstitute 6.2.0, AwesomeAssertions 9.5.0, Microsoft.AspNetCore.TestHost 10.0.10.

**Spec:** [docs/specs/2026-08-11-permission-based-gate.md](../../specs/2026-08-11-permission-based-gate.md)
**ADR:** [docs/adr/records/2026-08-11-permission-based-authorization-gate.md](../../adr/records/2026-08-11-permission-based-authorization-gate.md)

## Global Constraints

- **Zero warnings.** The current baseline is `0 Avertissement(s)`. Any new warning is a task failure.
- **All projects target `net10.0`** — single TFM. Default interface implementations are available and used deliberately.
- **English identifiers only.** No abbreviations except established ones.
- **No comments** unless they explain *why*. No section separators, no change logs.
- **Namespace style varies by file age.** `Cause.SecurityManagement.Core/Authentication/*.cs` uses file-scoped namespaces (`namespace X;`). `Cause.SecurityManagement.Core/Services/*.cs` and `Repositories/*.cs` use block namespaces (`namespace X { }`). **Match the file you are editing.**
- **Test conventions:** NUnit `[TestFixture]` / `[SetUp]` / `[Test]`, AAA pattern, `Substitute.For<T>()`, `result.Should().BeTrue()`. Test names follow `Scenario_WhenAction_ShouldOutcome`.
- **The handler must fail closed.** Succeed only on an explicit positive match.
- **Never call `RequireRole` in the dynamic policy.** Role logic lives in the handler.
- Build: `dotnet build Cause.SecurityManagement.sln -c Debug --nologo -p:GeneratePackageOnBuild=false`
- Test: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo`

## File Structure

| File | Responsibility |
|---|---|
| `Core/Authentication/PermissionRequirement.cs` | The requirement: `Tag`, `AllowAdministrator` |
| `Core/PermissionAttributes.cs` | Both attributes + the policy-name helpers they share |
| `Core/Authentication/PermissionAuthorizationPolicyProvider.cs` | Name → policy, delegating unknown names |
| `Core/Authentication/ScopedPermissionCache.cs` | Per-request memoization of the merged set |
| `Core/Authentication/PermissionAuthorizationHandler.cs` | The role rules |
| `Core/Authentication/PermissionTagValidationHostedService.cs` | Opt-in startup tag validation |
| `Core/Authentication/ServiceCollectionAuthorizationExtensions.cs` | Registration (modify) |
| `Core/Services/IUserPermissionService.cs` + impl | Async permission path (modify) |
| `Core/Repositories/I*PermissionRepository.cs` + impls | Async reads (modify) |

Task order is dependency order: the async data path (Tasks 1–2) comes before the cache that consumes it (Task 3), which comes before the handler (Task 5).

---

### Task 1: Async repository reads

**Files:**
- Modify: `Cause.SecurityManagement.Core/Repositories/IUserPermissionRepository.cs`
- Modify: `Cause.SecurityManagement.Core/Repositories/UserPermissionRepository.cs`
- Modify: `Cause.SecurityManagement.Core/Repositories/IGroupPermissionRepository.cs`
- Modify: `Cause.SecurityManagement.Core/Repositories/GroupPermissionRepository.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `Task<List<UserPermission>> IUserPermissionRepository.GetForUserAsync(Guid userId, CancellationToken cancellationToken)`
  - `Task<List<GroupPermission>> IGroupPermissionRepository.GetForUserAsync(Guid userId, CancellationToken cancellationToken)`

**Why these are needed:** `UserPermissionService` must not reference `Microsoft.EntityFrameworkCore` (it currently does not), so `ToListAsync` has to happen inside the repositories, which already do.

- [ ] **Step 1: Read the existing repositories to match style**

Read `Cause.SecurityManagement.Core/Repositories/UserPermissionRepository.cs` and `GroupPermissionRepository.cs`. Note the block-namespace style and that `GetForUser` on the group repository returns `IQueryable` while the user one returns `List`.

- [ ] **Step 2: Add the async method to `IUserPermissionRepository`**

Add to the interface (it already has `using System.Threading.Tasks;`; add `using System.Threading;`):

```csharp
Task<List<UserPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
```

- [ ] **Step 3: Implement it in `UserPermissionRepository`**

Mirror the existing synchronous `GetForUser`:

```csharp
public Task<List<UserPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
{
    return context.UserPermissions.AsNoTracking()
        .Where(uc => uc.IdUser == userId)
        .Include(uc => uc.Permission)
        .ToListAsync(cancellationToken);
}
```

Add `using System.Threading;` if absent.

- [ ] **Step 4: Add the async method to `IGroupPermissionRepository`**

```csharp
Task<List<GroupPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
```

Add `using System.Collections.Generic;` and `using System.Threading;` if absent.

- [ ] **Step 5: Implement it in `GroupPermissionRepository`**

The existing `GetForUser` is a query-expression `SelectMany` over `userGroup.Group.Permissions` and does **not** `Include` the `Permission` navigation — it doesn't need to, because its only caller projects `g.Permission.Tag`, which EF translates as a join.

`GetForUserAsync` returns entities, so it **does** need the navigation populated. Do not reuse `GetForUser` — `Include` after a `SelectMany` is fragile. Write an equivalent `Where` + `Include` instead, mirroring the shape already used in `UserPermissionRepository.GetUserPermissionsAsync`:

```csharp
public Task<List<GroupPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
{
    return context.GroupPermissions.AsNoTracking()
        .Where(groupPermission => context.UserGroups
            .Any(userGroup => userGroup.IdUser == userId && userGroup.IdGroup == groupPermission.IdGroup))
        .Include(groupPermission => groupPermission.Permission)
        .ToListAsync(cancellationToken);
}
```

Add `using System.Collections.Generic;` and `using System.Threading;`.

This is semantically the same set as `GetForUser` — group permissions of every group the user belongs to — expressed so EF can translate it and populate `Permission` reliably.

- [ ] **Step 6: Build**

Run: `dotnet build Cause.SecurityManagement.Core/Cause.SecurityManagement.Core.csproj -c Debug --nologo -p:GeneratePackageOnBuild=false`
Expected: `0 Avertissement(s)`, `0 Erreur(s)`.

- [ ] **Step 7: Commit**

```bash
git add Cause.SecurityManagement.Core/Repositories/
git commit -m "#115 - Add async permission reads to the permission repositories"
```

---

### Task 2: Async permission service path

**Files:**
- Modify: `Cause.SecurityManagement.Core/Services/IUserPermissionService.cs`
- Modify: `Cause.SecurityManagement.Core/Services/UserPermissionService.cs`
- Test: `Cause.SecurityManagement.Tests/Services/UserPermissionServiceTests.cs` (create)

**Interfaces:**
- Consumes: `IUserPermissionRepository.GetForUserAsync`, `IGroupPermissionRepository.GetForUserAsync` from Task 1.
- Produces:
  - `Task<bool> IUserPermissionService.HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)`
  - `Task<List<UserMergedPermission>> IUserPermissionService.GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)`

**Design note:** these are **default interface implementations** delegating to the synchronous members, so external implementors of `IUserPermissionService` keep compiling. `UserPermissionService` overrides both with genuinely async versions.

- [ ] **Step 1: Write the failing tests**

Create `Cause.SecurityManagement.Tests/Services/UserPermissionServiceTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AwesomeAssertions;
    using Cause.SecurityManagement.Core.Repositories;
    using Cause.SecurityManagement.Core.Services;
    using Cause.SecurityManagement.Models;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class UserPermissionServiceTests
    {
        private const string SomeTag = "CanEditBuilding";

        private IUserPermissionRepository userPermissionRepository;
        private IGroupPermissionRepository groupPermissionRepository;
        private UserPermissionService service;
        private Guid someUserId;

        [SetUp]
        public void SetUp()
        {
            userPermissionRepository = Substitute.For<IUserPermissionRepository>();
            groupPermissionRepository = Substitute.For<IGroupPermissionRepository>();
            someUserId = Guid.NewGuid();

            userPermissionRepository.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<UserPermission>());
            groupPermissionRepository.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<GroupPermission>());

            service = new UserPermissionService(groupPermissionRepository, userPermissionRepository);
        }

        private static UserPermission UserPermissionFor(string tag, bool isAllowed)
            => new() { IsAllowed = isAllowed, Permission = new ModulePermission { Tag = tag } };

        private static GroupPermission GroupPermissionFor(string tag, bool isAllowed)
            => new() { IsAllowed = isAllowed, Permission = new ModulePermission { Tag = tag } };

        [Test]
        public async Task UserWithAllowedPermission_WhenHasPermissionAsync_ShouldReturnTrue()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([UserPermissionFor(SomeTag, isAllowed: true)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Test]
        public async Task UserWithDeniedPermission_WhenHasPermissionAsync_ShouldReturnFalse()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([UserPermissionFor(SomeTag, isAllowed: false)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Test]
        public async Task UserWithoutThePermission_WhenHasPermissionAsync_ShouldReturnFalse()
        {
            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Test]
        public async Task GroupDenyAndUserAllow_WhenHasPermissionAsync_ShouldReturnFalseBecauseDenyWins()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([UserPermissionFor(SomeTag, isAllowed: true)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([GroupPermissionFor(SomeTag, isAllowed: false)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse("PermissionMergeTool computes Access with All(), so a deny wins");
        }

        [Test]
        public async Task UserAndGroupPermissions_WhenGetPermissionsForUserAsync_ShouldMergeBoth()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([UserPermissionFor("FromUser", isAllowed: true)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([GroupPermissionFor("FromGroup", isAllowed: true)]);

            var result = await service.GetPermissionsForUserAsync(someUserId, CancellationToken.None);

            result.Select(permission => permission.FeatureName)
                .Should().BeEquivalentTo(["FromUser", "FromGroup"]);
        }

        [Test]
        public void CancelledToken_WhenHasPermissionAsync_ShouldPropagateOperationCanceled()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns<List<UserPermission>>(_ => throw new OperationCanceledException());

            var act = async () => await service.HasPermissionAsync(someUserId, SomeTag, cancellation.Token);

            act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter UserPermissionServiceTests`
Expected: compile error — `HasPermissionAsync` / `GetPermissionsForUserAsync` do not exist.

- [ ] **Step 3: Add the async members to the interface**

`IUserPermissionService.cs` uses a **block namespace**. Add the usings and default implementations:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Core.Services
{
    public interface IUserPermissionService
    {
        bool HasPermission(Guid userId, string permissionTag);
        List<UserMergedPermission> GetPermissionsForUser(Guid userId);

        Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
            => Task.FromResult(HasPermission(userId, permissionTag));

        Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(GetPermissionsForUser(userId));
    }
}
```

- [ ] **Step 4: Implement the async path in `UserPermissionService`**

Replace the class body, keeping the primary-constructor style already in the file:

```csharp
public async Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
{
    var permissions = await GetPermissionsForUserAsync(userId, cancellationToken);
    return permissions.Exists(permission => permission.FeatureName == permissionTag && permission.Access);
}

public async Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
{
    var userPermissions = await userPermissionRepository.GetForUserAsync(userId, cancellationToken);
    var groupPermissions = await groupPermissionRepository.GetForUserAsync(userId, cancellationToken);

    return new PermissionMergeTool().MergeUserAndGroupPermissions(
        ToMergedPermissions(groupPermissions.Select(permission => (permission.IsAllowed, permission.Permission.Tag))),
        ToMergedPermissions(userPermissions.Select(permission => (permission.IsAllowed, permission.Permission.Tag))));
}

private static List<UserMergedPermission> ToMergedPermissions(IEnumerable<(bool IsAllowed, string Tag)> permissions)
    => permissions.Select(permission => new UserMergedPermission { Access = permission.IsAllowed, FeatureName = permission.Tag }).ToList();
```

Add `using System.Threading;` and `using System.Threading.Tasks;`. `PermissionMergeTool` lives in `Cause.SecurityManagement.Core`, already imported transitively — add `using Cause.SecurityManagement.Core;` if the compiler asks.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter UserPermissionServiceTests`
Expected: 6 passed.

- [ ] **Step 6: Run the full suite and build**

Run: `dotnet build Cause.SecurityManagement.sln -c Debug --nologo -p:GeneratePackageOnBuild=false` then `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo`
Expected: `0 Avertissement(s)`; all tests pass. Report the count.

- [ ] **Step 7: Commit**

```bash
git add Cause.SecurityManagement.Core/Services/ Cause.SecurityManagement.Tests/Services/UserPermissionServiceTests.cs
git commit -m "#115 - Add an async permission path to IUserPermissionService"
```

---

### Task 3: Per-request permission cache

**Files:**
- Create: `Cause.SecurityManagement.Core/Authentication/ScopedPermissionCache.cs`
- Test: `Cause.SecurityManagement.Tests/Authentication/ScopedPermissionCacheTests.cs`

**Interfaces:**
- Consumes: `IUserPermissionService.HasPermissionAsync` from Task 2.
- Produces: `Task<bool> ScopedPermissionCache.HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)` — registered `Scoped`, so one instance per request.

**Why:** a single request can check permissions several times (multiple requirements, or the handler plus a business-layer `HasPermission` call as in `UserGroupPermissionService`). Each miss costs two database reads.

**Known risk — verify at Step 2.** These tests stub `IUserPermissionService.GetPermissionsForUserAsync`, which Task 2 added as a **default interface implementation**. NSubstitute must be able to intercept it for `.Returns(...)` to take effect. Castle DynamicProxy generally does proxy default interface members, but confirm it rather than assume: if the stub is ignored (the substitute returns an empty list no matter what you configure), replace the substitute with a hand-written fake in both this task and Task 5:

```csharp
private sealed class FakePermissionService(List<UserMergedPermission> permissions) : IUserPermissionService
{
    public int GetPermissionsCallCount { get; private set; }

    public bool HasPermission(Guid userId, string permissionTag)
        => throw new NotSupportedException("The gate uses the async path.");

    public List<UserMergedPermission> GetPermissionsForUser(Guid userId)
        => throw new NotSupportedException("The gate uses the async path.");

    public Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
        => Task.FromResult(permissions.Exists(permission => permission.FeatureName == permissionTag && permission.Access));

    public Task<List<UserMergedPermission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        GetPermissionsCallCount++;
        return Task.FromResult(permissions);
    }
}
```

Report which path you took. Do not leave a test that passes for the wrong reason.

- [ ] **Step 1: Write the failing test**

Create `Cause.SecurityManagement.Tests/Authentication/ScopedPermissionCacheTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Authentication;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class ScopedPermissionCacheTests
{
    private const string AllowedTag = "CanEditBuilding";
    private const string DeniedTag = "CanDeleteBuilding";

    private IUserPermissionService permissionService;
    private ScopedPermissionCache cache;
    private Guid someUserId;

    [SetUp]
    public void SetUp()
    {
        permissionService = Substitute.For<IUserPermissionService>();
        someUserId = Guid.NewGuid();

        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserMergedPermission>
            {
                new() { FeatureName = AllowedTag, Access = true },
            });

        cache = new ScopedPermissionCache(permissionService);
    }

    [Test]
    public async Task SameUserTwice_WhenHasPermissionAsync_ShouldLoadPermissionsOnce()
    {
        await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);
        await cache.HasPermissionAsync(someUserId, DeniedTag, CancellationToken.None);

        await permissionService.Received(1).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TwoDifferentUsers_WhenHasPermissionAsync_ShouldLoadPermissionsOncePerUser()
    {
        var otherUserId = Guid.NewGuid();

        await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);
        await cache.HasPermissionAsync(otherUserId, AllowedTag, CancellationToken.None);

        await permissionService.Received(1).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
        await permissionService.Received(1).GetPermissionsForUserAsync(otherUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AllowedTag_WhenHasPermissionAsync_ShouldReturnTrue()
    {
        var result = await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public async Task TagNotGranted_WhenHasPermissionAsync_ShouldReturnFalse()
    {
        var result = await cache.HasPermissionAsync(someUserId, DeniedTag, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task TagPresentButNotAllowed_WhenHasPermissionAsync_ShouldReturnFalse()
    {
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserMergedPermission>
            {
                new() { FeatureName = AllowedTag, Access = false },
            });

        var result = await cache.HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task SameUserInTwoScopes_WhenHasPermissionAsync_ShouldLoadPermissionsOncePerScope()
    {
        var services = new ServiceCollection();
        services.AddSingleton(permissionService);
        services.AddScoped<ScopedPermissionCache>();
        using var provider = services.BuildServiceProvider();

        using (var firstScope = provider.CreateScope())
            await firstScope.ServiceProvider.GetRequiredService<ScopedPermissionCache>()
                .HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        using (var secondScope = provider.CreateScope())
            await secondScope.ServiceProvider.GetRequiredService<ScopedPermissionCache>()
                .HasPermissionAsync(someUserId, AllowedTag, CancellationToken.None);

        await permissionService.Received(2).GetPermissionsForUserAsync(someUserId, Arg.Any<CancellationToken>());
    }
}
```

The last test needs `using Microsoft.Extensions.DependencyInjection;`. It pins the cache to *request* lifetime deliberately: a permission revoked between two requests must be seen by the second one.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter ScopedPermissionCacheTests`
Expected: compile error — `ScopedPermissionCache` does not exist.

- [ ] **Step 3: Implement the cache**

Create `Cause.SecurityManagement.Core/Authentication/ScopedPermissionCache.cs`. This folder uses **file-scoped namespaces**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Memoizes a user's merged permission set for the lifetime of one request.
/// Registered as scoped; a user's permissions cannot change mid-request, so no invalidation is needed.
/// </summary>
public class ScopedPermissionCache(IUserPermissionService permissionService)
{
    private readonly Dictionary<Guid, List<UserMergedPermission>> permissionsByUser = [];

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);
        return permissions.Any(permission => permission.FeatureName == permissionTag && permission.Access);
    }

    private async Task<List<UserMergedPermission>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (permissionsByUser.TryGetValue(userId, out var cached))
            return cached;

        var permissions = await permissionService.GetPermissionsForUserAsync(userId, cancellationToken);
        permissionsByUser[userId] = permissions;
        return permissions;
    }
}
```

A plain `Dictionary` is correct here: a scoped instance is used by one request, and authorization runs sequentially before the action.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter ScopedPermissionCacheTests`
Expected: 6 passed.

- [ ] **Step 5: Commit**

```bash
git add Cause.SecurityManagement.Core/Authentication/ScopedPermissionCache.cs Cause.SecurityManagement.Tests/Authentication/ScopedPermissionCacheTests.cs
git commit -m "#115 - Add a per-request permission cache"
```

---

### Task 4: Requirement, attributes, and policy provider

**Files:**
- Create: `Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs`
- Create: `Cause.SecurityManagement.Core/PermissionAttributes.cs`
- Create: `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs`
- Test: `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationPolicyProviderTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces:
  - `PermissionRequirement` with `string Tag { get; }` and `bool AllowAdministrator { get; }`
  - `PermissionPolicy.Prefix` = `"Permission:"`, `PermissionPolicy.NameFor(string tag, bool allowAdministrator)`, `PermissionPolicy.TryParse(string policyName, out string tag, out bool allowAdministrator)`
  - `AdministratorOrUserWithPermissionAttribute(string tag)`, `UserWithPermissionAttribute(string tag)`
  - `PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider`

**Policy names:** `Permission:AdministratorOrUser:<tag>` and `Permission:User:<tag>`.

- [ ] **Step 1: Write the failing tests**

Create `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationPolicyProviderTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Authentication;

using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using NUnit.Framework;

[TestFixture]
public class PermissionAuthorizationPolicyProviderTests
{
    private const string SomeTag = "CanEditBuilding";

    private AuthorizationOptions options;
    private PermissionAuthorizationPolicyProvider provider;

    [SetUp]
    public void SetUp()
    {
        options = new AuthorizationOptions();
        options.AddPolicy("ExistingPolicy", policy => policy.RequireAssertion(_ => true));
        provider = new PermissionAuthorizationPolicyProvider(Options.Create(options));
    }

    private static PermissionRequirement RequirementOf(AuthorizationPolicy policy)
        => policy.Requirements.OfType<PermissionRequirement>().Single();

    [Test]
    public async Task AdministratorOrUserPolicyName_WhenGetPolicyAsync_ShouldCarryTagAndAllowAdministrator()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        RequirementOf(policy).Tag.Should().Be(SomeTag);
        RequirementOf(policy).AllowAdministrator.Should().BeTrue();
    }

    [Test]
    public async Task UserPolicyName_WhenGetPolicyAsync_ShouldNotAllowAdministrator()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: false));

        RequirementOf(policy).Tag.Should().Be(SomeTag);
        RequirementOf(policy).AllowAdministrator.Should().BeFalse();
    }

    [Test]
    public async Task PermissionPolicyName_WhenGetPolicyAsync_ShouldDenyAnonymous()
    {
        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.Requirements.Should().ContainItemsAssignableTo<DenyAnonymousAuthorizationRequirement>(
            "the fallback policy does not run on endpoints carrying an AuthorizeAttribute");
    }

    [Test]
    public async Task TagContainingAColon_WhenGetPolicyAsync_ShouldPreserveTheWholeTag()
    {
        var tagWithColon = "Module:CanEdit";

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(tagWithColon, allowAdministrator: true));

        RequirementOf(policy).Tag.Should().Be(tagWithColon);
    }

    [Test]
    public async Task UnrecognizedMode_WhenGetPolicyAsync_ShouldReturnNull()
    {
        var policy = await provider.GetPolicyAsync($"{PermissionPolicy.Prefix}Bogus:{SomeTag}");

        policy.Should().BeNull("an unrecognized mode must not degrade to a weaker gate");
    }

    [Test]
    public async Task ExistingPolicyName_WhenGetPolicyAsync_ShouldDelegateToTheDefaultProvider()
    {
        var policy = await provider.GetPolicyAsync("ExistingPolicy");

        policy.Should().NotBeNull();
        policy.Requirements.OfType<PermissionRequirement>().Should().BeEmpty();
    }

    [Test]
    public async Task UnknownPolicyName_WhenGetPolicyAsync_ShouldReturnNull()
    {
        var policy = await provider.GetPolicyAsync("NoSuchPolicy");

        policy.Should().BeNull();
    }

    [Test]
    public async Task FallbackPolicyWithSchemes_WhenGetPolicyAsync_ShouldCopyTheSchemeList()
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("SchemeOne", "SchemeTwo")
            .Build();

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.AuthenticationSchemes.Should().BeEquivalentTo(["SchemeOne", "SchemeTwo"]);
    }

    [Test]
    public async Task NoFallbackPolicy_WhenGetPolicyAsync_ShouldStillProduceAPolicy()
    {
        options.FallbackPolicy = null;

        var policy = await provider.GetPolicyAsync(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));

        policy.Should().NotBeNull();
        policy.AuthenticationSchemes.Should().BeEmpty();
    }

    [Test]
    public void BothAttributes_WhenConstructed_ShouldSetMatchingPolicyNames()
    {
        new AdministratorOrUserWithPermissionAttribute(SomeTag).Policy
            .Should().Be(PermissionPolicy.NameFor(SomeTag, allowAdministrator: true));
        new UserWithPermissionAttribute(SomeTag).Policy
            .Should().Be(PermissionPolicy.NameFor(SomeTag, allowAdministrator: false));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionAuthorizationPolicyProviderTests`
Expected: compile errors — the types do not exist.

- [ ] **Step 3: Create `PermissionRequirement`**

`Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs`, file-scoped namespace:

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Requires the named permission. <paramref name="allowAdministrator"/> decides whether an
/// Administrator principal passes without holding the permission.
/// </summary>
public class PermissionRequirement(string tag, bool allowAdministrator) : IAuthorizationRequirement
{
    public string Tag { get; } = tag;
    public bool AllowAdministrator { get; } = allowAdministrator;
}
```

- [ ] **Step 4: Create the attributes and the policy-name helper**

`Cause.SecurityManagement.Core/PermissionAttributes.cs`. This sits beside the existing `AuthorizeByRolesAttribute.cs`, which also holds several attributes in one file and uses a **file-scoped namespace**:

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Cause.SecurityManagement.Core;

/// <summary>
/// Builds and parses the dynamic policy names used by the permission attributes.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "Permission:";

    private const string AdministratorOrUserMode = "AdministratorOrUser";
    private const string UserMode = "User";

    public static string NameFor(string tag, bool allowAdministrator)
        => $"{Prefix}{(allowAdministrator ? AdministratorOrUserMode : UserMode)}:{tag}";

    public static bool TryParse(string policyName, out string tag, out bool allowAdministrator)
    {
        tag = null;
        allowAdministrator = false;

        if (policyName is null || !policyName.StartsWith(Prefix))
            return false;

        var remainder = policyName[Prefix.Length..];
        var separatorIndex = remainder.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == remainder.Length - 1)
            return false;

        var mode = remainder[..separatorIndex];
        if (mode != AdministratorOrUserMode && mode != UserMode)
            return false;

        allowAdministrator = mode == AdministratorOrUserMode;
        tag = remainder[(separatorIndex + 1)..];
        return true;
    }
}

/// <summary>
/// Administrators pass without a permission lookup. RegularUsers pass only when they hold
/// <paramref name="tag"/>. Every other principal is denied. Use this in applications that use Keycloak.
/// </summary>
public class AdministratorOrUserWithPermissionAttribute : AuthorizeAttribute
{
    public AdministratorOrUserWithPermissionAttribute(string tag)
        => Policy = PermissionPolicy.NameFor(tag, allowAdministrator: true);
}

/// <summary>
/// RegularUsers pass only when they hold <paramref name="tag"/>. Every other principal is denied,
/// including Administrators. Intended for applications that have no Administrator principals.
/// Warning: Keycloak-authenticated principals hold Administrator and not RegularUser, so this
/// attribute denies all of them.
/// </summary>
public class UserWithPermissionAttribute : AuthorizeAttribute
{
    public UserWithPermissionAttribute(string tag)
        => Policy = PermissionPolicy.NameFor(tag, allowAdministrator: false);
}
```

Splitting on the *first* colon after the prefix is what preserves a tag containing a colon.

- [ ] **Step 5: Create the policy provider**

`Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs`:

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Creates permission policies on demand so consuming applications never enumerate their tags.
/// Any other policy name is delegated to the default provider.
/// </summary>
public class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions authorizationOptions = options.Value;
    private readonly DefaultAuthorizationPolicyProvider fallbackProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => fallbackProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissionPolicy.Prefix))
            return fallbackProvider.GetPolicyAsync(policyName);

        if (!PermissionPolicy.TryParse(policyName, out var tag, out var allowAdministrator))
            return Task.FromResult<AuthorizationPolicy>(null);

        return Task.FromResult(BuildPolicy(tag, allowAdministrator));
    }

    private AuthorizationPolicy BuildPolicy(string tag, bool allowAdministrator)
    {
        var builder = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(tag, allowAdministrator));

        var schemes = authorizationOptions.FallbackPolicy?.AuthenticationSchemes;
        if (schemes is { Count: > 0 })
            builder.AddAuthenticationSchemes([.. schemes]);

        return builder.Build();
    }
}
```

The scheme list is copied because `AddAuthorizationForRegularUserKeycloakAndApiCertificate` names three schemes explicitly; a policy omitting them risks a 401 for principals authenticated under a non-default scheme. `PermissionPolicy` is in `Cause.SecurityManagement.Core`, the parent namespace, so no extra `using` is needed.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionAuthorizationPolicyProviderTests`
Expected: 10 passed. If `AddRequirements` is reported obsolete, use `builder.Requirements.Add(new PermissionRequirement(tag, allowAdministrator))` — zero warnings is a hard constraint.

- [ ] **Step 7: Commit**

```bash
git add Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs Cause.SecurityManagement.Core/PermissionAttributes.cs Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationPolicyProviderTests.cs
git commit -m "#115 - Add the permission attributes, requirement, and dynamic policy provider"
```

---

### Task 5: The authorization handler

**Files:**
- Create: `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs`
- Test: `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs`

**Interfaces:**
- Consumes: `PermissionRequirement` (Task 4), `ScopedPermissionCache.HasPermissionAsync` (Task 3).
- Produces: `PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>`, constructed with `(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)`.

**The rule.** `Administrator` → succeed when `AllowAdministrator`, otherwise deny, never a database read. `RegularUser` → succeed only when the permission is held. Everything else → deny with no database read.

Read scoped services from `HttpContext.RequestServices`, **not** a new child scope: `CreateAsyncScope()` per check would produce a fresh `ScopedPermissionCache` each time and memoize nothing.

- [ ] **Step 1: Write the failing tests**

Create `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Authentication;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class PermissionAuthorizationHandlerTests
{
    private const string SomeTag = "CanEditBuilding";

    private IUserPermissionService permissionService;
    private PermissionAuthorizationHandler handler;
    private ServiceProvider serviceProvider;
    private IHttpContextAccessor httpContextAccessor;
    private Guid someUserId;

    [SetUp]
    public void SetUp()
    {
        someUserId = Guid.NewGuid();
        permissionService = Substitute.For<IUserPermissionService>();
        GrantPermissions();

        var services = new ServiceCollection();
        services.AddSingleton(permissionService);
        services.AddScoped<ScopedPermissionCache>();
        serviceProvider = services.BuildServiceProvider();

        httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext
        {
            RequestServices = serviceProvider.CreateScope().ServiceProvider,
        });

        handler = new PermissionAuthorizationHandler(
            httpContextAccessor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    [TearDown]
    public void TearDown() => serviceProvider?.Dispose();

    private void GrantPermissions(params string[] allowedTags)
    {
        var permissions = new List<UserMergedPermission>();
        foreach (var tag in allowedTags)
            permissions.Add(new UserMergedPermission { FeatureName = tag, Access = true });

        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(permissions);
    }

    private ClaimsPrincipal PrincipalWith(string role, string sid)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (sid is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sid, sid));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private async Task<bool> EvaluateAsync(ClaimsPrincipal principal, bool allowAdministrator)
    {
        var requirement = new PermissionRequirement(SomeTag, allowAdministrator);
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RegularUserHoldingThePermission_WhenHandling_ShouldSucceed(bool allowAdministrator)
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator);

        result.Should().BeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RegularUserWithoutThePermission_WhenHandling_ShouldNotSucceed(bool allowAdministrator)
    {
        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator);

        result.Should().BeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RegularUserWithTheTagDenied_WhenHandling_ShouldNotSucceed(bool allowAdministrator)
    {
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserMergedPermission> { new() { FeatureName = SomeTag, Access = false } });

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator);

        result.Should().BeFalse();
    }

    [Test]
    public async Task Administrator_WhenHandlingWithAdministratorAllowed_ShouldSucceedWithoutLoadingPermissions()
    {
        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.Administrator, someUserId.ToString()), allowAdministrator: true);

        result.Should().BeTrue();
        await permissionService.DidNotReceive().GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Administrator_WhenHandlingWithAdministratorNotAllowed_ShouldNotSucceedWithoutLoadingPermissions()
    {
        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.Administrator, someUserId.ToString()), allowAdministrator: false);

        result.Should().BeFalse("UserWithPermission excludes Administrators, which is the whole reason it exists");
        await permissionService.DidNotReceive().GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [TestCase(SecurityRoles.ExternalSystem)]
    [TestCase(SecurityRoles.ApiCertificate)]
    [TestCase(SecurityRoles.UserCreation)]
    [TestCase(SecurityRoles.UserRecovery)]
    [TestCase(SecurityRoles.UserPasswordSetup)]
    [TestCase(SecurityRoles.UserLoginWithMultiFactor)]
    public async Task NonUserRole_WhenHandling_ShouldNotSucceedWithoutLoadingPermissions(string role)
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(role, someUserId.ToString()), allowAdministrator: true);

        result.Should().BeFalse();
        await permissionService.DidNotReceive().GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PrincipalWithoutRoleClaim_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sid, someUserId.ToString())], "TestAuth"));

        var result = await EvaluateAsync(principal, allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task RegularUserWithoutSidClaim_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, sid: null), allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task RegularUserWithUnparseableSidClaim_WhenHandling_ShouldNotSucceed()
    {
        GrantPermissions(SomeTag);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, "not-a-guid"), allowAdministrator: true);

        result.Should().BeFalse();
    }

    [Test]
    public async Task NullHttpContext_WhenHandling_ShouldStillEvaluateThroughAChildScope()
    {
        GrantPermissions(SomeTag);
        httpContextAccessor.HttpContext.Returns((HttpContext)null);

        var result = await EvaluateAsync(PrincipalWith(SecurityRoles.User, someUserId.ToString()), allowAdministrator: true);

        result.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionAuthorizationHandlerTests`
Expected: compile error — `PermissionAuthorizationHandler` does not exist.

- [ ] **Step 3: Implement the handler**

`Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs`:

```csharp
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Grants access to Administrators when the requirement allows them, and to RegularUsers holding
/// the named permission. Every other principal is denied without a permission lookup.
/// </summary>
public class PermissionAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole(SecurityRoles.Administrator))
        {
            if (requirement.AllowAdministrator)
                context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole(SecurityRoles.User))
            return;

        if (!Guid.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sid), out var userId))
            return;

        if (await HasPermissionAsync(userId, requirement.Tag))
            context.Succeed(requirement);
    }

    private async Task<bool> HasPermissionAsync(Guid userId, string tag)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;

        if (httpContext?.RequestServices is not null)
        {
            var cache = httpContext.RequestServices.GetRequiredService<ScopedPermissionCache>();
            return await cache.HasPermissionAsync(userId, tag, cancellationToken);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var scopedCache = scope.ServiceProvider.GetRequiredService<ScopedPermissionCache>();
        return await scopedCache.HasPermissionAsync(userId, tag, cancellationToken);
    }
}
```

`SecurityRoles` lives in the parent `Cause.SecurityManagement.Core` namespace, so no extra `using` is needed.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionAuthorizationHandlerTests`
Expected: 20 passed (several are `[TestCase]`-parameterized).

- [ ] **Step 5: Commit**

```bash
git add Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs
git commit -m "#115 - Add the permission authorization handler"
```

---

### Task 6: Registration extension

**Files:**
- Modify: `Cause.SecurityManagement.Core/Authentication/ServiceCollectionAuthorizationExtensions.cs`
- Test: `Cause.SecurityManagement.Tests/Authentication/PermissionRegistrationTests.cs` (create)

**Interfaces:**
- Consumes: everything from Tasks 3–5.
- Produces: `IServiceCollection AddPermissionBasedAuthorization(this IServiceCollection services)`.

Task 7 adds the `validateTagsAtStartup` parameter together with the hosted service it registers. Introducing the parameter here would leave it unused, and an unused parameter is a warning — which the zero-warning constraint forbids.

- [ ] **Step 1: Write the failing test**

Create `Cause.SecurityManagement.Tests/Authentication/PermissionRegistrationTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Authentication;

using System.Linq;
using AwesomeAssertions;
using Cause.SecurityManagement.Core.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class PermissionRegistrationTests
{
    [Test]
    public void AddPermissionBasedAuthorization_ShouldRegisterThePolicyProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddPermissionBasedAuthorization();

        var descriptor = services.Single(service => service.ServiceType == typeof(IAuthorizationPolicyProvider));
        descriptor.ImplementationType.Should().Be<PermissionAuthorizationPolicyProvider>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Test]
    public void AddPermissionBasedAuthorization_ShouldRegisterTheHandlerAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddPermissionBasedAuthorization();

        var descriptor = services.Single(service =>
            service.ServiceType == typeof(IAuthorizationHandler)
            && service.ImplementationType == typeof(PermissionAuthorizationHandler));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Test]
    public void AddPermissionBasedAuthorization_ShouldRegisterTheCacheAsScoped()
    {
        var services = new ServiceCollection();

        services.AddPermissionBasedAuthorization();

        var descriptor = services.Single(service => service.ServiceType == typeof(ScopedPermissionCache));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionRegistrationTests`
Expected: compile error — `AddPermissionBasedAuthorization` does not exist.

- [ ] **Step 3: Add the extension method**

Append to `ServiceCollectionAuthorizationExtensions` (file-scoped namespace, `Add*` naming convention, XML doc comments like its siblings). Add `using Microsoft.Extensions.DependencyInjection.Extensions;`:

```csharp
/// <summary>
/// Adds the permission gate used by [AdministratorOrUserWithPermission] and [UserWithPermission].
/// Opt-in; composes with the AddAuthorizationFor* extensions rather than replacing them.
/// </summary>
public static IServiceCollection AddPermissionBasedAuthorization(this IServiceCollection services)
{
    services.AddHttpContextAccessor();
    services.TryAddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());
    services.TryAddScoped<ScopedPermissionCache>();

    return services;
}
```

`TryAddSingleton` for the policy provider matters: it lets a consuming application substitute its own provider, and avoids a duplicate registration silently winning.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionRegistrationTests`
Expected: 3 passed.

- [ ] **Step 5: Build the whole solution**

Run: `dotnet build Cause.SecurityManagement.sln -c Debug --nologo -p:GeneratePackageOnBuild=false`
Expected: `0 Avertissement(s)`, `0 Erreur(s)`.

- [ ] **Step 6: Commit**

```bash
git add Cause.SecurityManagement.Core/Authentication/ServiceCollectionAuthorizationExtensions.cs Cause.SecurityManagement.Tests/Authentication/PermissionRegistrationTests.cs
git commit -m "#115 - Add AddPermissionBasedAuthorization registration extension"
```

---

### Task 7: Startup tag validation

**Files:**
- Create: `Cause.SecurityManagement.Core/Authentication/PermissionTagValidationHostedService.cs`
- Modify: `Cause.SecurityManagement.Core/Authentication/ServiceCollectionAuthorizationExtensions.cs`
- Test: `Cause.SecurityManagement.Tests/Authentication/PermissionTagValidationHostedServiceTests.cs`

**Interfaces:**
- Consumes: `PermissionPolicy.TryParse` (Task 4), `AddPermissionBasedAuthorization` (Task 6).
- Produces: `PermissionTagValidationHostedService : IHostedService`, constructed with `(IEnumerable<EndpointDataSource> endpointDataSources, IServiceScopeFactory scopeFactory, ILogger<PermissionTagValidationHostedService> logger)`.

**Why:** a typo'd tag matches no `ModulePermission` row, so every `RegularUser` is denied while Administrators still pass. Fails closed, but reads as a data problem.

**It must never throw.** A shared database missing a row must not take a 9-1-1 application down at boot, and the database may not be migrated yet at startup.

- [ ] **Step 1: Write the failing tests**

Create `Cause.SecurityManagement.Tests/Authentication/PermissionTagValidationHostedServiceTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Authentication;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services.Management;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class PermissionTagValidationHostedServiceTests
{
    private const string KnownTag = "CanEditBuilding";
    private const string UnknownTag = "CanEditBuildng";

    private IPermissionCatalogService catalogService;
    private FakeLogger<PermissionTagValidationHostedService> logger;

    [SetUp]
    public void SetUp()
    {
        catalogService = Substitute.For<IPermissionCatalogService>();
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PermissionDto> { new() { Tag = KnownTag } });
        logger = new FakeLogger<PermissionTagValidationHostedService>();
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }

    private sealed class StubEndpointDataSource(params string[] policies) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints { get; } = BuildEndpoints(policies);

        public override IChangeToken GetChangeToken() => throw new NotSupportedException();

        private static IReadOnlyList<Endpoint> BuildEndpoints(string[] policies)
        {
            var endpoints = new List<Endpoint>();
            foreach (var policy in policies)
            {
                var metadata = new EndpointMetadataCollection(new AuthorizeAttribute { Policy = policy });
                endpoints.Add(new Endpoint(_ => Task.CompletedTask, metadata, "test"));
            }
            return endpoints;
        }
    }

    private PermissionTagValidationHostedService CreateService(
        StubEndpointDataSource endpoints,
        bool registerCatalog = true)
    {
        var services = new ServiceCollection();
        if (registerCatalog)
            services.AddScoped(_ => catalogService);
        var provider = services.BuildServiceProvider();

        return new PermissionTagValidationHostedService(
            [endpoints],
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger);
    }

    [Test]
    public async Task AllTagsKnown_WhenStarting_ShouldNotWarn()
    {
        var service = CreateService(new StubEndpointDataSource(
            PermissionPolicy.NameFor(KnownTag, allowAdministrator: true)));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().BeEmpty();
    }

    [Test]
    public async Task UnknownTag_WhenStarting_ShouldWarnNamingTheTag()
    {
        var service = CreateService(new StubEndpointDataSource(
            PermissionPolicy.NameFor(UnknownTag, allowAdministrator: false)));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().HaveCount(1);
        logger.Warnings[0].Should().Contain(UnknownTag);
    }

    [Test]
    public async Task NonPermissionPolicies_WhenStarting_ShouldBeIgnored()
    {
        var service = CreateService(new StubEndpointDataSource("SomeOtherPolicy"));

        await service.StartAsync(CancellationToken.None);

        logger.Warnings.Should().BeEmpty();
    }

    [Test]
    public async Task CatalogThrows_WhenStarting_ShouldWarnAndNotThrow()
    {
        catalogService.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns<List<PermissionDto>>(_ => throw new InvalidOperationException("relation does not exist"));
        var service = CreateService(new StubEndpointDataSource(
            PermissionPolicy.NameFor(KnownTag, allowAdministrator: true)));

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        logger.Warnings.Should().ContainSingle(warning => warning.Contains("skipped"));
    }

    [Test]
    public async Task CatalogNotRegistered_WhenStarting_ShouldWarnAndNotThrow()
    {
        var service = CreateService(
            new StubEndpointDataSource(PermissionPolicy.NameFor(KnownTag, allowAdministrator: true)),
            registerCatalog: false);

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        logger.Warnings.Should().ContainSingle(warning => warning.Contains("skipped"));
    }
}
```

`Microsoft.Extensions.Primitives.IChangeToken` needs `using Microsoft.Extensions.Primitives;` — add it if the compiler asks.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionTagValidationHostedServiceTests`
Expected: compile error — `PermissionTagValidationHostedService` does not exist.

- [ ] **Step 3: Implement the hosted service**

`Cause.SecurityManagement.Core/Authentication/PermissionTagValidationHostedService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cause.SecurityManagement.Core.Services.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cause.SecurityManagement.Core.Authentication;

/// <summary>
/// Warns at startup when a permission attribute names a tag absent from the permission catalog.
/// Never fails startup: an unknown tag denies rather than grants, and the database may not be
/// migrated yet when this runs.
/// </summary>
public class PermissionTagValidationHostedService(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IServiceScopeFactory scopeFactory,
    ILogger<PermissionTagValidationHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var declaredTags = GetDeclaredTags();
        if (declaredTags.Count == 0)
            return;

        var knownTags = await GetKnownTagsAsync(cancellationToken);
        if (knownTags is null)
            return;

        foreach (var tag in declaredTags.Where(tag => !knownTags.Contains(tag)))
            logger.LogWarning("Permission tag '{PermissionTag}' is used by an endpoint but is missing from the permission catalog. Every RegularUser will be denied.", tag);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private HashSet<string> GetDeclaredTags()
    {
        var tags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dataSource in endpointDataSources)
        {
            foreach (var endpoint in dataSource.Endpoints)
            {
                foreach (var authorizeData in endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>())
                {
                    if (PermissionPolicy.TryParse(authorizeData.Policy, out var tag, out _))
                        tags.Add(tag);
                }
            }
        }
        return tags;
    }

    private async Task<HashSet<string>> GetKnownTagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var catalog = scope.ServiceProvider.GetService<IPermissionCatalogService>();
            if (catalog is null)
            {
                logger.LogWarning("Permission tag validation skipped: IPermissionCatalogService is not registered.");
                return null;
            }

            var permissions = await catalog.GetPermissionsAsync(cancellationToken);
            return [.. permissions.Select(permission => permission.Tag)];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Permission tag validation skipped: the permission catalog could not be read.");
            return null;
        }
    }
}
```

Catching bare `Exception` is deliberate and justified: this must never take an application down at boot.

- [ ] **Step 4: Add the flag to the registration extension**

Add the parameter now that there is a hosted service for it to register. Defaulting to `false` keeps the Task 6 call sites compiling unchanged:

```csharp
/// <param name="validateTagsAtStartup">
/// When true, logs a warning at startup for every attribute tag missing from the permission catalog.
/// Never fails startup.
/// </param>
public static IServiceCollection AddPermissionBasedAuthorization(
    this IServiceCollection services,
    bool validateTagsAtStartup = false)
{
    // ... existing registrations unchanged ...

    if (validateTagsAtStartup)
        services.AddHostedService<PermissionTagValidationHostedService>();

    return services;
}
```

Add `using Microsoft.Extensions.Hosting;` if the compiler asks.

- [ ] **Step 5: Add a registration test for the flag**

Append to `PermissionRegistrationTests`:

```csharp
[Test]
public void AddPermissionBasedAuthorizationWithoutValidation_ShouldNotRegisterTheHostedService()
{
    var services = new ServiceCollection();

    services.AddPermissionBasedAuthorization();

    services.Should().NotContain(service =>
        service.ImplementationType == typeof(PermissionTagValidationHostedService));
}

[Test]
public void AddPermissionBasedAuthorizationWithValidation_ShouldRegisterTheHostedService()
{
    var services = new ServiceCollection();

    services.AddPermissionBasedAuthorization(validateTagsAtStartup: true);

    services.Should().Contain(service =>
        service.ImplementationType == typeof(PermissionTagValidationHostedService));
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter "PermissionTagValidationHostedServiceTests|PermissionRegistrationTests"`
Expected: 10 passed.

- [ ] **Step 7: Commit**

```bash
git add Cause.SecurityManagement.Core/Authentication/ Cause.SecurityManagement.Tests/Authentication/
git commit -m "#115 - Warn at startup when a permission tag is missing from the catalog"
```

---

### Task 8: HTTP pipeline tests

**Files:**
- Create: `Cause.SecurityManagement.Tests/Authentication/PermissionGateEndpointTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 3–6.
- Produces: nothing consumed by later tasks.

**Why this task exists.** `UseDefaultAuthorizationWhenNotSpecifiedFilter` returns without evaluating whenever another authorization filter is present, and `AddAuthorizeFiltersControllerConvention` skips the default filter when the controller type carries any `AuthorizeAttribute`. Both permission attributes derive from `AuthorizeAttribute`, so **they suppress the application's fallback policy on the endpoints they decorate**. That hazard lives in filter composition, not in any single class, so unit tests cannot catch it.

These live in `Cause.SecurityManagement.Tests` because it references `Microsoft.AspNetCore.TestHost` and **both** Core and `Cause.SecurityManagement`. `Cause.SecurityManagement.Integration.Tests` has neither an HTTP pipeline nor a reference to the HTTP package.

- [ ] **Step 1: Read the existing TestServer pattern**

Read `Cause.SecurityManagement.Tests/Authentication/KeycloakJwtBearerIntegrationTests.cs`, particularly `CreateApiHostAsync`. Reuse its `HostBuilder().ConfigureWebHost(webBuilder => webBuilder.UseTestServer())` shape.

- [ ] **Step 2: Write the failing tests**

Create `Cause.SecurityManagement.Tests/Authentication/PermissionGateEndpointTests.cs`:

```csharp
namespace Cause.SecurityManagement.Tests.Authentication;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Cause.SecurityManagement.Core;
using Cause.SecurityManagement.Core.Authentication;
using Cause.SecurityManagement.Core.Services;
using Cause.SecurityManagement.Models.DataTransferObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class PermissionGateEndpointTests
{
    private const string GrantedTag = "CanEditBuilding";
    private const string TestScheme = "TestScheme";

    private IHost host;
    private TestServer server;
    private IUserPermissionService permissionService;
    private Guid someUserId;

    [SetUp]
    public async Task SetUpAsync()
    {
        someUserId = Guid.NewGuid();
        permissionService = Substitute.For<IUserPermissionService>();
        permissionService.GetPermissionsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserMergedPermission>
            {
                new() { FeatureName = GrantedTag, Access = true },
            });

        host = await CreateHostAsync();
        server = host.GetTestServer();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (host is not null)
            await host.StopAsync();
        host?.Dispose();
    }

    private async Task<IHost> CreateHostAsync()
    {
        var builder = new HostBuilder().ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseTestServer();
            webBuilder.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthentication(TestScheme)
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(TestScheme, _ => { });
                services.AddAuthorizationForRegularUser();
                services.AddPermissionBasedAuthorization();
                services.AddSingleton(permissionService);
            });
            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/granted", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor(GrantedTag, allowAdministrator: true));
                    endpoints.MapGet("/other", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor("CanDoSomethingElse", allowAdministrator: true));
                    endpoints.MapGet("/strict", Ok)
                        .RequireAuthorization(PermissionPolicy.NameFor(GrantedTag, allowAdministrator: false));
                });
            });
        });
        return await builder.StartAsync();
    }

    private static Task Ok(HttpContext context) => context.Response.WriteAsync("ok");

    private sealed class HeaderAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim> { new(ClaimTypes.Role, role.ToString()) };
            if (Request.Headers.TryGetValue("X-Test-Sid", out var sid))
                claims.Add(new Claim(JwtRegisteredClaimNames.Sid, sid.ToString()));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, TestScheme)));
        }
    }

    private async Task<HttpStatusCode> GetAsync(string path, string role = null, string sid = null)
    {
        using var client = server.CreateClient();
        if (role is not null)
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (sid is not null)
            client.DefaultRequestHeaders.Add("X-Test-Sid", sid);

        var response = await client.GetAsync(path);
        return response.StatusCode;
    }

    [Test]
    public async Task UnauthenticatedRequest_WhenCallingGatedEndpoint_ShouldReturnUnauthorized()
    {
        var status = await GetAsync("/granted");

        status.Should().Be(HttpStatusCode.Unauthorized,
            "the dynamic policy must require an authenticated user, because the fallback policy does not run here");
    }

    [Test]
    public async Task RegularUserHoldingThePermission_WhenCallingGatedEndpoint_ShouldReturnOk()
    {
        var status = await GetAsync("/granted", SecurityRoles.User, someUserId.ToString());

        status.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task RegularUserWithoutThePermission_WhenCallingGatedEndpoint_ShouldReturnForbidden()
    {
        var status = await GetAsync("/other", SecurityRoles.User, someUserId.ToString());

        status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Administrator_WhenCallingAdministratorOrUserEndpoint_ShouldReturnOk()
    {
        var status = await GetAsync("/granted", SecurityRoles.Administrator, someUserId.ToString());

        status.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Administrator_WhenCallingUserOnlyEndpoint_ShouldReturnForbidden()
    {
        var status = await GetAsync("/strict", SecurityRoles.Administrator, someUserId.ToString());

        status.Should().Be(HttpStatusCode.Forbidden,
            "UserWithPermission excludes Administrators, and Keycloak principals hold that role");
    }

    [Test]
    public async Task ExternalSystem_WhenCallingGatedEndpoint_ShouldReturnForbidden()
    {
        var status = await GetAsync("/granted", SecurityRoles.ExternalSystem, someUserId.ToString());

        status.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo --filter PermissionGateEndpointTests`
Expected: 6 passed.

If `UnauthenticatedRequest...ShouldReturnUnauthorized` fails with 403 instead of 401, that is a real finding, not a test bug: it means the policy is not challenging correctly. Fix the provider, not the assertion.

- [ ] **Step 4: Verify the scheme-list hypothesis**

The spec records as an **unverified hypothesis** that a dynamic policy omitting the fallback policy's authentication scheme list risks a 401 under `AddAuthorizationForRegularUserKeycloakAndApiCertificate`.

Test it: temporarily comment out the `AddAuthenticationSchemes` block in `PermissionAuthorizationPolicyProvider.BuildPolicy`, add a second fixture host registering **two** authentication schemes with the non-default one naming the principal, and see whether a request authenticated under the non-default scheme still passes.

- If removing the scheme copying breaks a test, keep it and record the confirmation in the spec.
- If it changes nothing, **delete the scheme-copying code and its test**, and update the spec's "Why The Scheme List Is Copied" section to record that it proved unnecessary. Do not keep unexercised code.

Report which outcome you observed, with the test output.

- [ ] **Step 5: Commit**

```bash
git add Cause.SecurityManagement.Tests/Authentication/PermissionGateEndpointTests.cs docs/specs/2026-08-11-permission-based-gate.md
git commit -m "#115 - Add HTTP pipeline tests for the permission gate"
```

---

### Task 9: Documentation and final verification

**Files:**
- Modify: `README.md`
- Modify: `docs/adr/records/2026-08-11-permission-based-authorization-gate.md`
- Modify: `docs/adr/records/overview.md`

- [ ] **Step 1: Read the README to find the right section and match its style**

Run: `grep -n "^#" README.md` and read the authorization-related sections.

- [ ] **Step 2: Document the gate in the README**

Add a section covering all four points. The warning is not optional — it is the one thing a reader can get badly wrong:

````markdown
## Permission-Based Endpoint Authorization

Register the gate alongside your existing authorization setup:

```csharp
services.AddAuthorizationForRegularUser();
services.AddPermissionBasedAuthorization();
```

Then gate endpoints by permission tag:

```csharp
[AdministratorOrUserWithPermission(Permission.CanEditBuilding)]
public async Task<IActionResult> EditBuilding(...) { }
```

| Attribute | Administrator | RegularUser | Everyone else |
|---|---|---|---|
| `[AdministratorOrUserWithPermission(tag)]` | passes | passes only with the permission | 403 |
| `[UserWithPermission(tag)]` | 403 | passes only with the permission | 403 |

### ⚠️ Choosing between them

`Administrator` is granted to **every Keycloak-authenticated principal** and never
by this library's own login path, so the two roles are mutually exclusive.

- **Using Keycloak?** Use `[AdministratorOrUserWithPermission]`.
- **Not using Keycloak?** You have no Administrator principals, so
  `[UserWithPermission]` is equivalent and reads more honestly.

**`[UserWithPermission]` denies every Keycloak-authenticated principal.** Do not
reach for the shorter name assuming it is the more general one.

### Referencing tags safely

Declare your tags as constants so typos become compile errors:

```csharp
public static class Permission
{
    public const string CanEditBuilding = "CanEditBuilding";
}
```

Optionally warn at startup about tags missing from the permission catalog:

```csharp
services.AddPermissionBasedAuthorization(validateTagsAtStartup: true);
```

This logs a warning per unknown tag and never fails startup.

### Performance

A permission check costs one database read per request per user; the merged set is
memoized for the request. Administrators are never looked up. There is no
cross-request cache, so revoking a permission takes effect on the next request.
````

- [ ] **Step 3: Flip the ADR to accepted**

In `docs/adr/records/2026-08-11-permission-based-authorization-gate.md`, change `* Status: proposed` to `* Status: accepted`, tick every `Implementation Plan` checkbox that is done, and update Task 7's entry to record the scheme-list outcome from Task 8 Step 4.

In `docs/adr/records/overview.md`, change this ADR's Status column from `proposed` to `accepted`.

- [ ] **Step 4: Full verification**

Run each and paste the actual output including exit codes:

```bash
dotnet build Cause.SecurityManagement.sln -c Debug --nologo -p:GeneratePackageOnBuild=false
```

```bash
dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo
```

Expected: `0 Avertissement(s)`, `0 Erreur(s)`; all tests pass. **Report the pass count.**

The integration test project needs Docker for Testcontainers PostgreSQL. Run it if Docker is available; if not, say so explicitly rather than claiming it passed:

```bash
dotnet test Cause.SecurityManagement.Integration.Tests/Cause.SecurityManagement.Integration.Tests.csproj --nologo
```

- [ ] **Step 5: Commit**

```bash
git add README.md docs/
git commit -m "#115 - Document the permission gate and accept the ADR"
```

---

## Deferred, Not Forgotten

Recorded in the spec's out-of-scope section; do **not** do these in this branch:

- Cross-request or distributed permission caching.
- Any change to `PermissionMergeTool` merge semantics.
- The unresolved maintainer comment at `MultiJwtClaimsTransformer.cs:32` about whether Keycloak principals should receive `Administrator`, and the `"Administrator"` string literal on line 34 that should be `SecurityRoles.Administrator`. This design depends on that behavior, so changing it later requires revisiting this spec.
- A NuGet version bump and release. Per the multi-package release governance ADR, releases are a separate deliberate step.
