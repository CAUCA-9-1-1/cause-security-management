# ADR Records Overview

This index tracks Architecture Decision Records (ADRs) stored in this folder.

## How to Use

1. Add one markdown file per architectural decision under `docs/adr/records/`.
2. Use a clear, sortable naming convention: `YYYY-MM-DD-short-title.md`.
3. Update this overview with each new ADR, keeping its status column current.

## Records

| Title | Status | ADR Record |
|---|---|---|
| Multi-Scheme Authentication Routing For JWT And Certificate Flows | accepted | [2026-06-04-multi-scheme-authentication-routing.md](2026-06-04-multi-scheme-authentication-routing.md) |
| Default Authorization And Controller Conventions | accepted | [2026-06-04-default-authorization-and-controller-conventions.md](2026-06-04-default-authorization-and-controller-conventions.md) |
| Modular Packaging Boundaries Across Core Http And Wolverine | accepted | [2026-06-04-modular-packaging-boundaries.md](2026-06-04-modular-packaging-boundaries.md) |
| Extensible Security Service Registration Through Options | accepted | [2026-06-04-extensible-security-service-registration.md](2026-06-04-extensible-security-service-registration.md) |
| EF Core Security Model Integration Through Base Context And Mappings | accepted | [2026-06-04-ef-core-security-model-integration.md](2026-06-04-ef-core-security-model-integration.md) |
| Security Testing Strategy Across Unit And Integration Layers | accepted | [2026-06-04-security-testing-strategy.md](2026-06-04-security-testing-strategy.md) |
| Mobile Version Compatibility Policy Using Semantic Version Gates | accepted | [2026-06-04-mobile-version-compatibility-policy.md](2026-06-04-mobile-version-compatibility-policy.md) |
| Multi-Package Release Governance For SecurityManagement Libraries | accepted | [2026-06-04-multi-package-release-governance.md](2026-06-04-multi-package-release-governance.md) |

## Review Protocol

Before proposing or implementing any major architectural change:

1. Review all relevant ADR records in this folder.
2. Reference the specific ADR files consulted.
3. Explain alignment with existing decisions or justify intentional deviations.
