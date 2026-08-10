# Roadmap

## Phase 0 — Scaffold (done)

- Solution + layered projects
- DI, Serilog, Swagger, exception middleware
- Health endpoint
- Email + Payment **interfaces** + scaffold implementations
- User entity + EF Core MySQL wiring

## Phase 1 — Auth + Users CRUD (done)

- Register / Login / Refresh / Forgot + Reset password / Logout
- JWT + refresh tokens (Infrastructure)
- `GET /api/v1/users/me`
- Admin user list / create / update / soft-delete
- Swagger Bearer auth + `EnsureCreated` on startup (dev)

## Phase 2 — Core CRUD

- Websites (MW)
- Deals (+ state-wise)
- Referrals

## Phase 3 — Payments (live Razorpay)

- Replace CreateOrder stub with Razorpay Orders API
- Verify + webhook + Invoice entity

## Phase 4 — Email product flows

- OTP, welcome, payment success templates
- Optional background queue

## Phase 5 — Franchisee / wallet / kit / admin

## Phase 6 — Hardening

- CORS lock, rate limits, tests, API versioning polish
