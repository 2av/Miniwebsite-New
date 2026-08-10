# MiniWebsite Admin (React)

React admin front-end for MiniWebsite. Talks to the **.NET API** only (no PHP SQL on this app).

## Stack

- Vite + React 19 + TypeScript
- React Router, TanStack Query, Axios, Tailwind CSS v4
- Feature-based folders under `src/features`

## Run locally

1. Start .NET API on `http://localhost:5209` (Visual Studio / `dotnet run`)
2. In this folder:

```bash
npm install
npm run dev
```

Open `http://localhost:5173` → **Manage Users**.

Dev uses Vite proxy (`/api` → `localhost:5209`) when `VITE_API_BASE_URL` is empty (see `.env.development`).

## Production

`.env.production` points to `https://api.miniwebsite.in`.

```bash
npm run build
```

Output: `dist/`

## Architecture

```
src/
  app/                 # router providers
  layouts/             # AdminLayout shell
  shared/              # api client, types, UI primitives
  features/
    dashboard/
    manage-users/      # fully wired to /api/v1/admin/manage-users
    auth/              # login shell (JWT later)
```

## Notes

- Admin auth is temporary/open on the API (same as current .NET AllowAnonymous).
- PHP `admin/` pages are unchanged / restored and remain the legacy UI until fully migrated.
