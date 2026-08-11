# Permission-Based Authorization Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `[AdministratorOrUserWithPermission(tag)]` and `[UserWithPermission(tag)]` endpoint attributes that gate `RegularUser` principals on a named permission, leaving every other principal type behaving as it does today.

**Architecture:** A dynamic `IAuthorizationPolicyProvider` turns the policy name `Permission:<mode>:<tag>` into a policy on demand, so consuming applications never enumerate their tags in startup code. A single `AuthorizationHandler<PermissionRequirement>` applies the role rules, differing between the two attributes only by an `AllowAdministrator` flag. The handler is a singleton that reaches the request-scoped `IUserPermissionService` through `HttpContext.RequestServices`, memoizing the merged permission set per request.

**Tech Stack:** .NET 10 (`net10.0`), ASP.NET Core authorization, EF Core, NUnit 4.6.1, NSubstitute 6.2.0, AwesomeAssertions 9.5.0, Microsoft.AspNetCore.TestHost 10.0.10.

**Spec:** [docs/specs/2026-08-11-permission-based-gate.md](../../specs/2026-08-11-permission-based-gate.md)
**ADR:** [docs/adr/records/2026-08-11-permission-based-authorization-gate.md](../../adr/records/2026-08-11-permission-based-authorization-gate.md)

## Global Constraints

- **Zero warnings.** The current baseline is `0 Avertissement(s)`. Any new warning is a task failure. Verify with `--no-incremental` in Release — a plain incremental build skips up-to-date projects and will not re-emit their warnings, so "0 warnings" from an incremental run is not evidence.
- **XML doc `<param>` tags: all or none.** `GenerateDocumentationFile=True` on the packable projects, and CS1591 is `severity = none` in `.editorconfig` so missing docs are fine. But **CS1573 is not suppressed** and fires when *some* parameters are documented and others are not. Adding a single `<param>` tag to a method without tagging every parameter breaks the zero-warning gate.
- **Any test asserting on a `Task` must `await` the assertion.** `ThrowAsync`, `NotThrowAsync`, and NSubstitute's `Received()` on an async member all return awaitables. A `[Test]` containing one must be `async Task`, never `void` — CS4014 does not fire in a `void` method, so an un-awaited assertion passes vacuously.
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
| `Core/Authentication/ScopedPermissionCache.cs` | Per-request memoization of the merged set — **`internal`** |
| `Core/UserMergedPermissionExtensions.cs` | The shared `Allows(tag)` predicate — **`internal`** |
| `Core/Authentication/PermissionAuthorizationHandler.cs` | The role rules |
| `Core/Authentication/PermissionTagValidationHostedService.cs` | Opt-in startup tag validation |
| `Core/Authentication/ServiceCollectionAuthorizationExtensions.cs` | Registration (modify) |
| `Core/Services/IUserPermissionService.cs` + impl | Async permission path (modify) |
| `Core/Repositories/I*PermissionRepository.cs` + impls | Async reads (modify) |

Task order is dependency order: the async data path (Tasks 1–2) comes before the cache that consumes it (Task 3), which comes before the handler (Task 5).

**Default to `internal` for anything consumers do not need to name.** These are published NuGet packages, so every public type is a permanent commitment under `docs/RELEASING.md` — `internal` → `public` later is a MINOR change, but `public` → `internal` is MAJOR. Core already has `InternalsVisibleTo` for both test assemblies, so `internal` costs nothing in testability. Only the two attributes, the requirement, the policy provider, and the registration extension need to be public.

---

### Task 1: Async repository reads — ✅ COMPLETE

**Shipped differently from the original plan.** Two decisions changed during execution; this section records what actually exists, because Tasks 2 and 3 depend on it.

**Files changed:**
- `Cause.SecurityManagement.Core/Repositories/IUserPermissionRepository.cs`
- `Cause.SecurityManagement.Core/Repositories/UserPermissionRepository.cs`
- `Cause.SecurityManagement.Core/Repositories/IGroupPermissionRepository.cs`
- `Cause.SecurityManagement.Core/Repositories/GroupPermissionRepository.cs`
- `Cause.SecurityManagement.Integration.Tests/Repositories/GroupPermissionRepositoryTests.cs` (new)

**Actual signature — both interfaces:**

```csharp
Task<List<UserMergedPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
```

#### Change 1 — default interface implementations, not abstract members

`docs/RELEASING.md:41` makes a breaking public-API change a MAJOR bump. These are public interfaces in a published NuGet package, so an abstract member would break consumers with hand-written implementations and force 11.0.0 instead of 10.7.0. Both members ship as default implementations delegating to the synchronous `GetForUser`; the library's own repository classes override them with genuinely async versions.

The default bodies reference no EF Core. `Include` and `ToListAsync` are EF extensions that fail on a non-EF query provider, and an interface default body must not assume EF.

#### Change 2 — projection, not entities

Originally planned to return `List<UserPermission>` / `List<GroupPermission>`. Code review established that returning entities was wrong on three counts, and the maintainer approved the change:

1. EF materialized ~10 columns across two tables per row, and `UserPermissionService` then discarded all but two fields in memory — a payload regression on a hot authorization path.
2. Returning entities required an `Include` to populate `Permission`, which an interface default body cannot do, leaving a documented "implementors must populate this" trap that NREs inside an authorization path.
3. A public interface return type cannot change after 10.7.0 publishes without a MAJOR bump, so the window to fix it was now.

Projecting inside the query solved all three: reading `Permission.Tag` inside a `Select` makes EF emit the join, so no `Include` is needed and there is no navigation left to forget. There was precedent — `IUserPermissionRepository.GetUserPermissionsAsync` already returns `List<AuthenticationUserPermission>`.

**Concrete implementations:**

```csharp
// UserPermissionRepository
public Task<List<UserMergedPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
{
    return context.UserPermissions.AsNoTracking()
        .Where(userPermission => userPermission.IdUser == userId)
        .Select(userPermission => new UserMergedPermission { Access = userPermission.IsAllowed, FeatureName = userPermission.Permission.Tag })
        .ToListAsync(cancellationToken);
}

// GroupPermissionRepository
public Task<List<UserMergedPermission>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
{
    return context.GroupPermissions.AsNoTracking()
        .Where(groupPermission => context.UserGroups
            .Any(userGroup => userGroup.IdUser == userId && userGroup.IdGroup == groupPermission.IdGroup))
        .Select(groupPermission => new UserMergedPermission { Access = groupPermission.IsAllowed, FeatureName = groupPermission.Permission.Tag })
        .ToListAsync(cancellationToken);
}
```

The group query deliberately does not reuse `GetForUser`, whose query-expression `SelectMany` shape makes chained operators fragile. Code review verified the `Where` + `context.UserGroups.Any(...)` form translates to `WHERE EXISTS (...)` fully server-side, and that it returns the same set as `GetForUser` — with one benign improvement: `UserGroupMapping` has no unique index on `(IdUser, IdGroup)`, so `GetForUser` can emit duplicate rows where this form cannot. `PermissionMergeTool` groups by `FeatureName`, so duplicates were already idempotent.

#### Verified findings that affect later tasks

**NSubstitute *does* intercept default interface members** — it does not fall through to them. A substitute returns a non-null `Task` whose `Result` is `null`. So the hand-written-fake fallback described in Task 3 is unnecessary, but **every fixture must stub the async members explicitly**; stubbing only the synchronous member produces an NRE inside the service rather than a useful assertion failure.

**Verification (Release, `--no-incremental`):** `0 Avertissement(s)`, `0 Erreur(s)`. Unit tests 219/219. Integration tests 65/65, including the new `GroupPermissionRepositoryTests` which seeds a user in two groups and asserts both the tag and the `IsAllowed` flag round-trip, covering the `false` case.

### Task 2: Async permission service path — ✅ COMPLETE

Shipped as planned, with two additions from code review: three tests proving the interface default bodies delegate to the synchronous members (the fixture's `SynchronousOnlyPermissionService` stub compiling *is* the MINOR-compatibility proof), and a mirrored deny-wins case. The methods sit beside their synchronous counterparts so a maintainer editing one sees the other — they execute different SQL.

`GetPermissionsForUserAsync` awaits the two repositories **sequentially, deliberately**. Both resolve the same scoped `DbContext` through `IScopedDbContextProvider`, so `Task.WhenAll` would throw on a second concurrent operation — intermittently, inside an authorization handler. Do not "optimize" this.

The original detail follows.

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

**Updated after Task 1.** The repositories now return `List<UserMergedPermission>` rather than entities, so the service no longer projects — it merges what the repositories hand it. The test doubles must return `UserMergedPermission` too. NSubstitute *does* intercept default interface members (verified in Task 1), so `.Returns(...)` works, but **both async members must be stubbed explicitly** — stubbing only the synchronous one yields a `Task` whose `Result` is `null` and an NRE rather than a useful assertion failure.

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
    using Cause.SecurityManagement.Models.DataTransferObjects;
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
                .Returns(new List<UserMergedPermission>());
            groupPermissionRepository.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<UserMergedPermission>());

            service = new UserPermissionService(groupPermissionRepository, userPermissionRepository);
        }

        private static UserMergedPermission PermissionFor(string tag, bool isAllowed)
            => new() { Access = isAllowed, FeatureName = tag };

        [Test]
        public async Task UserWithAllowedPermission_WhenHasPermissionAsync_ShouldReturnTrue()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: true)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Test]
        public async Task UserWithDeniedPermission_WhenHasPermissionAsync_ShouldReturnFalse()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: false)]);

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
                .Returns([PermissionFor(SomeTag, isAllowed: true)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor(SomeTag, isAllowed: false)]);

            var result = await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            result.Should().BeFalse("PermissionMergeTool computes Access with All(), so a deny wins");
        }

        [Test]
        public async Task UserAndGroupPermissions_WhenGetPermissionsForUserAsync_ShouldMergeBoth()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor("FromUser", isAllowed: true)]);
            groupPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns([PermissionFor("FromGroup", isAllowed: true)]);

            var result = await service.GetPermissionsForUserAsync(someUserId, CancellationToken.None);

            result.Select(permission => permission.FeatureName)
                .Should().BeEquivalentTo(["FromUser", "FromGroup"]);
        }

        [Test]
        public async Task BothRepositories_WhenGetPermissionsForUserAsync_ShouldReceiveTheCancellationToken()
        {
            using var cancellation = new CancellationTokenSource();

            await service.GetPermissionsForUserAsync(someUserId, cancellation.Token);

            await userPermissionRepository.Received(1).GetForUserAsync(someUserId, cancellation.Token);
            await groupPermissionRepository.Received(1).GetForUserAsync(someUserId, cancellation.Token);
        }

        [Test]
        public async Task RepositoryThrowsOperationCanceled_WhenHasPermissionAsync_ShouldPropagate()
        {
            userPermissionRepository.GetForUserAsync(someUserId, Arg.Any<CancellationToken>())
                .Returns<List<UserMergedPermission>>(_ => throw new OperationCanceledException());

            var act = async () => await service.HasPermissionAsync(someUserId, SomeTag, CancellationToken.None);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
```

> **This corrects a defect in an earlier draft of this plan.** The cancellation test
> was originally written as `public void` with a bare, un-awaited
> `act.Should().ThrowAsync<...>();`. That compiles silently — CS4014 only fires
> inside an `async` method — and passes no matter what the code does. It also
> pre-cancelled a token the mocked repositories ignore, so the cancellation was
> decorative.
>
> **Any test asserting on a `Task` must `await` the assertion.** `ThrowAsync`,
> `NotThrowAsync`, and `Received()` on an async member all return awaitables. A
> `[Test]` method containing one must be `async Task`, never `void`.

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

    return new PermissionMergeTool().MergeUserAndGroupPermissions(groupPermissions, userPermissions);
}
```

Add `using System.Threading;` and `using System.Threading.Tasks;`. `PermissionMergeTool` lives in `Cause.SecurityManagement.Core`, already imported transitively — add `using Cause.SecurityManagement.Core;` if the compiler asks.

Note how much simpler this is than the synchronous `GetPermissionsForUser` above it: because Task 1's repositories project to `UserMergedPermission` server-side, the service has nothing left to map. Do **not** add a projection helper here — there is nothing to project.

Argument order matters: `MergeUserAndGroupPermissions(groupPermissions, userPermissions)` — group first, matching the existing synchronous call. The merge itself is order-independent (`Access = group.All(...)`), but keep the call sites consistent.

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

### Task 3: Per-request permission cache — ✅ COMPLETE

**Shipped with two changes from the original plan.** This section records what exists, because Tasks 5 and 6 depend on it.

**Files:**
- `Cause.SecurityManagement.Core/Authentication/ScopedPermissionCache.cs` (new)
- `Cause.SecurityManagement.Core/UserMergedPermissionExtensions.cs` (new)
- `Cause.SecurityManagement.Core/Services/UserPermissionService.cs` (modified — two call sites)
- `Cause.SecurityManagement.Tests/Authentication/ScopedPermissionCacheTests.cs` (new, 7 tests)

**Actual API:**

```csharp
internal class ScopedPermissionCache(IUserPermissionService permissionService)
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionTag, CancellationToken cancellationToken)
}
```

#### Change 1 — the type is `internal`, not `public`

Code review flagged this as blocking, and it is irreversible once published: this release is MINOR (10.7.0) per `docs/RELEASING.md`, so narrowing `public` → `internal` afterward would require a MAJOR bump.

The cache uses a plain non-thread-safe `Dictionary`. That is **correct** for the designed path — the reviewer verified against the ASP.NET Core 10 source that `DefaultAuthorizationService.AuthorizeAsync` and `AuthorizationHandler<T>.HandleAsync` both invoke in a sequential awaited `foreach`, with no `Task.WhenAll` at either level. So multiple requirements on one endpoint are evaluated strictly sequentially against one instance.

But `public` would let a consumer write `Task.WhenAll(userIds.Select(id => cache.HasPermissionAsync(...)))`. With no synchronization context those continuations land on different thread-pool threads, and two concurrent misses write the dictionary unsynchronized — which during a resize can corrupt the bucket chain and spin forever inside `TryGetValue`, hanging a request thread. That is a denial-of-service surface in an authorization path, not a cosmetic race.

`internal` makes the sequential-access guarantee structural rather than assumed, and costs nothing: `Cause.SecurityManagement.Core/Properties/AssemblyInfo.cs` already declares `InternalsVisibleTo` for both `Cause.SecurityManagement.Tests` and `Cause.SecurityManagement.Integration.Tests`, internal types resolve through DI normally, and every consumer (the handler in Task 5, the registration in Task 6) lives inside the Core assembly.

**Do not add `ConcurrentDictionary`.** The plain `Dictionary` is correct once access is confined to the assembly, and a concurrent collection would imply a guarantee this type does not have.

**Known consumer caveat, worth remembering:** in a Blazor Server host a "scoped" lifetime is the *circuit* lifetime — hours, concurrently accessed — not a request. `internal` contains that too.

#### Change 2 — `PermissionSetExtensions` renamed to `UserMergedPermissionExtensions`

The predicate `FeatureName == tag && Access` had reached three occurrences (`HasPermission`, `HasPermissionAsync`, the cache), triggering the Rule of Three. It was extracted to an `internal` extension — internal so it adds no public API surface to a published package.

The original name pointed at a `PermissionSet` type that does not exist. Renamed to match the type it actually extends. Block namespace and tab indentation, matching its neighbour `PermissionMergeTool.cs`.

```csharp
internal static class UserMergedPermissionExtensions
{
    public static bool Allows(this List<UserMergedPermission> permissions, string permissionTag)
        => permissions.Exists(permission => permission.FeatureName == permissionTag && permission.Access);
}
```

All three call sites use it. The 12 `UserPermissionServiceTests` passed unmodified, and the reviewer confirmed by reading that the extraction is behavior-preserving including the null-receiver case.

#### Design details that must be preserved

**It caches `List<T>`, not `Task<List<T>>`.** This is the non-obvious part. The usual implementation of this pattern memoizes the `Task` to deduplicate in-flight loads, and then permanently caches a **faulted** task when the first load fails — turning one transient database blip into a request-wide authorization failure. Storing the materialized list *after* a successful await avoids that: the dictionary write sits after the `await`, so a throw skips it and the next call retries cleanly. `LoadFailsThenSucceeds_WhenHasPermissionAsync_ShouldNotCacheTheFailure` pins this.

**Cancellation is uniform.** `GetPermissionsAsync` calls `cancellationToken.ThrowIfCancellationRequested()` first, so a cache hit and a cache miss behave the same under a cancelled token. Without it, a hit returned a value while a miss threw.

**The cached list never escapes.** `GetPermissionsAsync` is private and the only public member returns `bool`, so no caller can mutate the cache. If a later task adds a public list-returning member to this type, it must return `IReadOnlyList<UserMergedPermission>`.

**Indentation:** `ScopedPermissionCache.cs` uses 4 spaces, matching the other five files in `Core/Authentication/`. `UserMergedPermissionExtensions.cs` uses tabs, matching `PermissionMergeTool.cs`. `Core/` root is genuinely split 4-4 between tabs and spaces and `.editorconfig` sets no `indent_style`, so match the neighbouring file rather than normalizing.

#### Tests — 7

Cache hit (one load for two different tags), per-user isolation, per-scope isolation via a real `ServiceCollection` asserting `Received(2)` — caching across scopes would be a bug, since a permission revoked between requests must be visible to the next — allowed tag, absent tag, present-but-denied tag, and failed-load-then-retry.

The `SetUp` stub returns a **fresh list per call** via `Returns(_ => Task.FromResult(...))`. A single `Returns(new List<...>)` is evaluated once, handing every caller the same instance, which would make the tests structurally unable to detect cross-user aliasing.

#### Carried forward to Task 6

Registration **must** be `AddScoped`. `IUserPermissionService` is scoped, so a singleton would both create a captive dependency and silently defeat the design — the cache would live for the application's lifetime and a revoked permission would never become visible. The Task 6 registration test asserts `ServiceLifetime.Scoped` for exactly this reason.

The Task 6 registration test resolves `ScopedPermissionCache` by type from `Cause.SecurityManagement.Tests`, which works because of the existing `InternalsVisibleTo`.

**Verification (Release, `--no-incremental`):** `0 Avertissement(s)`, `0 Erreur(s)`. Unit 238/238. Integration 65/65.

### Task 4: Requirement, attributes, and policy provider — ✅ COMPLETE

**Shipped with one significant design change and several hardening fixes.** Read this before Tasks 5, 6, and 8 — all three depend on it.

**Files:**
- `Cause.SecurityManagement.Core/Authentication/PermissionRequirement.cs` (public)
- `Cause.SecurityManagement.Core/Authentication/PermissionPolicy.cs` (public; `TryParse` internal)
- `Cause.SecurityManagement.Core/PermissionAttributes.cs` (both attributes, public)
- `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationPolicyProvider.cs` (public)
- `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationPolicyProviderTests.cs` (22 tests)

Policy names: `Permission:AdministratorOrUser:<tag>` and `Permission:User:<tag>`.

#### Change 1 — the dynamic policy inherits the fallback policy in full

Originally it copied only `AuthorizationOptions.FallbackPolicy.AuthenticationSchemes` and discarded the requirements. Security review found that **widened access**, and the maintainer approved the change.

`AuthorizationPolicy.CombineAsync` consults the fallback policy only when an endpoint carries no authorize data, so decorating an endpoint replaces the application's baseline gate. Under `AddAuthorizationForKeycloakAndRegularUserSchemes`, whose fallback requires role `Administrator` **only**:

| Principal | Undecorated | Decorated, schemes-only composition |
|---|---|---|
| Keycloak `Administrator` | allowed | allowed |
| `RegularUser` holding the tag | **denied** | **allowed** ← escalation |

A `RegularUser` token from this library's own anonymous login endpoints would reach an endpoint reserved for Keycloak principals. Discarding requirements was also unbounded for consumers — tenant scoping, MFA-completed checks, IP allowlists all vanished silently.

**The rule now: a permission attribute may only make an endpoint stricter, never looser.**

```csharp
private AuthorizationPolicy BuildPolicy(string tag, bool allowAdministrator)
{
    var builder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();

    var fallbackPolicy = authorizationOptions.FallbackPolicy;
    if (fallbackPolicy is not null)
        builder.Combine(fallbackPolicy);

    builder.AddRequirements(new PermissionRequirement(tag, allowAdministrator));

    return builder.Build();
}
```

`AuthorizationPolicyBuilder.Combine(AuthorizationPolicy)` — confirmed present in the net10.0 ref assembly — copies both `Requirements` and `AuthenticationSchemes`, so there is no separate scheme-copying step.

**This also closes the spec's unverified scheme-list hypothesis.** `Combine` brings the schemes along, and the framework confirms the underlying concern was real: `PolicyEvaluator.AuthenticateAsync` is documented as a no-op when a policy names no schemes, so it would trust only whatever the default scheme placed in `HttpContext.User`. Task 8 no longer needs to decide whether to delete the scheme copying — there is none to delete.

**Documented limitation:** the scheme list must live on `FallbackPolicy`. Schemes on `DefaultPolicy` are not inherited, because `CombineAsync` does not consult `DefaultPolicy` once a named policy resolves.

#### Change 2 — `AllowsCachingPolicies => true`

The interface default is `false`, and `DefaultAuthorizationPolicyProvider` overrides it to `true` only for its own exact type. Because registering this provider **replaces** the default globally, leaving the default would disable `AuthorizationPolicyCache` for **every endpoint in the consuming application** — not just permission-gated ones — re-running policy resolution and `CombineAsync` on every request app-wide.

Safe because the built policy is a pure function of the policy name plus `AuthorizationOptions`, which is a singleton snapshot fixed after startup. A test pins it so it cannot regress to the interface default.

#### Change 3 — the default provider is consulted first

`GetPolicyAsync` now tries `fallbackProvider.GetPolicyAsync(policyName)` before parsing. A consumer who explicitly registered a policy literally named `"Permission:..."` keeps it, and a whole class of misparse-to-500 failures disappears.

It also guards `ArgumentNullException.ThrowIfNull(policyName)`, matching `DefaultAuthorizationPolicyProvider`, which throws `ArgumentNullException` rather than the `NullReferenceException` the earlier version produced. Null is not reachable from the framework — `CombineAsync` skips whitespace-only names and `DefaultAuthorizationService` guards first — but this class is a public drop-in replacement.

#### Change 4 — ordinal comparison and tag validation

`TryParse` uses `StringComparison.Ordinal` for the prefix check. The previous culture-sensitive `StartsWith` was not exploitable (policy names come from compile-time constants, never requests, and every crafted variant failed closed at the ordinal mode comparison) but it was unsound: a linguistic match does not guarantee the prefix occupies exactly `Prefix.Length` characters, yet the next line slices at that ordinal index. It also resolved differently under request localization or `InvariantGlobalization=true`.

**Leave the mode comparison as `!=` on strings** — that is ordinal, and it is what keeps the parse fail-closed. Do not "consistently" make it linguistic.

`PermissionPolicy.NameFor` now calls `ArgumentException.ThrowIfNullOrWhiteSpace(tag)`. `[UserWithPermission("")]` previously produced `"Permission:User:"`, which failed to parse and 500'd on **every request** with the tag in the log; a whitespace tag parsed into a requirement that could never match, silently denying everyone. Both failed closed but were diagnosed in production.

#### Change 5 — `PermissionPolicy` moved to its own file

It now lives in `Core/Authentication/PermissionPolicy.cs`, beside the analogous `SecurityPolicy.cs`. The `AuthorizeByRolesAttribute.cs` precedent justifies grouping several *attributes* in one file, not a policy-name helper — and `PermissionAttributes.cs` was a misleading place to look for `NameFor`, the member consumers need for minimal-API gating. Free pre-publication; a MAJOR break afterward.

#### Verified framework behavior worth not re-deriving

- **A `null` policy from a provider is fail-closed.** `AuthorizationPolicy.CombineAsync`, `AuthorizationMiddleware.Invoke`, `AuthorizeFilter.ComputePolicyAsync`, and `DefaultAuthorizationService.GetPolicyAsync` all throw `InvalidOperationException`. None treats `null` as unprotected. So returning `null` for an unrecognized mode yields 500, not open access.
- **The tag never reaches SQL.** `GetPermissionsForUserAsync` fetches by `userId` and projects `Permission.Tag`; the comparison happens in memory in `UserMergedPermissionExtensions.Allows` using `==` (ordinal). No injection, no collation dependency. **Do not** "optimize" Task 5 into a server-side `Where(p => p.Tag == tag)` without pinning the collation.
- **Missing `UseAuthorization()` is fail-closed** — `EndpointMiddleware` throws when an endpoint has authorization metadata and the middleware never ran.

#### Corrected: the MVC filter is unioned, not suppressed

An earlier revision of the spec, ADR, and this plan claimed these attributes suppress `UseDefaultAuthorizationWhenNotSpecifiedFilter`. **Wrong on the MVC filter path.** `AuthorizationApplicationModelProvider.OnProvidersExecuting` returns early when `MvcOptions.EnableEndpointRouting` is true (the default), so authorization attributes never become filters and the guard never trips. The filter runs, and `AuthorizeFilter.GetEffectivePolicyAsync` **unions** its policy with the endpoint's metadata — a union of requirements being an AND.

So on applications using `AskForAuthorizationByDefault` or `AddAuthorizeFiltersControllerConvention`, the legacy `"defaultpolicy"` (`RequireRole(SecurityRoles.User)`) is ANDed with the permission policy, and **every Keycloak Administrator is denied** on decorated endpoints. Fails closed, but presents as a broken gate — and the tempting remedy, loosening the legacy policy, is the dangerous one. Task 8 must cover both filter-based registrations.

The fallback-*policy* half of the original claim is correct, and is not new: the existing `AuthorizeByRolesAttribute` family already suppresses `FallbackPolicy` the same way.

#### Tests — 22

The 12 originals plus: `AllowsCachingPolicies` is true; `GetDefaultPolicyAsync` and `GetFallbackPolicyAsync` each return the configured policy (previously untested — transposing those two one-line bodies would have passed everything); null policy name throws `ArgumentNullException`; a fallback role requirement is inherited; a fallback custom requirement is inherited; an explicitly registered `Permission:`-prefixed policy wins; and blank tags throw.

#### Carried into later tasks

**Task 5** — see the non-negotiable requirements listed in that task. The highest-risk one: succeed only the requirement instance passed in.

**Task 6** — registration must use `Replace`, not `TryAddSingleton`.

**Task 8** — cover `AskForAuthorizationByDefault` and `AddAuthorizeFiltersControllerConvention` hosts, and the `[AllowAnonymous]` bypass.

**Out of scope, worth its own issue:** `AuthorizeByPoliciesAttribute` in `AuthorizeByRolesAttribute.cs` joins policy names with a comma into a single `Policy` value. The framework treats `Policy` as one name, so any use with more than one policy is a guaranteed 500. Developers may reach for it with the new policy names.

**Verification (Release, `--no-incremental`):** `0 Avertissement(s)`, `0 Erreur(s)`. Unit 260/260. Integration 65/65.

### Task 5: The authorization handler — ✅ COMPLETE

Shipped as specified. `internal sealed`, `context.Succeed(requirement)` on the passed instance only, no `context.Fail()`, scoped services from `HttpContext.RequestServices` with the child scope as a non-HTTP fallback, and no database read on any Administrator or non-`RegularUser` path. 25 tests.

Three changes came out of review, all verified against the framework source rather than reasoned about:

1. **The guard became `context.User?.Identity?.IsAuthenticated != true`.** `Identity is null` did not cover its own rationale: a `ClaimsIdentity` built with no `authenticationType` has a non-null `Identity`, `IsAuthenticated == false`, and `IsInRole("RegularUser") == true`, so such a principal carrying a real user's `Sid` reached the lookup. Not reachable through library code, since the provider always adds `RequireAuthenticatedUser()`, but `PermissionRequirement` is publicly constructible.
2. **`ScopedPermissionCache` moved to `ConcurrentDictionary`.** The earlier conclusion that `internal` made single-threaded access structural was wrong — a consumer can drive concurrent resource-based authorization through the public `IAuthorizationService` with a public policy name, sharing one request scope and therefore one cache.
3. **A pre-existing privilege escalation in `MultiJwtClaimsTransformer` was fixed on this branch**, with maintainer approval. See the spec section "Pre-Existing Security Fix Included In This Branch".

**Verified framework behavior worth not re-deriving:**

* `ClaimsPrincipal.IsInRole` tests each identity against **that identity's own** `RoleClaimType`. The Keycloak JWT identity has `RoleClaimType = "role"` and never carries the Administrator role; `MultiJwtClaimsTransformer` grafts a second identity whose default `RoleClaimType` is `ClaimTypes.Role`, and that is what matches. The `RoleClaimType = "role"` setting is a red herring for this handler. A test now pins that two-identity shape — moving the claim one identity over would deny every Administrator and, before that test, every other test would still have passed.
* **An exception from the handler is fail-closed.** Both a transient fault and `OperationCanceledException` propagate to a 500 with the endpoint never reached; nothing converts a throw into a pass.
* **A real client abort logs nothing at warning or error level** and does not reach the endpoint — `RequestAborted` does not produce confusing 500s.
* **Allowed and denied cost the same** — the cache fetches the whole merged set and compares in memory, so there is no permission-enumeration timing oracle.
* **A requirement with no registered handler stays pending** → denied. A missing policy provider makes the framework throw. Both fail closed.

The original detail follows.

---

### Task 5 (original detail): The authorization handler

**Files:**
- Create: `Cause.SecurityManagement.Core/Authentication/PermissionAuthorizationHandler.cs`
- Test: `Cause.SecurityManagement.Tests/Authentication/PermissionAuthorizationHandlerTests.cs`

**Interfaces:**
- Consumes: `PermissionRequirement` (Task 4), `ScopedPermissionCache.HasPermissionAsync` (Task 3).
- Produces: `PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>`, constructed with `(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)`.

**The rule.** `Administrator` → succeed when `AllowAdministrator`, otherwise deny, never a database read. `RegularUser` → succeed only when the permission is held. Everything else → deny with no database read.

Read scoped services from `HttpContext.RequestServices`, **not** a new child scope: `CreateAsyncScope()` per check would produce a fresh `ScopedPermissionCache` each time and memoize nothing.

#### Non-negotiable requirements from the Task 4 security review

These are the identified ways this handler could introduce a real authorization bypass.

1. **Call `context.Succeed(requirement)` on the requirement instance passed to `HandleRequirementAsync` — never iterate `context.Requirements` and succeed them all.** `AuthorizeAttribute` sets `AllowMultiple = true` and is `Inherited = true`, so stacked attributes produce several `PermissionRequirement`s that must **all** be satisfied (AND). Succeeding them all from one check turns that into an OR. This was named the single highest-risk line in the task.
2. **Do not assume an authenticated principal.** `PermissionRequirement` has a public constructor, so a consumer can attach it to a hand-built policy with no `RequireAuthenticatedUser()`. Check `context.User` defensively rather than dereferencing it.
3. **Do not add a `ToString()` override to `PermissionRequirement`.** The framework's "requirements were not met" log line would then emit the permission tag.
4. **Test the two-attribute AND case explicitly** — two `PermissionRequirement`s on one context where the principal holds only one of the tags must not succeed.
5. **Add `AddAuthorizationForKeycloakAndRegularUserSchemes` to the test matrix.** It is the one registration variant where the mode rules are not a subset of the application's baseline, so it is where a composition mistake would widen access rather than narrow it.
6. **`[AllowAnonymous]` disables the gate entirely**, including inherited from a base controller. Add an HTTP-level test asserting the bypass so it is a known, pinned contract rather than a surprise.

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

### Task 6: Registration extension — ✅ COMPLETE

Shipped as specified, with all four load-bearing registration choices intact: `Replace` for the policy provider, explicit `AddHttpContextAccessor()`, `TryAddEnumerable` for the handler, `TryAddScoped` for the cache. Seven tests.

**The `Replace` choice was proven, not assumed.** Swapping it back to `TryAddSingleton` made `PermissionRegistrationAfterAuthorization_...` fail with "Expected type to be PermissionAuthorizationPolicyProvider, but found DefaultAuthorizationPolicyProvider" — the exact production bug, where a consumer calling an `AddAuthorizationFor*` helper first would silently keep the default provider and every gated endpoint would 500. The tests resolve from a built provider rather than inspecting `ServiceDescriptor` entries, which is why they catch it; descriptor inspection looks correct either way.

The XML doc records that `AddAuthorizationForKeycloakAndRegularUserSchemes` and `AddAuthorizationForExternalSystem` leave the gate with nothing useful to do. The README in Task 9 must repeat that.

The original detail follows.

---

### Task 6 (original detail): Registration extension

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
    services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>());
    services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());
    services.TryAddScoped<ScopedPermissionCache>();

    return services;
}
```

**Also required, from the Task 5 security review:**

* **Call `services.AddHttpContextAccessor()` explicitly.** The handler depends on `IHttpContextAccessor`; assuming the consumer registered it means the first gated request throws at DI resolution.
* **Document `AddAuthorizationForKeycloakAndRegularUserSchemes` and `AddAuthorizationForExternalSystem` as unsupported** for this feature. In the first, `[AdministratorOrUserWithPermission]` is a no-op — the fallback admits only `Administrator` and the handler passes every `Administrator` unconditionally — while `[UserWithPermission]` is deny-all. In the second, everything is deny-all because the handler always denies `ExternalSystem`. Neither is a security hole; both make the feature useless, which developers must be told rather than left to discover.
* **Consider guarding the explicit-policy-shadowing case.** `GetPolicyAsync` consults the default provider first, so a consumer who registers a policy literally named `Permission:AdministratorOrUser:SomeTag` — for instance by copying the `AddMetricsPolicy` pattern, which is `RequireAssertion(_ => true)` — gets an endpoint that looks gated and is not. Low likelihood, silent failure, ungated result. Either throw at startup on such a collision or combine both policies.

**`Replace`, not `TryAddSingleton` — this is a correctness bug, not a style preference.** An earlier draft of this plan used `TryAddSingleton`. `AddAuthorizationCore()`, which every one of the five `AddAuthorizationFor*` helpers calls, already does `TryAddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>()`. So a consumer writing

```csharp
services.AddAuthorizationForRegularUser();      // registers the default provider
services.AddPermissionBasedAuthorization();     // TryAdd → silent no-op
```

would get the default provider, and **every permission-gated endpoint would fail with `InvalidOperationException` → 500**, because the default provider cannot resolve a `Permission:` name. `Replace` makes registration order irrelevant.

Needs `using Microsoft.Extensions.DependencyInjection.Extensions;` for `Replace`.

**The registration test must build the provider and resolve `IAuthorizationPolicyProvider` in both registration orders.** Asserting on `ServiceDescriptor` entries alone would not have caught this — the descriptor list looks correct either way.

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

- [ ] **Step 4: Cover the MVC filter interactions — this replaces the obsolete scheme-list step**

The scheme-list hypothesis is **resolved and no longer needs testing**: Task 4 now inherits the whole fallback policy via `Combine`, which brings the schemes along, so there is no scheme-copying code left to delete.

What replaces it is more important. Security review established that `AuthorizationApplicationModelProvider` returns early under endpoint routing, so these attributes never become filters — meaning `UseDefaultAuthorizationWhenNotSpecifiedFilter` is **not** suppressed as the spec originally claimed. It runs, and `AuthorizeFilter.GetEffectivePolicyAsync` **unions** its policy with the endpoint metadata, which ANDs the requirements.

Add two more fixture hosts and determine empirically what each does:

| Host registration | Endpoint | Assert |
|---|---|---|
| `MvcOptionsExtensions.AskForAuthorizationByDefault` + `[AdministratorOrUserWithPermission]` on an action | controller action | Keycloak `Administrator` — expected **403**, because `"defaultpolicy"` requires `RegularUser` |
| same | controller action | `RegularUser` holding the tag — expected **200** |
| `AddAuthorizeFiltersControllerConvention` + the attribute on an **action** (controller type undecorated) | controller action | Keycloak `Administrator` — expected **403** |
| `AddAuthorizeFiltersControllerConvention` + the attribute on the **controller type** | controller action | the convention skips its default filter; record what actually happens |
| Any host, endpoint carrying `[AllowAnonymous]` **and** a permission attribute | either | unauthenticated request — expected **200**, the gate is bypassed |

**Report the actual observed status codes, not the expected ones.** If any differ from the table, that is a finding about the framework, not a test bug — record it in the spec and stop rather than adjusting the assertion to match.

The `[AllowAnonymous]` case is pinned deliberately: it is standard framework behavior, `AllowAnonymousAttribute` is `Inherited = true`, and this library's own `BaseAuthenticationController` uses it heavily — so a base controller carrying it would silently disable the gate on every derived controller. Better a known contract than a discovery.

- [ ] **Step 5: Commit**

```bash
git add Cause.SecurityManagement.Tests/Authentication/PermissionGateEndpointTests.cs docs/specs/2026-08-11-permission-based-gate.md
git commit -m "#115 - Add HTTP pipeline tests for the permission gate"
```

---

### Task 8b: End-to-end gate test against a real database

**Files:**
- Modify: `Cause.SecurityManagement.Integration.Tests/Cause.SecurityManagement.Integration.Tests.csproj`
- Create: `Cause.SecurityManagement.Integration.Tests/Authentication/PermissionGateEndToEndTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 1–7.
- Produces: nothing.

**Why this task exists — it is the most valuable test in the feature.**

Task 1 proves the SQL. Task 8 proves the HTTP pipeline but stubs `IUserPermissionService`. Nothing proves the two connect. Every one of these failures leaves both other layers green while production is broken:

* The DI registration is wrong, so `ScopedPermissionCache` or `IUserPermissionService` fails to resolve inside the handler.
* The handler resolves the cache from `HttpContext.RequestServices` but the request scope does not contain the security services.
* The `Sid` claim on the token does not match the `IdUser` column the repositories filter on, so every lookup silently returns nothing and every `RegularUser` gets 403.

That last one is the dangerous case: it fails closed, so it looks like a permissions-data problem in production rather than a bug.

**Placement.** This belongs in `Cause.SecurityManagement.Integration.Tests` — it needs Testcontainers PostgreSQL, which only that project has. It needs **only** `Microsoft.AspNetCore.TestHost` added; no reference to the `Cause.SecurityManagement` MVC package, because the whole gate (attributes, requirement, policy provider, handler, cache, registration) lives in `Core`, which the project already references. Use minimal-API endpoints with `.RequireAuthorization(PermissionPolicy.NameFor(...))` rather than controllers.

- [ ] **Step 1: Add the TestHost package**

```bash
dotnet add Cause.SecurityManagement.Integration.Tests/Cause.SecurityManagement.Integration.Tests.csproj package Microsoft.AspNetCore.TestHost --version 10.0.10
```

Pin `10.0.10` to match `Cause.SecurityManagement.Tests`. Confirm the csproj diff shows only that one addition.

- [ ] **Step 2: Read the two patterns you are combining**

Read `Cause.SecurityManagement.Integration.Tests/Infrastructure/IntegrationTestBase.cs` and `Infrastructure/DatabaseFixture.cs` for the database side, and the `PermissionGateEndpointTests` you wrote in Task 8 for the TestServer side. Also re-read `Cause.SecurityManagement.Integration.Tests/Repositories/GroupPermissionRepositoryTests.cs` for the seeding helpers — reuse that seeding approach rather than inventing another.

`IntegrationTestBase` builds a bare `ServiceCollection`, not a web host, so this fixture will **not** inherit it. Build a `HostBuilder().ConfigureWebHost(...)` host whose services include the real `TestSecurityContext` from `DatabaseFixture.CreateContext()`, then `InjectSecurityServices<TestUser>()` and `AddPermissionBasedAuthorization()`.

- [ ] **Step 3: Write the tests**

Create `Cause.SecurityManagement.Integration.Tests/Authentication/PermissionGateEndToEndTests.cs`.

Seed **real rows** — a `TestUser`, a `Module`, two `ModulePermission` rows, and `UserPermission` rows granting one and denying the other. Authenticate with a principal carrying `ClaimTypes.Role = SecurityRoles.User` and `JwtRegisteredClaimNames.Sid = <the seeded user's Id>`. Use the same header-driven `AuthenticationHandler` approach as Task 8 so no real token signing is needed.

Required cases:

| Case | Expected | What it catches |
|---|---|---|
| Seeded user holds the permission | 200 | the full chain works |
| Seeded user's row has `IsAllowed = false` | 403 | the flag is honored end to end, not just parsed |
| Seeded user has no row for the tag | 403 | absence denies |
| Permission granted via a **group** the user belongs to | 200 | the group query path, which the user-permission path does not cover |
| Group denies while the user row allows the same tag | 403 | deny-wins survives the whole chain, not just `PermissionMergeTool` |
| `Sid` claim set to a random Guid that matches no user | 403 | proves the claim is actually used to look up rows |
| Two requests in one test, same user | 200 twice | the per-request cache does not leak or corrupt state across requests |

The deny-wins case and the mismatched-`Sid` case are the two that justify this task. Do not drop them.

**Also add one sync/async equivalence assertion here.** The synchronous and asynchronous permission paths now execute *different SQL* for the same logical question — the sync path materializes entities and projects in memory, the async path projects server-side and reaches group permissions through `WHERE EXISTS`. Both are correct today, but nothing would notice if they drifted, and "drifted" means an authorization gate and a permissions UI disagreeing about whether a user holds a permission.

Unit tests cannot catch this because they mock the repositories. With real rows already seeded here, it costs one test:

```csharp
var service = Resolve<IUserPermissionService>();

var synchronous = service.GetPermissionsForUser(user.Id);
var asynchronous = await service.GetPermissionsForUserAsync(user.Id, CancellationToken.None);

asynchronous.Should().BeEquivalentTo(synchronous,
    "the sync and async paths run different SQL and must not answer differently");
```

Seed the user with both a direct `UserPermission` and a group-derived `GroupPermission`, including one denial, so the assertion exercises both sources and the deny-wins merge.

- [ ] **Step 4: Run the tests**

Run: `dotnet test Cause.SecurityManagement.Integration.Tests/Cause.SecurityManagement.Integration.Tests.csproj --nologo --filter PermissionGateEndToEndTests`
Expected: all pass. Requires Docker. **If Docker is unavailable, report the tests as written-but-not-executed — do not claim they passed.**

If the mismatched-`Sid` case returns 200, stop: that means the handler is not filtering by user and every authenticated `RegularUser` would pass every gate. That is a critical finding, not a test bug.

- [ ] **Step 5: Run the full suites and commit**

```bash
dotnet build Cause.SecurityManagement.sln -c Release --nologo --no-incremental -p:GeneratePackageOnBuild=false
dotnet test Cause.SecurityManagement.Tests/Cause.SecurityManagement.Tests.csproj --nologo
dotnet test Cause.SecurityManagement.Integration.Tests/Cause.SecurityManagement.Integration.Tests.csproj --nologo
```

```bash
git add Cause.SecurityManagement.Integration.Tests/
git commit -m "#115 - Add end-to-end permission gate tests against a real database"
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

- [ ] **Step 3: Bump the package versions to 10.7.0**

`docs/RELEASING.md` § Semver Bump Rules classifies this feature as **additive → MINOR**, and requires the same version across all three published packages. Every interface member added in this work is a default implementation specifically to keep it additive.

All three of `Cause.SecurityManagement.Models`, `Cause.SecurityManagement.Core`, and `Cause.SecurityManagement` currently read `10.6.0`. In each `.csproj` update `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` to `10.7.0`, and add a `<PackageReleaseNotes>` line describing the permission gate.

Do **not** run `release.ps1` — publishing is a separate deliberate step per the multi-package release governance ADR.

- [ ] **Step 4: Flip the ADR to accepted**

In `docs/adr/records/2026-08-11-permission-based-authorization-gate.md`, change `* Status: proposed` to `* Status: accepted`, tick every `Implementation Plan` checkbox that is done, and update Task 7's entry to record the scheme-list outcome from Task 8 Step 4.

In `docs/adr/records/overview.md`, change this ADR's Status column from `proposed` to `accepted`.

- [ ] **Step 5: Full verification**

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

- [ ] **Step 6: Commit**

```bash
git add README.md docs/ Cause.SecurityManagement.Models/ Cause.SecurityManagement.Core/ Cause.SecurityManagement/
git commit -m "#115 - Document the permission gate, bump to 10.7.0, and accept the ADR"
```

---

## Deferred, Not Forgotten

Recorded in the spec's out-of-scope section; do **not** do these in this branch:

- Cross-request or distributed permission caching.
- Any change to `PermissionMergeTool` merge semantics.
- The unresolved maintainer comment at `MultiJwtClaimsTransformer.cs:32` about whether Keycloak principals should receive `Administrator`, and the `"Administrator"` string literal on line 34 that should be `SecurityRoles.Administrator`. This design depends on that behavior, so changing it later requires revisiting this spec.
- A NuGet version bump and release. Per the multi-package release governance ADR, releases are a separate deliberate step.
