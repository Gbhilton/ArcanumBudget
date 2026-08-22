# Household Finance

Personal spending-analysis app: connects bank accounts via Plaid, breaks down
spending by category, links household members (e.g. you + your wife) so
their accounts show up on one shared dashboard, and generates savings
recommendations (recurring merchants, forgotten subscriptions).

**Stack — 100% free to build and run:**
- Backend: ASP.NET Core Web API (.NET 8), C#
- Database: SQL Server Express (free tier, up to 10GB)
- Auth: ASP.NET Identity + JWT
- Bank data: Plaid (free Sandbox now; free Trial plan for real accounts later)
- Frontend (not yet scaffolded): Ionic + Angular — one codebase for web + mobile

## Project layout

```
ArcanumBudget.sln
src/ArcanumBudget.Api/
├── Controllers/       # Auth, Plaid, Household, Dashboard, Recommendations
├── Models/            # EF Core entities (User, Household, PlaidItem, Transaction, ...)
├── Data/              # AppDbContext
├── Services/          # PlaidService, SyncService, HouseholdService, RecommendationEngine
├── Program.cs
└── appsettings.json
```

## Setup

### 1. Install prerequisites (all free)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [SSMS](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (optional, for browsing the DB)

### 2. Get free Plaid credentials
1. Sign up at https://dashboard.plaid.com/signup — free.
2. Grab your `client_id` and `Sandbox secret` from the dashboard.
3. Sandbox is free forever with fake bank data. Later, request a **Trial plan**
   (free, supports up to 10 real connections, covers Bank of America) to test
   against your actual accounts before ever paying anything.

### 3. Configure secrets (don't put these in appsettings.json)
From `src/ArcanumBudget.Api/`, use .NET's user-secrets (keeps them out of git entirely):

```bash
dotnet user-secrets init
dotnet user-secrets set "Plaid:ClientId" "your-client-id"
dotnet user-secrets set "Plaid:Secret" "your-sandbox-secret"
dotnet user-secrets set "Jwt:Key" "a-long-random-string-at-least-32-characters"
```

(Alternatively copy `appsettings.Development.json.example` to
`appsettings.Development.json` and fill it in — that file is gitignored.)

### 4. Create the database
```bash
cd src/ArcanumBudget.Api
dotnet tool install --global dotnet-ef   # one-time
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Run it
```bash
dotnet run
```
Swagger UI opens at `https://localhost:5001/swagger` — good for testing
endpoints before the frontend exists.

## How the pieces fit together

- **Sign up / log in** → `AuthController` (ASP.NET Identity + JWT).
- **Connect a bank** → frontend calls `POST /api/plaid/link-token`, opens
  Plaid Link with that token, then sends the resulting `public_token` to
  `POST /api/plaid/exchange`. We store an **encrypted** access token
  (`IDataProtector`) and immediately run a first sync.
- **Household linking** → `POST /api/household/invite` with your wife's
  email creates a household (if you don't have one) and a `Pending` member
  row, and logs a verification link (dev stand-in for a real email). She
  hits `POST /api/household/verify` with that token from her own logged-in
  session — only then does her data become visible to you.
- **Dashboard** → `GET /api/dashboard/spend-by-category` looks up your
  household's user IDs (falls back to just you if unlinked), pulls all their
  transactions, and returns category totals for a pie chart.
- **Recommendations** → `POST /api/recommendations/generate` (optionally
  scoped to a category, e.g. "Food") runs the recommendation engine's
  modules — recurring-merchant detection, forgotten-subscription detection —
  and saves + returns results. `GET /api/recommendations` lists active ones.

## Known simplifications to revisit

- `RecommendationEngine`'s subscription detector only looks at 30 days of
  data — it'll get much more accurate once you have a few months of
  transaction history to spot true monthly recurrence.
- Email sending uses real SMTP (`SmtpEmailService`, via MailKit) once
  `Smtp:Host` is configured in `appsettings.Development.json` — see
  `appsettings.Development.json.example` for the Gmail App Password setup.
  Falls back to a console-log stub (`ConsoleEmailService`) when unconfigured,
  so local dev works without real credentials.
- No background job yet for nightly auto-sync — right now sync is triggered
  by the initial connect and a manual "refresh" button. Add a hosted service
  or cron trigger calling `ISyncService.SyncAllForUserAsync` if you want it
  automatic.

## Next steps
- Scaffold the Ionic + Angular frontend (separate project, calls this API).
- Add EF Core migration + first run-through in Sandbox with Plaid's test
  institutions (`user_good` / `pass_good` credentials work in Sandbox).
- Once it works end-to-end in Sandbox, flip `Plaid:Environment` to your Trial
  plan credentials to test against real Bank of America accounts.
