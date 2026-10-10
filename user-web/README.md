# user-web

Public + logged-in user React app for MiniWebsite (home, login, dashboard, portal).

## Rules

- Follow [`docs/ui-design-knowledge-base.md`](docs/ui-design-knowledge-base.md) for marketing pages and the logged-in portal
- Do not apply that system to customer Digital Business Profile themes in `public/theme`
- Tailwind for styling. Font is Poppins. Primary actions use the purple–pink–orange gradient
- Shared .NET API (`VITE_API_BASE_URL`)
- Static assets live under `public/assets` and `public/theme` (`VITE_ASSETS_BASE_URL=/assets`)
- Legacy PHP deep links: `VITE_SITE_URL`
- Uploaded media (profiles, etc.): `VITE_MEDIA_BASE_URL`

## Dev

```bash
cd user-web
npm install
npm run dev
```

Runs on **http://localhost:5174** (admin-web uses 5173).

Static CSS/images are served from this app’s `public/` folder — no local PHP `assets/` required.

## Build

```bash
npm run build
```

Output: `user-web/dist/`

## Phase status

See [`../USER_WEB_MIGRATION_CHECKLIST.md`](../USER_WEB_MIGRATION_CHECKLIST.md).
