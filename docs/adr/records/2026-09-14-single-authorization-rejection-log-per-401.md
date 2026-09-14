# Emit One Authorization Rejection Log Per 401

* Status: accepted
* Date: 2026-09-14
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Issue #119

## Context and Problem Statement

`CertificateAuthenticationHandler.HandleAuthenticateAsync` logged
`Information: "Certificate authentication failed."` for every failure it caught.
Operators reported the line firing on essentially all traffic, including traffic
that had nothing to do with certificates.

Four mechanics combine, none of them under the handler's control:

* `AddExternalCertificateAuthentication` and `AddAuthenticationWithScheme` set
  `DefaultAuthenticateScheme`. ASP.NET Core's `AuthenticationMiddleware`
  authenticates the default scheme on **every** request, before routing — so
  health checks, swagger and `[AllowAnonymous]` endpoints all ran the handler.
* **The reverse proxy always sets the certificate headers.** nginx terminates TLS
  and forwards `ssl-client-verify` on every request whether or not a client
  certificate was used; a call without one arrives as `ssl-client-verify: NONE`.
  `CertificateValidator.ClientVerifyValidation` turns that into
  `CertificateNotPresentException`. There is therefore no request on which the
  certificate handler declines to act — it runs and fails on all of them.
* When an `AuthorizationPolicy` lists several schemes, `PolicyEvaluator`
  authenticates each in turn and merges identities, so a caller already
  authenticated by another scheme still ran the certificate handler.
* `AddDualExternalSystemAuthentication`'s `ForwardDefaultSelector` forwards to the
  certificate scheme whenever no `Bearer` header is present.

The requirement that emerged is narrower and stricter than "log less":

> Exactly one `Information` line per 401, naming the exact reason — no credentials
> at all, an invalid certificate, an invalid token, or anything else. Nothing at
> all when authentication was never the point.

That requirement is a property of **the rejection**, not of any handler. An
authentication handler knows only its own outcome. It does not know whether a 401
is the final answer, and it cannot report a failure that occurred in a *different*
scheme — an expired JWT is invisible to the certificate handler. Any design that
logs inside a handler therefore cannot satisfy the requirement.

## Decision Drivers

* One line per 401, carrying the actual reason, whatever the mechanism.
* Silence when certificate authentication was not being attempted — the primary
  complaint.
* Fail closed. Nothing may authenticate that did not authenticate before, and no
  status code may change.
* Do not disturb the failure semantics established by
  [2026-08-13-certificate-authentication-failure-semantics.md](2026-08-13-certificate-authentication-failure-semantics.md),
  whose maintenance invariants govern the same handler.
* Replace a component a consumer may already have supplied only with a documented
  opt-out and a visible signal at startup - never silently.

## Considered Options

* **Option A**: Classify by exception type inside `HandleAuthenticateAsync` — log
  `CertificateNotPresentException` at `Debug`, keep `Information` otherwise.
* **Option B**: Move the handler's logging into a `HandleChallengeAsync` override,
  which the framework invokes only when the failure produced a challenge.
* **Option C**: Stop setting `DefaultAuthenticateScheme`.
* **Option D**: Log once from an `IAuthorizationMiddlewareResultHandler`, which
  runs exactly once per request and receives the final `PolicyAuthorizationResult`.

## Decision Outcome

Chosen option: **Option D**, with Option B's supporting result-shape changes retained.

Option A silences by *category* rather than by *consequence*: it cannot distinguish
a missing certificate nobody needed from a missing certificate that just caused a
401, because both raise the same exception.

Option B was implemented first and is a real improvement — challenge-time logging
does go quiet on anonymous and already-authenticated requests. It was rejected as
the final answer for two reasons. It cannot report a failure from another scheme,
so an invalid token is outside its reach; and it still leaves several lines per
401, because the handler's own line joins the framework's per-scheme diagnostics.
It is the right instinct applied one layer too low.

Option C was rejected as disproportionate and ineffective: dropping
`DefaultAuthenticateScheme` stops populating `HttpContext.User` on anonymous
endpoints, which consumers may rely on, and does nothing for a policy that names
the scheme explicitly.

### Where The Single Line Comes From

`AuthorizationRejectionLogger` implements `IAuthorizationMiddlewareResultHandler`.
It runs once per request. When `authorizeResult.Challenged` is true — the 401 case,
distinct from `Forbidden`, which is a 403 and out of scope here — it reads the
already-cached `AuthenticateResult` of each relevant scheme and writes one line:

* the policy's `AuthenticationSchemes` when it names any, otherwise the default
  scheme;
* `Failure` present → that exception's message is the reason;
* `None` → no credentials of that scheme were presented;
* `Succeeded` → contributes no reason.

Reading the results is free. `AuthenticationHandlerProvider` is registered scoped
and caches handler instances per scheme, and `HandleAuthenticateOnceAsync` caches
the task, so `context.AuthenticateAsync(scheme)` returns the result the middleware
already computed rather than re-running authentication.

Logging is wrapped so that a failure to determine the reason degrades to a warning
and never prevents delegation. The inner `AuthorizationMiddlewareResultHandler`
always runs, so challenge and forbid behavior is unchanged.

### Why Registration Is On By Default

Every `AddAuthorizationFor*` extension registers the rejection logger itself, through
a trailing `logAuthorizationRejections` parameter defaulting to `true`. Existing call
sites therefore gain the behavior by upgrading, with no source change.

An opt-in design was built first and rejected. Its failure mode is silent and points
the wrong way: a consumer who upgrades without making the call gets *less* logging
than 10.9.1 — the certificate handler no longer logs, the validator logs only at
`Debug`, and nothing replaces them at `Information` — with no signal that a step was
missed. Weighed against that, the
argument for opt-in was thin. It rested on not silently replacing a consumer's own
`IAuthorizationMiddlewareResultHandler`, but neither this library nor the consumer
repositories inspected had one, a custom handler is a rare thing to write, and these
same extensions already set an application-wide `FallbackPolicy` — a far larger
opinion than swapping a result handler.

`AddAuthorizationRejectionLogging()` remains public for applications that build
authorization without the `AddAuthorizationFor*` extensions. It uses `Replace`, so it
is idempotent and order-independent: the framework registers its own default through a
`TryAdd` inside `AddAuthorizationPolicyEvaluator()`, which no-ops once ours is present.
That registration is `Transient`; ours is a `Singleton`, which is safe because the class
holds no mutable state and its only dependencies are an `ILogger` and a stateless
framework handler.

Note also that `AddAuthorizationCore` — what the `AddAuthorizationFor*` extensions call —
does not register an `IAuthorizationMiddlewareResultHandler` at all. Opting out therefore
leaves the service absent rather than falling back to a framework default.

### The Supporting Result Shapes Are Load-Bearing

Two changes introduced for Option B are retained because Option D depends on them:

* A missing certificate returns `AuthenticateResult.NoResult()` rather than a
  failure. It is the correct semantic — the handler found no credentials of its
  kind and has no opinion, matching `JwtBearerHandler` for a missing
  `Authorization` header — and it is what lets the rejection logger distinguish
  "nothing was presented" from "something was presented and rejected".
* Every other failure returns `AuthenticateResult.Fail(exception)` rather than
  `Fail(string)`. The string overload wraps the message in a synthetic
  `AuthenticationFailureException` and discards the real exception; the rejection
  logger needs the real one to name the reason.

### What Stopped Logging

* `CertificateAuthenticationHandler` no longer logs on any authentication-failure
  path. Its `HandleChallengeAsync` override is gone.
* `CertificateValidator` no longer logs at `Information`. Its three calls each logged
  text that was immediately thrown as the exception message, which the rejection
  logger now reports — duplication at the level operators actually watch, and a second
  copy of caller-supplied header content there.

  They were removed outright in the first version of this change and restored at
  `Debug` during review. Restoring them costs nothing at `Information`, keeps the
  discrete `{SslclientSubjectDn}`, `{SslClientIssuerDn}` and `{SslClientVerify}`
  structured fields available to anyone who raises this one category to `Debug`, and
  avoids a breaking constructor change. The missing-certificate path
  (`ssl-client-verify: NONE` or absent) still logs nothing at any level: that is the
  every-request case, and a `Debug` line there would be noise on all traffic.

### The Framework's Own Diagnostics

`AuthenticationHandler` emits its own lines — `"{scheme} was not authenticated.
Failure message: …"` when `Failure` is set, and `"AuthenticationScheme: {scheme}
was challenged."` on every challenge — using the **handler's type** as the logger
category. They cannot be suppressed from this library: `ChallengeAsync` is not
virtual, and the first line is driven by a result shape we need.

This is why `AuthorizationRejectionLogger` logs under its own category. A consumer
wanting strictly one line per 401 raises the handler categories to `Warning`, for
example:

```json
"Logging": {
  "LogLevel": {
    "Cause.SecurityManagement.Core.Authentication.Certificate.CertificateAuthenticationHandler": "Warning",
    "Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler": "Warning"
  }
}
```

Note that the missing-certificate case already avoids the `Information` variant:
`NoResult` makes the framework log at `Debug`, not `Information`. The filter
matters mainly for presented-but-rejected credentials.

**That filter has a security cost, and a consumer doing security monitoring should
weigh it rather than copy the snippet.** Since the certificate handler and validator
no longer log, the framework's "was not authenticated" line is the only remaining
signal for a bad credential presented on a request that did *not* end in 401 - a
forged or expired certificate probed against an `[AllowAnonymous]` health, swagger or
error endpoint, or one rescued by another scheme in a multi-scheme policy. Raising the
category to `Warning` discards that signal at the source. The safer shape for a
monitored deployment is to leave the application at `Information` and filter at the log
sink instead, where rules can match on message text: the dashboard shows one line per
401 while the probe traffic stays in storage. The `appsettings` filter is the right
choice only where simplicity outweighs that visibility.

### Failure Semantics

Status codes are unchanged from the 2026-08-13 record. Only logging changes.

| Condition | Status | `AuthenticateResult` | Logged by |
|---|---|---|---|
| Authentication not attempted, request allowed | as the endpoint dictates | `NoResult` | nothing |
| No credentials presented, request rejected | 401 | `NoResult` | rejection logger, one `Information` line |
| Invalid certificate (issuer, subject, verify status) | 401 | `Fail` | rejection logger, one `Information` line carrying the reason |
| Certificate subject matches no active system | 401 | `Fail` | rejection logger, one `Information` line |
| Invalid or expired token | 401 | `Fail` | rejection logger, one `Information` line |
| Certificate subject matches more than one active system | 500 | n/a — rethrows | `CertificateAuthenticationHandler`, eagerly, at `Error` |

The duplicate-subject row is deliberately unchanged and deliberately eager. Its
bare `throw;` propagates out of `HandleAuthenticateOnceAsync` and past
`PolicyEvaluator` to the unhandled-exception path, so the authorization middleware
is never reached and no `IAuthorizationMiddlewareResultHandler` runs. The rejection
logger structurally cannot cover it, which is why its `LogError` stays in the
handler where the earlier record put it.

## Consequences

* Good: A 401 produces one line naming the real reason, uniformly across
  certificates, tokens and no credentials at all. Previously the reason was spread
  across up to four lines from three categories, or absent for schemes other than
  the certificate one.
* Good: Requests where authentication was not the point produce nothing. An
  `[AllowAnonymous]` endpoint is short-circuited by `AuthorizationMiddleware` before
  the result handler is even resolved, so the rejection logger never runs. The test
  covering this proves the end-to-end outcome — no line on traffic that never wanted
  authentication — rather than the `Challenged` guard specifically, which is pinned
  by the successful-authentication test instead.
* Good: The reason is composed where the outcome is actually known, so adding a new
  authentication scheme needs no new logging code.
* Good: One fewer copy of caller-supplied header content at `Information`, now that
  `CertificateValidator` logs what it throws at `Debug` instead.
* Bad: **A consumer who opts out gets less logging than before, not more.** Passing
  `logAuthorizationRejections: false` leaves the certificate handler silent and the
  validator at `Debug` with nothing replacing them at `Information`, so only the
  framework's own diagnostics remain there.
  The default prevents this from happening by accident, but the parameter makes it
  reachable on purpose.
* Bad: **Applications not using the AddAuthorizationFor* extensions still get nothing
  until they call `AddAuthorizationRejectionLogging()` themselves.** The default only
  covers consumers who register authorization through this library.
* Bad: **Observable contract change on `AuthenticateResult`.** `Failure` is now
  `null` when no certificate was presented, and carries the original exception
  rather than an `AuthenticationFailureException` elsewhere. A consumer branching
  on `Failure != null` instead of `Succeeded` would admit an unauthenticated
  caller. Status codes do not change on any path reachable through the
  authorization pipeline.
* Bad: Strictly one line still requires a consumer-side log filter, because the
  framework's own per-scheme diagnostics share the handler's category and cannot be
  suppressed from here - and that filter costs visibility of failed credentials on
  requests that were not rejected. See the note above; filtering at the sink avoids
  the trade.
* Bad: **The structured header fields move to `Debug`.** 10.9.1 emitted
  `{SslclientSubjectDn}`, `{SslClientIssuerDn}` and `{SslClientVerify}` as discrete
  fields at `Information`, which a SIEM could aggregate on. They keep their exact
  names and shapes, but a consumer relying on them must now raise the
  `CertificateValidator` category to `Debug` — and at `Information` the same values
  are available only as free text inside `{RejectionReason}`. The scheme name is
  additionally exposed as a queryable `{RejectionScheme}` field via a logging scope,
  which reaches only providers configured to capture scopes (`IncludeScopes`).
* Bad: The reason text is truncated at 512 characters. A pathologically long failure
  message loses its tail, which is the correct trade against attacker-influenced log
  throughput but is a loss nonetheless.
* Bad: Registering the rejection logger replaces whatever
  `IAuthorizationMiddlewareResultHandler` was registered before it. It delegates to
  the built-in one, not to a previously registered custom one, so a consumer with
  their own handler must compose manually.
* Bad: The rejection reason is built from exception messages, which for
  `CertificateValidator` failures embed caller-supplied header values. This is the
  same content the validator previously logged itself, so it is not a new
  disclosure class, but it is now the text of the one line operators read.
* Bad: 403 is out of scope. A `Forbidden` result — authenticated but lacking the
  required role — produces no line from this component.
* Bad: **"One line per 401" is one line per *rejected request*, which is not always
  one line per *client* 401.** `UseStatusCodePagesWithReExecute` re-executes the
  pipeline against an error endpoint, and every `AddAuthorizationFor*` extension in
  this library sets a `FallbackPolicy`, so that error endpoint is itself protected
  unless the consumer marks it `[AllowAnonymous]`. The re-executed request is then
  genuinely rejected too, and a second line appears — for `/error/401` rather than
  the original path. Both lines are truthful, so the logger is not wrong; the
  remedy is to mark the error endpoint `[AllowAnonymous]`, which is sound advice
  independent of logging. `UseExceptionHandler` does not have this problem: a 401 is
  not an exception, so nothing re-executes.
* Bad: **The logged scheme name is the one that was asked, not always the one that
  failed.** When the default scheme is a policy scheme that forwards — as
  `AddDualExternalSystemAuthentication` does — `context.AuthenticateAsync()` returns
  the forwarded handler's result, but the scheme name available to us is the policy
  scheme's. Every line from such a consumer is labelled `DualExternalSystemScheme`
  whether a certificate or a token failed. The concrete reason in the same line still
  distinguishes them, but the line cannot be aggregated or filtered by the underlying
  scheme in a log tool. Resolving the forward target would mean reading
  `ForwardDefaultSelector` out of the policy scheme's options, coupling this
  component to policy schemes; judged not worth it, but the limitation is real.
* Bad: The rejection reason is composed from exception messages, so the framework's
  own sibling line — which interpolates the same `Failure.Message` — changed text
  when `Fail(string)` became `Fail(exception)`. Monitoring keyed on that line's text
  must be updated. A host that surfaces `AuthenticateResult.Failure.Message` in a
  response body likewise now echoes a concrete message rather than a constant.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- `AuthorizationRejectionLogger` must always delegate to the inner
  `AuthorizationMiddlewareResultHandler`, on every path including when logging
  throws. It decorates; it must never become the whole implementation.
- It must log only when `authorizeResult.Challenged` is true. Logging on
  `Forbidden` or on success reintroduces exactly the noise this record removes.
- It must read scheme results through `context.AuthenticateAsync(...)` and rely on
  the per-request cache. Never re-run authentication to build a log message.
- It must keep its own logger category, distinct from any authentication handler's.
  Moving its line under a handler's category makes the framework's diagnostics
  unfilterable without also losing this line.
- Registration must stay on by default in the AddAuthorizationFor* extensions. The
  opt-out parameter exists for a consumer with their own result handler; flipping the
  default back to off silently reduces logging on upgrade, which is the failure mode
  this design was changed to avoid.
- No authentication handler may resume logging its own failures. Doing so restores
  the duplication this record exists to remove. A test guards this by asserting that
  no entry outside the rejection logger's category carries the authentication
  exception object; the framework's own diagnostics are text-only and so do not trip
  it. Do not weaken that assertion to a category count — counting entries from the
  rejection logger alone cannot see a handler that starts logging again.
- A missing certificate must keep returning `NoResult()`, and other failures must
  keep returning `Fail(exception)`. Both are inputs the rejection logger depends on
  to tell "nothing presented" from "presented and rejected" and to name the reason.
- The `catch (DuplicateCertificateSubjectException)` must stay first in
  `HandleAuthenticateAsync`, keep its eager `LogError` with the structured
  `{CertificateSubjectDn}` placeholder, and keep its bare `throw;`. It never
  reaches the authorization middleware, so the rejection logger cannot cover it.
- Maintain the test asserting an `[AllowAnonymous]` request with
  `ssl-client-verify: NONE` produces zero entries from the rejection logger. That is
  the user-reported defect this record exists to fix.
- Maintain the tests asserting exactly one entry for each of: no credentials, an
  invalid certificate, and an invalid bearer token. The count is the requirement;
  an assertion that merely checks "at least one" does not protect it.

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
- [x] Task 1: Add `AuthorizationRejectionLogger` implementing
      `IAuthorizationMiddlewareResultHandler`, delegating to the built-in handler.
- [x] Task 2: Add `AddAuthorizationRejectionLogging()`, called by default from every
      AddAuthorizationFor* extension through an opt-out parameter, with
      XML docs naming the categories a consumer filters.
- [x] Task 3: Delete `CertificateAuthenticationHandler.HandleChallengeAsync` and its
      logging helper; keep `NoResult()`, `Fail(exception)` and the duplicate-subject
      branch untouched.
- [x] Task 4: Delete `CertificateValidator`'s three `Information` calls and its
      now-unused logger parameter, updating construction sites and tests.
- [x] Task 5: Cover no-credentials, invalid certificate, invalid bearer token,
      `[AllowAnonymous]`, and success — asserting the exact entry count each time.
- [x] Task 6: Build with no warnings, run the full unit suite, and flip this ADR to
      `accepted`.
