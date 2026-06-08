# Cause.SecurityManagement.Wolverine

> **Not currently published.** This package is incomplete, unfinished, and not
> yet validated for production use. It is built and tested as part of the
> solution but is **excluded from the release/publishing process** until it is
> complete. See `docs/RELEASING.md` and the
> `2026-06-04-exclude-wolverine-from-published-release-set.md` ADR.

Wolverine integration for the **Cause.SecurityManagement** platform: HTTP
endpoints and sagas as an alternative to the MVC-based `Cause.SecurityManagement`
package, for APIs built on [Wolverine](https://wolverine.netlify.app/).

It depends on `Cause.SecurityManagement.Core` and `WolverineFx.Http`. You do
**not** need `Cause.SecurityManagement` (the HTTP/MVC package).

See the repository README section *Cause.SecurityManagement.Wolverine* for the
intended setup, the provided endpoints, and the saga catalogue.
