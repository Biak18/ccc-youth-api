# CityYouth API

ASP.NET Core 10 Web API for the Christian City Church Youth website.
Clean Architecture + MediatR (vertical slices), talking to Supabase over
HTTPS — **no database password, no Npgsql, no local user store**.

## How it works

- **Identity** = Supabase Auth. The site logs in as usual; the API validates
  that access token (`Authority: {Supabase:Url}/auth/v1`). No ASP.NET Identity.
- **Reads** go through PostgREST with the anon key, so RLS applies.
- **Writes** go through PostgREST with the service key (server-side only);
  the API enforces ownership itself: leaders edit their own content
  (`created_by`), admins edit everything, leaders/settings/users need the
  `Admin` policy (role read from the `profiles` table, never from the client).
- Publishing workflow `draft → published → archived` lives in the Domain
  layer (`Announcement.Publish()` stamps `PublishedAt`).

## Setup

```powershell
cd D:\CityYouth\src\CityYouth.Api

dotnet user-secrets init
dotnet user-secrets set "Supabase:Url" "https://<ref>.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "<anon public key>"
dotnet user-secrets set "Supabase:ServiceKey" "<service-role key>"

dotnet run
```

Open `http://localhost:5209/scalar` (the browser opens there automatically).

## Testing in Scalar

1. `POST /api/auth/login` with a leader/admin email + password → copy `access_token`.
2. Click **Authorize**, paste the raw token (no `Bearer ` prefix).
3. Try the draft lifecycle: `POST /api/activities` (draft) →
   `PATCH /api/activities/{id}/status` (published) → `GET /api/activities` →
   `DELETE /api/activities/{id}`.
4. `GET /api/auth/me` shows your id, email and role.

## Endpoints

| Area | Routes |
|---|---|
| Health | `GET /api/health`, `GET /api/health/db` |
| Auth | `POST /api/auth/login`, `GET /api/auth/me` (no public registration) |
| Events | `GET /api/events?filter=upcoming\|past\|all`, `GET /api/events/{slug}`, + CRUD (auth) |
| Activities | `GET /api/activities?status&year&category&limit&offset`, `GET /api/activities/{slug}`, `GET /api/activities/{id}/media`, + CRUD (auth) |
| Media | `GET /api/media?type&activityId`, + CRUD (auth) |
| Announcements | `GET /api/announcements`, + CRUD, `PATCH …/status`, `PATCH …/pin` (auth) |
| Leaders | `GET /api/leaders`, `GET /api/leaders/all` (auth), CRUD (`Admin`) |
| Settings | `GET /api/settings`, `PUT` (`Admin`) |
| Categories | `GET /api/categories` (fixed list) |
| Uploads | `POST /api/uploads` multipart (`bucket`, `folder`, `file`) → public URL (auth) |
| Users | `GET /api/users/profiles`, `PUT …/role` (`Admin`) |

## Rate limits (§49)

- `auth` — 5 login attempts / minute / IP (brute-force cover).
- `api` — 60 requests / minute, per user when logged in, per IP otherwise.
  Exceeding either returns `429`.

## Projects

```text
CityYouth.Api            controllers, auth, middleware, composition
CityYouth.Application    features (MediatR), validation, abstractions
CityYouth.Domain         entities, publishing rules, exceptions
CityYouth.Infrastructure Supabase gateway, storage, roles
```

## Notes

- Behind a VPN that flaps, Supabase failures surface as clean `502`s
  (`SupabaseUnreachableException`); validation → `400`, missing → `404`,
  other people's content → `403`. Nothing leaks internals.
- Uploads forward the original bytes; the web client's WebP compression
  does not run here — compress client-side if size matters.
