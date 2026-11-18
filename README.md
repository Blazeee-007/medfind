# MedFind — Medicine Availability, Verified

A location-based platform that helps people find nearby pharmacies and check **recently verified**
medicine availability. Instead of a fake "in stock" flag, every result shows *when* the pharmacy last
confirmed stock — so availability is honest and time-bound.

Built to explore real backend problems: **geospatial search at scale, a sensible caching layer with
invalidation, and clean, tested ASP.NET Core.**

`C# · ASP.NET Core 8 · EF Core · PostgreSQL + PostGIS · Redis · React · Docker`

---

## Why this project (the interesting parts)

- **Real geo search** — PostGIS `geography(Point,4326)` + GiST index; radius search via `ST_DWithin`,
  not in-memory distance loops.
- **A cache with a strategy** — hot search + availability reads cached in Redis with short TTLs and
  explicit invalidation on stock updates. We deliberately chose TTL expiry over geo-bucket fan-out
  invalidation (documented trade-off).
- **Data freshness as a feature** — availability is always shown with a `verified_at` age; stale rows
  drop out of default search. Solves the "is this inventory real?" problem honestly.
- **Tested** — unit tests for service logic, integration tests (Testcontainers: Postgres+PostGIS, Redis).

## Architecture

```
 Browser (React)
     │  REST / JWT
     ▼
 ASP.NET Core API ──► NearbyService ──► Redis (nearby:*, availability:*)
     │                       └────────► EF Core ──► PostgreSQL + PostGIS (GiST)
     └─ auth (JWT) / role policies / rate limiting / health
```

Layers: `Api → Application → Infrastructure → Domain` (Domain has no dependencies).

## Quick start

```bash
cp .env.example .env        # fill JWT key etc.
docker compose up --build   # API + Postgres/PostGIS + Redis + web
# API:  http://localhost:8080/swagger
# Web:  http://localhost:5173
```
Seeds ~30 pharmacies, 60 medicines, believable inventory on first run.

## Performance (measured)

> Replace with your real k6 numbers before publishing.

| Endpoint | Without cache (P95) | With Redis (P95) | Notes |
|----------|---------------------|------------------|-------|
| `GET /api/pharmacies/nearby` | XXX ms | XXX ms | k6, N=50 concurrency, 30s |
| Cache hit rate (demo) | — | XX% | from /health/cache |

## API

See [`docs/API_SPEC.md`](docs/API_SPEC.md). Auth via `POST /api/auth/login` → Bearer token.

```bash
curl "http://localhost:8080/api/pharmacies/nearby?lat=12.97&lng=77.64&radiusKm=5&medicine=Paracetamol"
```

## Project layout
```
src/MedFind.Api            Controllers, middleware, DI
src/MedFind.Application    Services, DTOs, validators, interfaces
src/MedFind.Domain         Entities, enums
src/MedFind.Infrastructure EF Core, migrations, Redis
tests/                     Unit + Integration
web/                       React app
docs/                      PRD, schema, API, architecture, design, action plan
```

## Docs
- [Action Plan](docs/ACTION_PLAN.md) · [PRD](docs/PRD.md) · [DB Schema](docs/DATABASE_SCHEMA.md) · [API Spec](docs/API_SPEC.md) · [Architecture](docs/ARCHITECTURE.md) · [Design System](docs/DESIGN_SYSTEM.md) · [Design Preview](docs/design-preview.html) · [Vibe Coding Guide](docs/VIBE_CODING_GUIDE.md)

## Resume bullets (fill in real numbers)

- Built MedFind, a geo-based medicine-availability API (ASP.NET Core 8, EF Core, PostgreSQL/PostGIS, Redis) with JWT auth and role-based access for pharmacy admins.
- Implemented PostGIS `ST_DWithin` radius search with a GiST spatial index, cutting nearby-query time from **XXX ms → XXX ms**.
- Designed a Redis caching layer with TTL + write-through invalidation, reaching **XX% hit rate** and lowering `/nearby` P95 latency to **XXX ms** under load.
- Modeled inventory with a `verified_at` freshness window so availability is honest and time-bound; added unit + integration tests (Testcontainers) run in CI.

## License
MIT
