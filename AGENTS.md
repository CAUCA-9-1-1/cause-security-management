# AGENTS

## Purpose

This file defines baseline instructions for human and AI contributors working in this repository.

## Architecture Decision Records (ADR)

Before proposing major architectural changes, follow the review protocol in
[docs/adr/records/overview.md](docs/adr/records/overview.md).

When authoring a new ADR:

- Follow the ADR template in `docs/adr/template.md`.
- Use the naming convention `YYYY-MM-DD-short-title.md`.
- Update `docs/adr/records/overview.md` and add a table row with the ADR title, status, and link.

Always create an ADR when changes are made to the codebase that affect the overall architecture.

## Change Quality Requirements

- Keep changes scoped and aligned with existing package boundaries.
- Add or update tests for behavioral changes, especially authentication and authorization flows.
- Prefer secure defaults and document any intentional deviations from current architecture decisions.

## Pull Request Checklist

- Confirm whether architecture-impacting changes were introduced.
- If yes, include a new ADR and index update.
- Reference relevant ADR files in the PR description.
- No warnings.
- No errors.
- All unit and integrations tests are green.
