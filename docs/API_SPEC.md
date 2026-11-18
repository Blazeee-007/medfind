# MedFind — API Specification (REST, ASP.NET Core)

Base URL: `/api` · Auth: `Authorization: Bearer <JWT>` · Content-Type: `application/json`
Errors: RFC 7807 `application/problem+json` (400 validation, 401 unauth, 403 forbidden, 404 not found, 429 rate-limited).

Prefer **controllers** here — interviewers often type controller+service patterns, and explicit code is easy to defend.

---

## 1. Auth

### POST /api/auth/register
```json
{ "email": "a@b.com", "password": "Str0ng!pw", "fullName": "A B", "role": "Patient", "pharmacyCode": null }
```
- `role`: `Patient` (default) or `PharmacyAdmin` (requires a valid `pharmacyCode` → else `403`).
- `201 Created` → `{ "id": "...", "email": "...", "role": "Patient" }`
- `409` if email exists · `400` validation failed.

### POST /api/auth/login
```json
{ "email": "a@b.com", "password": "Str0ng!pw" }
```
→ `200 { "accessToken": "<jwt>", "expiresAt": "...", "role": "Patient" }`
JWT claims: `sub` (userId), `role`, `pharmacyId` (only for PharmacyAdmin), `exp`.

---

## 2. Pharmacy search (public)

### GET /api/pharmacies/nearby
Query params:
| param | type | default | notes |
|-------|------|---------|-------|
| lat | double | required | latitude |
| lng | double | required | longitude |
| radiusKm | double | 5 | max 25 |
| medicine | string | null | filter to pharmacies carrying this medicine |
| page | int | 1 | |
| pageSize | int | 20 | max 50 |

`200 OK`:
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Green Cross Pharmacy",
      "addressLine": "12 MG Road",
      "city": "Bengaluru",
      "distanceMeters": 820,
      "availability": [
        { "medicineId": "guid", "name": "Paracetamol 500mg", "quantity": 6, "verifiedAt": "2026-11-18T18:50:00Z", "verifiedAgeMinutes": 12 }
      ]
    }
  ],
  "page": 1, "pageSize": 20, "totalCount": 37
}
```
- Only pharmacies with ≥1 item `verifiedAt` within the stale window (default 60 min) and `quantity > 0`.
- Sorted ascending by distance.
- **Cached** in Redis (see `ARCHITECTURE.md`). `X-Cache: HIT|MISS` response header for demos.

### GET /api/pharmacies/{id}
`200 OK` → pharmacy detail (no cache) · `404` if not found.

### GET /api/pharmacies/{id}/availability
`200 OK` → full inventory list for that pharmacy with `quantity` + `verifiedAt` + `verifiedAgeMinutes`.
- **Cached** in Redis (`availability:{id}`, TTL 30s). Invalidated on any stock update.

---

## 3. Medicine search (public, helper)

### GET /api/medicines?q=paracet
`200 OK` → `[ { "id": "...", "name": "Paracetamol 500mg", "genericName": "Paracetamol", "brand": "Crocin" } ]`
- Autocomplete for the search box. Uses `pg_trgm` ILIKE / trigram index.

---

## 4. Pharmacy admin (protected: role=PharmacyAdmin)

### GET /api/pharmacy/inventory
Returns admin's own inventory, including stale items (with a `needsUpdate: true` flag when older than window).
`200 OK` → `[ { "medicineId", "name", "quantity", "verifiedAt", "needsUpdate" } ]`

### PUT /api/pharmacy/inventory/{medicineId}
```json
{ "quantity": 24 }
```
- Sets `quantity`, `verified_at = now()`.
- **Side effect:** invalidates Redis `availability:{pharmacyId}` immediately.
- `200 OK` → updated item · `404` if medicine not in this pharmacy's inventory (admin must add it first).
- `400` if quantity < 0.

### POST /api/pharmacy/inventory
```json
{ "medicineId": "guid", "quantity": 0 }
```
- Adds a medicine to the pharmacy's inventory (quantity starts at 0 / verifiedAt null).
- `201 Created` · `409` if already present.

---

## 5. Ops

### GET /health
`200 OK` → `{ "status": "Healthy", "checks": { "postgres": "Healthy", "redis": "Healthy" } }`

### GET /health/cache
`200 OK` → `{ "hits": 1024, "misses": 210, "hitRate": 0.83 }` (in-memory counters, for demo).

---

## Validation rules (FluentValidation)

- Email format, password min 8 with a number + symbol.
- `lat` in [-90, 90], `lng` in [-180, 180], `radiusKm` in (0, 25], `pageSize` in (0, 50].
- `quantity` ≥ 0.

## Auth/authorization summary

| Endpoint group | Access |
|----------------|--------|
| auth, nearby, medicines, pharmacy/{id}/availability, health | Anonymous (nearby rate-limited) |
| /pharmacy/inventory* | `PharmacyAdmin` policy |
| /auth/register role=PharmacyAdmin | requires pharmacyCode |

## Deliverable
Commit an `api.http` (VS Code REST Client) file covering the happy path + one error per endpoint.
This doubles as your manual test harness and your demo script.
