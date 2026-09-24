# CityYouth API

ASP.NET Core 10 Web API for the CCC Youth website (React frontend:
https://github.com/Biak18/ccc-youth).

Clean Architecture + MediatR (vertical slices) + EF Core (Npgsql) straight
against the Supabase Postgres database. Same style as DressShop.

## Secrets (3 only — no service-role key)

```powershell
cd D:\CityYouth\src\CityYouth.Api

dotnet user-secrets set "Supabase:Url" "https://<your-ref>.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "<anon public key>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<your-ref>;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Cloudinary:Name" "<cloud name>"
dotnet user-secrets set "Cloudinary:ApiKey" "<api key>"
dotnet user-secrets set "Cloudinary:ApiSecret" "<api secret>"
```

Use the **session-mode pooler** (port `5432`). No `Supabase:ServiceKey`:
auth uses the anon key and uploads forward each caller's own JWT, so
Supabase storage RLS (staff-only, owner = uploader) still applies.

## Run

```powershell
cd D:\CityYouth\src\CityYouth.Api
dotnet run
```

Open `http://localhost:5029/scalar`. `GET /api/health` needs no secrets;
`GET /api/health/db` checks the database connection.

## Database

The schema is managed in Supabase (tables already exist) — there are **no
EF migrations** in this repo. The EF model maps the existing tables/columns
1:1. `role`/`status` are `text` columns with CHECK constraints in the
database and rich `UserRole`/`ContentStatus` enums in C#, translated by EF
value converters — so the API speaks `"draft"`/`"admin"` strings while the
code stays type-safe.

## Auth model

- Identity = Supabase Auth. `POST /api/auth/login` exchanges email+password
  for tokens; every other call sends `Authorization: Bearer <access_token>`.
- Reads are public but status-aware: anonymous callers see `published` only;
  signed-in leaders additionally see `archived` + their own drafts; admins
  see everything (optional `?status=` filter).
- Writes need authentication. Leaders may create content and edit/delete
  **their own** (`created_by`); admins may edit/delete everything.
- `Admin` policy (role read from `profiles`, never from the client) gates:
  leaders CRUD, site settings, users/profiles.
- No public registration endpoint — users are created in Supabase Auth,
  then given a `profiles` row + role.

## Endpoints

| Area | Routes |
|---|---|
| Health | `GET /api/health`, `GET /api/health/db` |
| Auth | `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me` |
| Events | `GET /api/events?filter=upcoming\|past\|all&status&search&page&pageSize`, `GET /api/events/{id}`, `GET /api/events/slug/{slug}`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `DELETE /{id}` |
| Activities | `GET /api/activities?status&year&category&search&page&pageSize`, `GET /api/activities/{id}`, `GET /api/activities/slug/{slug}`, `GET /api/activities/{id}/media`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `DELETE /{id}` |
| Media | `GET /api/media?activityId&type&page&pageSize`, `GET /api/media/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Announcements | `GET /api/announcements?status&search&page&pageSize` (pinned first), `GET /api/announcements/{id}`, `GET /api/announcements/slug/{slug}`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `PATCH /{id}/pin`, `DELETE /{id}` |
| Leaders | `GET /api/leaders` (visible), `GET /api/leaders/all` (auth), `GET /api/leaders/{id}`, CRUD (`Admin`) |
| Settings | `GET /api/settings`, `PUT /api/settings` (`Admin`) |
| Categories | `GET /api/categories` (fixed list) |
| Uploads | `POST /api/uploads` multipart (`bucket`, `file`) → Cloudinary URL (auth, signed server-side) |
| Users | `GET /api/users?search&page&pageSize`, `PUT /api/users/{id}/role` (`Admin`) |

Status workflow is `draft → published → archived` with domain guards
(archived items can't be republished). Validation failures → `400`, missing
→ `404`, other people's content → `403`.

## Projects

```text
CityYouth.Api            controllers, Supabase JWT auth, Admin policy, middleware, CORS, rate limits
CityYouth.Application    features (MediatR), FluentValidation, abstractions, role/ownership checks
CityYouth.Domain         entities, ContentStatus/UserRole enums, publishing rules, exceptions
CityYouth.Infrastructure EF Core (AppDbContext, configs), Supabase auth client, storage (JWT-forwarded)
```
