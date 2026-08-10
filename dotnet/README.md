# MiniWebsite.Api — Phase 1 (Auth + Users)

ASP.NET Core **.NET 9** Web API for MiniWebsite. Frontend will call these APIs only.

## Solution layout

```
dotnet/
├── MiniWebsite.sln
├── src/
│   ├── MiniWebsite.Api/              Controllers, middleware, Program.cs
│   ├── MiniWebsite.Application/      Use-cases, DTOs, interfaces, FluentValidation
│   ├── MiniWebsite.Domain/           Entities, enums
│   ├── MiniWebsite.Infrastructure/   EF Core (MySQL), SMTP, Razorpay, JWT
│   └── MiniWebsite.Shared/           Shared constants
├── tests/
│   └── MiniWebsite.UnitTests/
└── docs/
    ├── API-CONVENTIONS.md
    └── ROADMAP.md
```

## Run (local)

1. Start XAMPP MySQL.
2. Ensure DB exists (API also runs `EnsureCreated` on startup):

```bash
mysql -u root -e "CREATE DATABASE IF NOT EXISTS miniwebsite_api;"
```

3. Run the API:

```bash
cd C:\xampp\htdocs\miniwebsite\dotnet
dotnet run --project src/MiniWebsite.Api --urls http://localhost:5088
```

Swagger (Development): `/swagger`  
Health: `GET /api/v1/health`

## Phase 1 endpoints

| Method | Route | Auth |
|--------|--------|------|
| POST | `/api/v1/auth/register` | — |
| POST | `/api/v1/auth/login` | — (`userId` = email or phone) |
| POST | `/api/v1/auth/refresh` | — |
| POST | `/api/v1/auth/forgot-password` | — |
| POST | `/api/v1/auth/reset-password` | — |
| POST | `/api/v1/auth/logout` | Bearer |
| GET | `/api/v1/users/me` | Bearer |
| GET/POST/PUT/DELETE | `/api/v1/users`… | Bearer + Admin |

## Configure secrets (do not commit real values)

Edit `src/MiniWebsite.Api/appsettings.Development.json` or use user-secrets:

```bash
cd src/MiniWebsite.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=miniwebsite_api;User=root;Password=YOUR_PASS"
dotnet user-secrets set "Jwt:Key" "your-long-random-secret-at-least-32-chars"
dotnet user-secrets set "Razorpay:KeyId" "rzp_test_..."
dotnet user-secrets set "Razorpay:KeySecret" "..."
dotnet user-secrets set "Smtp:Host" "smtp.example.com"
```

## Stack

| Item | Choice |
|------|--------|
| Runtime | .NET 9 |
| DB | MySQL `miniwebsite_api` (Pomelo) |
| Auth | JWT + refresh tokens (ASP.NET password hasher) |
| Email | MailKit SMTP (skipped if Host empty) |
| Payments | Razorpay stub until Phase 3 |

## Next

See `docs/ROADMAP.md` — Phase 2 = Websites / Deals / Referrals CRUD.
