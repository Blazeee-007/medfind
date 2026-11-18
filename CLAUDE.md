# CLAUDE.md — Project rules for AI coding assistants

These rules apply to every AI-assisted change in this repo. Read them before generating code.

## Project
MedFind — geo-based medicine availability API. Stack: ASP.NET Core 8, EF Core, PostgreSQL + PostGIS,
Redis, React (Vite), Docker. Layered: Api → Application → Infrastructure → Domain (Domain has no deps).

## Backend rules
- DTOs only over the wire. Never return EF entities.
- `.AsNoTracking()` on all read queries.
- Use `DateTimeOffset` for time (maps to `timestamptz`), never `DateTime`.
- Geo queries MUST use NetTopologySuite `IsWithinDistance`/`Distance` (→ `ST_DWithin`/`ST_Distance`).
  Never filter distance in C#/memory.
- EF Core **migrations** only — never `EnsureCreated()`.
- Services in `Application`; controllers thin (map → service → DTO).
- Validation via FluentValidation. Errors via RFC7807 ProblemDetails middleware.
- Secrets from config/env only. Never hardcode keys.
- Caching lives in a decorator (`CachedNearbyService`), not inside business services.
- For any new endpoint: add a happy-path + one-error integration test.

## Frontend rules (see docs/DESIGN_SYSTEM.md)
- It is an **app/tool layout**, not a marketing landing page.
- Fonts: Fraunces (display), IBM Plex Sans (UI), IBM Plex Mono (data/timestamps).
- Colors only from CSS variables in DESIGN_SYSTEM.md (warm paper + ink + one pharmacy green).
- FORBIDDEN: indigo/blue/violet/purple, colored gradients, pill buttons, floating cards with big shadows,
  emoji in UI, "Fast/Secure/Reliable" feature grids. Use hairline borders and 2px radius.
- Copy is calm and specific ("verified 12 min ago"), no exclamation marks.
- Match the look of `docs/design-preview.html`.

## Explainability
- After any non-trivial change, state in 3 bullets: what, why this approach, one rejected alternative.
- Print the generated SQL for any EF query you add (dev logging branch).

## Commit style
Conventional commits: `feat(scope):`, `fix(scope):`, `test:`, `chore:`, `docs:`.
