# Distinguish Data-Integrity Faults From Rejected Certificates In Certificate Authentication

* Status: accepted
* Date: 2026-08-13
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Issue #115

## Context and Problem Statement

`CertificateAuthenticationHandler` wrapped its whole authentication path in a
single `catch (Exception)` that logged at `Information` and returned
`AuthenticateResult.Fail`. Every failure — a missing certificate, an untrusted
issuer, an unknown subject, or a fault inside the library itself — produced the
same 401 and the same log line.

A production incident in `geoloc-external-webapi` exposed the cost. Two active
certificate-bound `ExternalSystem` rows shared one `CertificateSubjectDn`, so the
`SingleOrDefault` in `ExternalSystemRepository.GetByCertificateSubject` threw
`InvalidOperationException("Sequence contains more than one element")`. The
catch-all swallowed it. The integrating system saw a plain 401 and the operators
saw an `Information`-level "Certificate authentication failed." — the same output
a client presenting a revoked certificate would produce. The actual condition was
a data-integrity fault in our own database that no amount of client-side
investigation could resolve.

The distinction matters because the two classes of failure have different owners.
A rejected certificate is the caller's problem and 401 tells them so correctly. A
duplicate subject DN is our problem, is not resolvable by the caller, and stays
invisible until someone reads a stack trace.

## Decision Drivers

* An operator must be able to tell a rejected certificate from a broken server
  state without reading source or stack traces.
* Fail closed. Nothing may authenticate that did not authenticate before.
* Keep the change small and additive; this is a bug fix on a published package,
  not a redesign of the handler.
* Avoid a bare framework `InvalidOperationException` as the carrier of a
  domain condition.

## Considered Options

* **Option A**: Detect the duplicate explicitly in the repository, throw a
  dedicated `DuplicateCertificateSubjectException`, catch that specific type in
  the handler, log at `Error`, and rethrow so it surfaces as a 500.
* **Option B**: Keep returning 401 but raise the existing catch-all log to
  `Error` or `Warning`.
* **Option C**: Treat the duplicate as "no match" and return `null`, producing
  the existing `ExternalSystemNotFound` path and a 401.
* **Option D**: Pick one row deterministically — for example `OrderBy(Id).First()`
  — and authenticate against it.

## Decision Outcome

Chosen option: **Option A**.

Option B was rejected because the status code is the part downstream monitoring
actually keys on, and a 401 asserts something false: that the caller's
credentials were rejected. Raising the log level alone would also promote genuine
certificate rejections — a routine, client-caused event — to `Error`, which
inverts the signal-to-noise problem rather than solving it.

Option C was rejected for the same reason and is worse: it makes the server fault
deliberately indistinguishable from a legitimate miss.

Option D was rejected outright. Authenticating against an arbitrary one of two
ambiguous identities is a silent privilege decision. The two rows may carry
different `Name` values and different downstream authorization, so picking one
would hand an integration an identity nobody assigned it. Ambiguous identity must
fail, not resolve.

### Failure Semantics

| Condition | Status | Log level | Owner |
|---|---|---|---|
| No certificate presented | 401 | Information | caller |
| Untrusted issuer, malformed subject | 401 | Information | caller |
| Subject DN matches no active system | 401 | Information | caller |
| Subject DN matches more than one active system | **500** | **Error** | **us** |

The specific `catch` precedes the general one, so every pre-existing 401 path is
untouched. The rethrow uses a bare `throw;` to preserve the original stack trace.

### Detection Belongs In The Repository

`GetByCertificateSubject` runs `Where(...).Take(2).ToList()` and throws when it
gets two rows. `Take(2)` is deliberate: EF Core already emitted `LIMIT 2` for
`SingleOrDefault`, so the query cost is unchanged, and the guard runs before
`FirstOrDefault()` can ever pick between candidates. The consequence is that the
exception reports *that* duplicates exist, not how many — an accepted trade for
not scanning the table.

This places a type from `Core/Authentication/Exceptions/` in the repository's
`using` list. That is a mild layering inversion, accepted because it is
intra-assembly, the condition is semantically about certificate authentication,
and the folder already collects every auth exception in one place.

## Consequences

* Good: A duplicate subject DN now pages instead of hiding. The `Error` log names
  the condition, the DN, and the remediation.
* Good: Genuine certificate rejections keep their 401 and their quiet
  `Information` log, so the new `Error` signal stays meaningful.
* Good: Fail-closed. No principal authenticates that did not authenticate before;
  the change only alters how a failure is reported.
* Bad: **This is an observable contract change for consumers.** A published
  package that returned 401 for this condition now returns 500. Consumers with
  alerting keyed on 5xx will begin paging on a state that previously sat silent.
  That is the intent, but it must be called out in release notes rather than
  discovered.
* Bad: `IExternalSystemRepository.GetByCertificateSubject` can now throw where it
  previously returned. Only `CertificateAuthenticationHandler` calls it inside
  this library, but the interface is public and consumers may implement or call
  it.
* Bad: **Error-log volume multiplies.** A triggering request now produces at
  least two `Error` entries with stack traces — the handler's own, and ASP.NET
  Core's unhandled-exception entry — where it previously produced one
  `Information` entry with none. Consumers using the re-execute form
  `app.UseExceptionHandler("/error")` get a third, because re-execution replays
  the cached faulted authentication task. The affected integration is typically
  the one retrying on a poll, so the multiplier lands on a sustained loop rather
  than a single event. This is inherent to log-then-rethrow and cannot be removed
  without giving up one of the two goals; dedupe on `CertificateSubjectDn` at the
  log aggregator and alert on first occurrence per DN per window.
* Bad: Consumers supplying their own handler through
  `AddCertificateAuthenticationWithCustomHandler<THandler>` get no benefit if
  their handler has its own catch-all — the exception is swallowed there and they
  still see a 401.
* Bad: Under `ASPNETCORE_ENVIRONMENT=Development` the 500 response body carries
  the exception message, which includes the subject DN. Production returns an
  empty body. This is standard ASP.NET Core behavior and not introduced here, but
  it is a surface that did not exist while the exception was being swallowed.
* Bad: `GetByApiKey` still uses `SingleOrDefault` and retains the identical latent
  bug. Left deliberately out of scope; the two sibling methods now handle the same
  class of fault differently.
* Bad: The change makes the bad state loud without making it impossible. Nothing
  at the schema level prevents duplicate DNs.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- The `catch (DuplicateCertificateSubjectException)` must stay ahead of the
  general `catch (Exception)`. Reordering silently restores the 401 behavior this
  record exists to remove.
- The rethrow must stay a bare `throw;`. `throw exception;` resets the stack trace
  and discards the origin of the fault.
- The duplicate guard must run before any row is selected. Never resolve an
  ambiguous certificate subject by ordering and taking the first — that converts a
  detected fault into a silent identity choice.
- Maintain the test asserting a duplicate subject DN yields 500 and not 401, and
  the tests asserting that an unknown subject, an inactive duplicate, and a
  `Token`-type row with a matching DN all keep their existing behavior. The
  active/inactive and `Certificate`/`Token` cases are what prove the guard does not
  over-trigger.
- Maintain the test asserting the fault logs at `Error` with a structured
  `CertificateSubjectDn` field. Log level and message content are otherwise
  unprotected: the 500 test alone still passes if `LogError` is downgraded or the
  placeholder is dropped.
- Any future change that adds a duplicate-detecting query elsewhere in
  `ExternalSystemRepository` — `GetByApiKey` being the obvious candidate — should
  follow this same shape rather than inventing a second convention.
- A schema-level guarantee (a filtered unique index on `CertificateSubjectDn` for
  active certificate-bound rows) would make this state unrepresentable and
  supersede the runtime guard. It requires its own record, because it needs a
  data-cleanup step sequenced ahead of the migration for consumers that already
  hold duplicates.

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
- [x] Task 1: Add `DuplicateCertificateSubjectException` under
      `Cause.SecurityManagement.Core/Authentication/Exceptions/`, carrying the
      offending DN as a public property.
- [x] Task 2: Rewrite `ExternalSystemRepository.GetByCertificateSubject` as
      `Where(...).Take(2).ToList()` with an explicit duplicate guard.
- [x] Task 3: Add the specific `catch` in
      `CertificateAuthenticationHandler.HandleAuthenticateAsync` — `LogError` with
      a structured `{CertificateSubjectDn}` placeholder, then `throw;`.
- [x] Task 4: Cover the repository guard (duplicate, active/inactive,
      `Certificate`/`Token`) and the 500-versus-401 outcome through the HTTP
      pipeline, plus the `Error` log assertion.
- [x] Task 5: Build with no warnings, run the full unit and integration suites,
      and flip this ADR to `accepted`.
</content>
