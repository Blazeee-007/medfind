# MedFind — Vibe Coding Guide (fast, but not generic or fragile)

Vibe coding = you drive, AI types. The risk: you ship code you can't explain and a UI that looks AI-made.
This guide keeps you fast **and** interview-ready.

---

## The golden rule

> **You must be able to explain and rewrite every file AI writes.**
> If you can't, delete it and re-ask with more context. Interviewers will ask "why did you do it this way?"

---

## Workflow (per feature)

1. **Write the contract first** (in `docs/`): endpoint + request/response JSON + acceptance criteria. (Already done in API_SPEC.)
2. **Ask AI for the smallest slice**: e.g. "implement `NearbyService` per API_SPEC.md, using the query in DATABASE_SCHEMA.md."
3. **Review line by line.** Run it. Write one test that proves it.
4. **Commit** with a message describing *why* ("feat(search): PostGIS ST_DWithin nearby query with GiST index").
5. **Refactor**: ask AI "what would a senior change here?" then decide yourself.

Never let AI generate 10 files at once. One slice → review → commit.

---

## Prompting rules for backend (C#/ASP.NET)

**Always paste context:** the relevant DTOs, the entity, and the exact doc section. AI without context produces generic slop.

**Bad prompt:** "Build a pharmacy search API."
**Good prompt:**
> In my ASP.NET Core 8 project (layers: Api/Application/Domain/Infrastructure), implement
> `NearbyService.GetNearbyAsync(NearbyQuery q)` that:
> - uses EF Core with `NetTopologySuite` Point (geography 4326),
> - filters with `IsWithinDistance(point, radiusMeters)` (ST_DWithin),
> - only includes inventory items where `VerifiedAt >= now - staleWindow` and `Quantity > 0`,
> - orders by `Distance(point)`, paginates,
> - returns `PagedResult<NearbyPharmacyDto>` (no entities).
> Also add a `.ToQueryString()` logging branch in Development so I can verify the SQL.
> Do NOT add caching here — that's a separate decorator.

**Force explainability:** end every backend prompt with:
> "Explain in 3 bullets why you chose this approach and one alternative you rejected."

**Force the SQL check:** always ask it to print the generated SQL for any EF query. Paste that SQL into your notes — it's interview gold ("here's the exact ST_DWithin query my app runs").

---

## Prompting rules for frontend (the anti-generic part)

**Never** prompt "make a modern landing page for a medicine app." That = AI template.

Instead:
> Build the MedFind search page as an **app layout** per `DESIGN_SYSTEM.md`. It's NOT a landing page.
> Left rail 240px (search inputs, radius chips, verified-within chips) + main result **ledger** (rows
> with hairline dividers, status dot, mono timestamps, stock-squares). Use Fraunces + IBM Plex Sans +
> IBM Plex Mono. Colors from the CSS variables exactly. No gradients, no shadows, 2px radius buttons,
> no indigo/blue/purple. Copy: calm and specific ("verified 12 min ago").
> Reuse the exact markup of `docs/design-preview.html` as the target look.

Pointing AI at your own preview file is the single best anti-generic trick.

---

## AI usage cheat sheet

| Task | Do this | AI should NOT do |
|------|---------|------------------|
| Scaffold solution | ask for folder structure only | generate all business logic |
| EF entity | give schema table, ask for class + config + migration | invent columns |
| Geo query | paste exact query from schema doc | do client-side distance filter |
| Redis caching | ask for a `CachedNearbyService` decorator | cache everything blindly |
| Auth | ask for JWT + BCrypt + role policy | roll custom crypto |
| Tests | ask for xUnit cases from acceptance criteria | write tests that assert nothing |
| UI | point at design-preview.html | "modern dashboard" wishy-washy |
| Docker | ask for compose with healthchecks | hardcode secrets |
| README | give it measured numbers, ask for concise doc | invent benchmark numbers |

---

## Review checklist before you commit AI code

- [ ] I understand every line (or rewrote it).
- [ ] No entity leaked in a response; DTOs only.
- [ ] `.AsNoTracking()` on reads.
- [ ] No magic secrets in code; config/env used.
- [ ] Errors return ProblemDetails, not stack traces.
- [ ] One test proves the slice works.
- [ ] I can explain the design choice out loud in 30 seconds.

---

## Repository hygiene (signals you're a real engineer)

- `.gitignore`: `bin/ obj/ node_modules/ .env *.user`
- `.env.example` committed, `.env` ignored.
- Conventional commits: `feat(search): ...`, `fix(api): ...`, `test: ...`, `chore: ...`
- `CLAUDE.md` (or `.cursorrules`) at root with project rules so every AI tool stays on-brand (see README pack).
- Tag `v1.0.0`.

---

## Interview prep (do this weekly, not at the end)

For each build slice, jot 3 sentences in `docs/INTERVIEW_NOTES.md`:
1. What I built and why.
2. The trade-off / alternative I rejected.
3. How I'd measure or test it.

By week 6 you'll have a talk-track for every part of the project — that's what turns "I used AI" into "I engineered this."
