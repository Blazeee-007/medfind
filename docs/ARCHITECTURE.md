# MedFind — Architecture, Caching & Testing

---

## 1. Layered solution structure

```
MedFind.sln
├─ src/
│  ├─ MedFind.Api/            # Controllers, middleware, DI wiring, Program.cs
│  ├─ MedFind.Application/    # Services / use-cases, DTOs, validators, interfaces
│  ├─ MedFind.Domain/         # Entities, enums, value objects, no external deps
│  └─ MedFind.Infrastructure/ # EF Core DbContext, migrations, Redis, repositories
├─ tests/
│  ├─ MedFind.UnitTests/
│  └─ MedFind.IntegrationTests/
├─ web/                       # React app (Vite)
├─ docker-compose.yml
├─ .github/workflows/ci.yml
└─ docs/
```

**Dependency rule:** Domain ← Application ← Infrastructure ← Api. Domain references nothing.
Api references Application; Infrastructure implements Application interfaces (DI in Program.cs).

### Why layers (interview answer)
- Testable use-cases without HTTP.
- Swap Redis/DB without touching controllers.
- Clear ownership: controllers are thin (map → call service → return DTO).

---

## 2. Request flow (nearby search)

```
Controller → NearbyService.GetNearbyAsync(query)
   → build cache key
   → try Redis GET  ──(hit)──► return cached DTO, set X-Cache: HIT
   → (miss) → EF Core query (PostGIS ST_DWithin) → map to DTO
   → Redis SET key ttl=60s
   → set X-Cache: MISS → return
```

---

## 3. Redis caching strategy (READ THIS — biggest interview point)

### Keys & TTL
| key | value | ttl | rationale |
|-----|-------|-----|-----------|
| `nearby:{lat2}:{lng2}:{radius}:{medicine?}:p{page}` | JSON results | 60s | Search results tolerate short staleness; coordinates rounded to 2 decimals (~1km bucket) to raise hit rate |
| `availability:{pharmacyId}` | JSON inventory | 30s | Stock changes matter more; shorter TTL |
| `pharmacyVersion:{pharmacyId}` | int | none | bumped on writes, used to version keys |

### Invalidation
- On `PUT /pharmacy/inventory/{id}`: `DEL availability:{id}` **immediately** (write-through delete).
  Also `INCR pharmacyVersion:{id}`.
- `nearby` keys are **not** aggressively deleted (fan-out is hard); they expire in ≤60s.
  **Say this out loud in interviews:** "I chose a short TTL for search results instead of trying to
  invalidate every geo bucket, because the fan-out invalidation is O(pharmacies × buckets) and
  correctness within 60s is acceptable for availability search." → This is a *deliberate trade-off*,
  which is exactly what seniors look for.

### What I deliberately did NOT cache (and why)
- `GET /pharmacies/{id}` — cheap, low traffic, no benefit.
- Admin inventory — must be fresh for the admin editing it; caching would confuse them.
- Auth tokens — stateless JWT, no server cache.

### Cache stampede guard (mention, optionally implement)
On miss, use `SET key value NX PX` lock or `Lazy<T>` per-key to avoid thundering herd. Even describing it is a plus.

### Metrics
- Counters `hits`/`misses` in Redis `INCR`, surfaced at `/health/cache`. Put hit rate in README.

---

## 4. Auth architecture

- Stateless JWT (HS256, signing key from config/env).
- Password: BCrypt (`BCrypt.Net-Next`) — say why not plaintext/MD5.
- `[Authorize(Roles = "PharmacyAdmin")]` on admin controllers; `pharmacyId` claim scopes queries to owner's pharmacy.
- Always filter by `owner_user_id == currentUserId` (IDOR prevention — call this out, it impresses).

---

## 5. Error handling

- Global middleware catches exceptions → `ProblemDetails`.
- FluentValidation auto-returns 400 with field errors.
- Never leak stack traces or EF entities.

---

## 6. Observability

- Serilog → console (structured). Request logging: method, path, status, elapsed, cache hit/miss.
- `/health` via `AddHealthChecks().AddNpgSql().AddRedis()`.

---

## 7. Testing approach

**Unit (xUnit + FluentAssertions, no DB):**
- Cache-key builder produces stable/rounded keys.
- Staleness filter drops rows older than window.
- Auth service: wrong password rejected, role claim correct.

**Integration (WebApplicationFactory + Testcontainers):**
- Register → login → call protected endpoint (200) and anonymous (401).
- Seed 2 pharmacies → nearby returns only in-stock + recent one, sorted by distance.
- Stock update → availability endpoint reflects new quantity immediately (invalidation works).

> Integration tests with Testcontainers (Postgres+PostGIS + Redis) are a strong resume signal — few freshers do this.

---

## 8. CI (GitHub Actions)

```yaml
name: ci
on: [push, pull_request]
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release
      - run: dotnet format --verify-no-changes
```

---

## 9. Docker compose (one command)

Services: `postgis` (Postgres 16 + PostGIS), `redis`, `api` (depends_on healthy), `web` (Vite preview).
Add `healthcheck` to postgres/redis; api waits for them.

---

## 10. Config via environment

`ConnectionStrings__Default`, `Redis__Connection`, `Jwt__Key`, `Jwt__Issuer`, `MEDFIND_STALE_AFTER_MINUTES`.
Never commit secrets — use `.env` (gitignored) + `.env.example`.

---

## 11. Diagram (ASCII for README)

```
 Browser (React)
     │  HTTPS
     ▼
 ASP.NET Core API  ──►  NearbyService ──► Redis (nearby:*)
     │                        └────────► EF Core ──► PostgreSQL + PostGIS (GiST)
     └── JWT auth / policies
```
