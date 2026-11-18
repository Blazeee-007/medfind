# MedFind — Product Requirements Document (PRD)

Version key: **MVP-1** = Tier 1 (must build) · **MVP-2** = Tier 2 (build for polish) · **Later** = skip

---

## 1. Problem

People looking for a specific medicine don't know which nearby pharmacy has it in stock right now.
Calling around wastes time. Pharmacy inventory is invisible to customers.

## 2. Solution (scope for this project)

A web platform where:
- **Patients** search for a medicine by location and see **nearby pharmacies with recently verified stock**.
- **Pharmacy admins** update their stock, which stamps a `verifiedAt` time — so availability is **honest** (time-bound), not a fake yes/no.

> **Core honesty principle:** We never say "in stock." We say **"Verified 12 minutes ago: 6 units."**
> This is the product's identity and your main interview talking point about data freshness.

## 3. Personas & roles

| Role | Can do |
|------|--------|
| **Guest** | Search nearby availability (read-only, rate-limited) |
| **Patient (User)** | Register/login, save searches, (MVP-2: favorite pharmacies) |
| **Pharmacy Admin** | Login, view own inventory, update stock quantity per medicine |
| (System) | Expires/ages availability; caches; health-checks |

## 4. User stories (MVP-1)

**Search (Patient/Guest)**
- As a user, I can enter my location (or geolocation) + a medicine name, and get a list of pharmacies within a radius sorted by distance, each showing the quantity and **how long ago it was verified**.
  - *Acceptance:* results only include pharmacies with `verifiedAt` within last N minutes (default 60); sorted ascending by distance; paginated.

**Auth**
- As a pharmacy admin, I can register with my pharmacy code and log in.
- As a patient, I can register and log in.
  - *Acceptance:* JWT issued; protected endpoints reject anonymous; role enforced.

**Inventory update (Admin)**
- As a pharmacy admin, I can set the quantity for a medicine in my inventory.
  - *Acceptance:* update sets `verifiedAt = now()`; cached availability for that pharmacy is invalidated immediately.

**Availability detail**
- As a user, I can tap a pharmacy to see all medicines it currently reports, with verified times.
  - *Acceptance:* served from cache when warm; reflects invalidation within the TTL.

## 5. Non-functional requirements (this is what interviews probe)

| Concern | Requirement |
|---------|-------------|
| **Latency** | `/nearby` P95 < 150 ms warm (target; measured with k6) |
| **Caching** | Hot reads cached in Redis with TTL + explicit invalidation on writes |
| **Data freshness** | Results filtered by `verifiedAt` recency window (configurable) |
| **Geo correctness** | Radius search via PostGIS `ST_DWithin` on `geography` (meters, spherical) |
| **Security** | JWT auth, role policies, hashed passwords, input validation, no entity leakage |
| **Observability** | Serilog logs, `/health`, `/health/cache` |
| **Portability** | Runs via `docker compose up` only |

## 6. Out of scope (deliberately — say this in interviews)

Real pharmacy integrations, payments/prescriptions, real-time push, mobile app, multilingual admin,
full build of user-saved lists. Listing these shows scoping maturity.

## 7. Data-freshness model (the nuance that saves you)

- Every stock row carries `quantity` and `verified_at`.
- "Availability" is **always** shown with age: `verified_at` relative to now.
- A configurable **staleness window** (`MEDFIND_STALE_AFTER_MINUTES`, default 60) decides what counts as "recent".
- Stale rows are **hidden from default search** but remain queryable (admin sees them as "needs update").
- This kills the "your data is fake" objection: the product is explicitly honest about time.

## 8. Success criteria for the portfolio

- End-to-end demo in < 90 seconds.
- README with a real perf table (before/after cache).
- You can whiteboard the schema, the cache flow, and the geo query from memory.
