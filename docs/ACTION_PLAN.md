# MedFind — Action Plan (SDE-1 C# Backend Portfolio Project)

> Goal: A clean, defensible, **60–70% complete** medicine-availability platform that proves you can
> design an API, model geo data, cache with a strategy, and ship something that runs in one command.
> **Depth beats breadth.** Stop adding features the moment you can't defend every line.

---

## Guiding principles

1. **Tier 1 is the floor.** Finish every Tier 1 item before touching Tier 2.
2. **Every feature = a talking point.** If you can't explain *why* a design choice exists, don't merge it.
3. **Measure, don't claim.** Add numbers (latency, cache hit rate, test count). Put them in the README.
4. **Vibe-code with guardrails.** Use AI to go fast, but review every file and be able to rewrite it by hand.
5. **Commit daily.** Small, descriptive commits = your activity graph looks alive and your Git history proves ownership.

---

## Timeline overview

Assumes ~2–3 hrs/day, 5–6 days/week. Total ≈ **5–6 weeks**.

| Week | Focus | Tier | Output |
|------|-------|------|--------|
| 1 | Scaffolding + data model + migrations | 1 | Repo runs `docker compose up`, DB seeded |
| 2 | Auth + core search (geo) + read API | 1 | User can register/login/search nearby |
| 3 | Write flows + Redis caching + invalidation | 1 | Pharmacy updates stock; cache strategy works |
| 4 | Tests + CI + logging + polish + README numbers | 2 | Green CI, measured latency, clean README |
| 5 | Pharmacy-admin UI + demo data + screenshots | 2 | Believability (real-looking data) |
| 6 | Deployment *(optional)* + interview prep (explain every decision) | 3 | Live link or strong talk-track |

---

## WEEK 1 — Foundation (Tier 1)

- [ ] Create solution: `MedFind.Api`, `MedFind.Application`, `MedFind.Domain`, `MedFind.Infrastructure`
- [ ] Add `docker-compose.yml`: `postgis/postgis:16`, `redis:7`, API, web
- [ ] Configure EF Core + Npgsql, enable PostGIS extension in migration
- [ ] Create initial schema (see `DATABASE_SCHEMA.md`)
- [ ] Write `DbSeeder` with ~30 pharmacies, ~60 medicines, stock rows (real-looking names, not "Test Pharmacy 1")
- [ ] Run first migration → verify tables + GiST index exist
- [ ] `dotnet run` locally + confirm `/health` returns 200

**Definition of done:** Fresh clone → `docker compose up` → API healthy + data seeded.

---

## WEEK 2 — Auth + Geo Search (Tier 1)

- [ ] JWT auth: register/login, password hashing (ASP.NET Identity or BCrypt + custom), roles `User` / `PharmacyAdmin`
- [ ] Policy-based authorization; protect admin endpoints
- [ ] `GET /api/pharmacies/nearby?lat=&lng=&radiusKm=` using PostGIS `ST_DWithin`
- [ ] `GET /api/pharmacies/{id}/availability` returns medicine + stock + `verifiedAt`
- [ ] DTOs for all responses (never expose EF entities directly)
- [ ] FluentValidation on request models
- [ ] Global exception-handling middleware → consistent `ProblemDetails`
- [ ] Pagination on list endpoints (`page`, `pageSize`, total count)

**Definition of done:** Postman/HTTP file shows full flow: register → login → search → view availability.

---

## WEEK 3 — Write Flows + Caching (Tier 1, the differentiator)

- [ ] `PATCH /api/pharmacy/stock/{medicineId}` — admin updates quantity → sets `verifiedAt = now`
- [ ] Cache hot reads in Redis:
  - Key: `nearby:{roundedLat}:{roundedLng}:{radius}` → TTL 60s
  - Key: `availability:{pharmacyId}` → TTL 30s
- [ ] **Invalidation:** on stock update, delete `availability:{pharmacyId}` AND bump a `pharmacyVersion` key
      so related `nearby` queries can be busted if needed.
- [ ] Add simple cache metrics: log hit/miss; expose counters in `/health/cache`
- [ ] Write `docs/CACHING_STRATEGY.md` explaining TTL choice, invalidation, and what you *chose not to cache*

**Definition of done:** You can demonstrate (log output) a cache HIT vs MISS, and show stock update invalidating it.

---

## WEEK 4 — Tests, CI, Observability, README (Tier 2)

- [ ] xUnit: 8–12 unit tests (services, geo query building, cache-key builder)
- [ ] 2 integration tests with `WebApplicationFactory` (auth flow + search endpoint against test container)
- [ ] Serilog structured logging; request logging middleware
- [ ] `GET /health` (DB + Redis) and `/health/cache`
- [ ] GitHub Actions: `dotnet build` + `dotnet test` + `dotnet format --verify-no-changes`
- [ ] Run a **k6** (or `bombardier`) load test on `/nearby`, record P50/P95 before & after Redis → **put numbers in README**

**Definition of done:** Green CI badge. README has a "Performance" table with real numbers.

---

## WEEK 5 — Admin UI + Believability (Tier 2)

- [ ] React pharmacy-admin page: login → list my medicines → edit stock
- [ ] User-facing search page (map or list) — clean, uses the DESIGN_SYSTEM, not default Tailwind/Shadcn look
- [ ] Seed a believable dataset (names, addresses, timestamps within last 30 min)
- [ ] Record a 60–90s demo GIF for README
- [ ] Add `docs/DEMO_SCRIPT.md`

**Definition of done:** Anyone watching the GIF believes it's a real product.

---

## WEEK 6 — Polish + Interview prep (Tier 3, optional)

- [ ] Deploy to Render/Azure (optional; not required for fresher, but a live link is nice)
- [ ] Write `docs/INTERVIEW_NOTES.md`: for each design choice, a 3-sentence answer
- [ ] Do a mock interview with yourself: explain caching, indexing, auth, EF migrations out loud
- [ ] Tag a `v1.0.0` release
- [ ] Update resume with measured bullets (see `README.md`)

---

## "Done enough to apply" checklist

Tick every box before you call it ready:

- [ ] `docker compose up` works from a clean clone
- [ ] Full auth → search → admin-update → cache-invalidate flow works
- [ ] PostGIS geo query is real (GiST index present)
- [ ] Redis cache has a documented strategy + invalidation
- [ ] ≥10 tests pass in CI
- [ ] README has architecture diagram + performance numbers + demo GIF
- [ ] You can explain every one of the above without looking anything up

> **You do NOT need:** real pharmacies, real users, payments, mobile app, real-time websockets,
> or production deployment. Those are Tier 3+ and add interview risk without adding resume signal.
