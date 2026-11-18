# MedFind — Database Schema (PostgreSQL 16 + PostGIS + EF Core)

Conventions: `snake_case` in DB, PascalCase in C# (Npgsql maps automatically).
All tables have `id` (uuid), `created_at`, `updated_at` (timestamptz).

---

## Entity-relationship (text)

```
users 1───1 pharmacies (owner)
pharmacies 1───n inventory_items
medicines 1───n inventory_items
pharmacies 1───* (geo: point)
```

---

## 1. users

| column | type | notes |
|--------|------|-------|
| id | uuid PK | |
| email | citext UNIQUE NOT NULL | case-insensitive |
| password_hash | text NOT NULL | BCrypt / ASP.NET Identity hash |
| full_name | text | |
| role | text NOT NULL | `Patient` \| `PharmacyAdmin` (enum, check constraint) |
| created_at / updated_at | timestamptz | |

Index: `unique(email)`.

---

## 2. pharmacies

| column | type | notes |
|--------|------|-------|
| id | uuid PK | |
| owner_user_id | uuid FK→users.id | unique — one admin owns one pharmacy in MVP |
| name | text NOT NULL | |
| address_line | text | |
| city | text | |
| phone | text | |
| location | **geography(Point, 4326)** NOT NULL | PostGIS point (lng, lat) |
| is_active | boolean NOT NULL default true | |
| created_at / updated_at | timestamptz | |

**Geo index (the important part):**
```sql
CREATE INDEX ix_pharmacies_location ON pharmacies USING GIST (location);
```
> Why: `ST_DWithin(location, point, meters)` uses this GiST index for a bounding-box prefilter then exact spherical distance. Without it, every search is a full scan.

---

## 3. medicines

| column | type | notes |
|--------|------|-------|
| id | uuid PK | |
| name | text NOT NULL | e.g. "Paracetamol 500mg" |
| generic_name | text | e.g. "Paracetamol" |
| brand | text | e.g. "Crocin" |
| form | text | tablet/syrup/etc. |
| created_at / updated_at | timestamptz | |

Index: `GIN (to_tsvector('english', name || ' ' || generic_name || ' ' || brand))` for search, OR a `trgm` index on `name` for fuzzy. Start with `ILIKE` + `pg_trgm` GIN; note it in docs.

---

## 4. inventory_items  (the heart)

| column | type | notes |
|--------|------|-------|
| id | uuid PK | |
| pharmacy_id | uuid FK→pharmacies.id | cascade delete |
| medicine_id | uuid FK→medicines.id | |
| quantity | integer NOT NULL default 0 | check >= 0 |
| verified_at | timestamptz | last admin confirmation |
| created_at / updated_at | timestamptz | |

Constraints/indexes:
```sql
UNIQUE (pharmacy_id, medicine_id);         -- one row per pharmacy+medicine
CREATE INDEX ix_inventory_verified ON inventory_items (verified_at);
CREATE INDEX ix_inventory_pharmacy ON inventory_items (pharmacy_id);
```

> `verified_at` is the product's identity — always surface age ("verified 12 min ago").

---

## EF Core notes (say these in interviews)

- Use **`DateTimeOffset`** (maps to `timestamptz`) — not `DateTime`.
- Enable PostGIS: in migration: `migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;")` before table creation, and add a `HasPostgresExtension("postgis")` in `OnModelCreating` or via `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` for `NetTopologySuite.Geometries.Point`.
- Use `[Column(TypeName = "geography (point)")]` on the `Point Location` property.
- **Migrations**, not `EnsureCreated()`. Commit migration files.
- Use `.AsNoTracking()` on all read queries (perf + memory).
- Keep queries in one place (query objects / repositories) so the geo SQL is reviewable.

### The critical query (memorize it)

```csharp
// dto: NearbyPharmacyDto
var point = new Point(lng, lat) { SRID = 4326 };

var query = db.Pharmacies
    .AsNoTracking()
    .Where(p => p.IsActive &&
                p.Location.IsWithinDistance(point, radiusMeters))
    .Select(p => new {
        p.Id, p.Name,
        DistanceMeters = p.Location.Distance(point),
        Available = p.Inventory
            .Where(i => i.Medicine.Name == medicineName &&
                        i.VerifiedAt >= DateTimeOffset.UtcNow.AddMinutes(-staleWindowMinutes) &&
                        i.Quantity > 0)
            .Select(i => new { i.Medicine.Name, i.Quantity, i.VerifiedAt })
            .ToList()
    })
    .Where(x => x.Available.Any())
    .OrderBy(x => x.DistanceMeters)
    .Take(pageSize);

// DTO materialization happens after — never return anonymous/entity types over the wire.
```

> NetTopologySuite's `IsWithinDistance`/`Distance` translate to `ST_DWithin`/`ST_Distance`. Verify with `.ToQueryString()` in tests/logging — **do this once and screenshot it** as proof it's real geo, not C#-side filtering.

---

## Seed data (make it believable)

- 30 pharmacies across your city with real-ish names ("Green Cross Pharmacy", "Apollo Pharmacy — Indiranagar") and coordinates spread over a few km.
- 60 medicines with realistic brands/generics.
- Inventory rows with `verified_at` within **last 30 minutes** so demos look live.
- Distribution: some pharmacies have 0 (stale), some have plenty.
