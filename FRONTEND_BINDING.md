# Frontend binding guide — ccc-youth (React + Vite) → CityYouth API (.NET)

For the agent doing the work in https://github.com/Biak18/ccc-youth.
Backend repo/contract: `D:\CityYouth\README.md` (live OpenAPI at
`<API>/scalar` in Development). Backend base URL below is called `API`.

## 0. Goal and end state

Today the frontend talks to Supabase directly: public reads via
`src/lib/rest.ts` (PostgREST) and auth/reads/writes/uploads via
`supabase-js` (`src/lib/supabase.ts`, `src/hooks/useAuth.tsx`,
`src/hooks/useAdminData.ts`, admin forms, `src/lib/upload.ts`).

After binding, the frontend talks **only to the backend**:

- public pages → `API/api/...` (no keys, no Supabase)
- admin login/writes/uploads → `API/api/...` with `Authorization: Bearer`
- `supabase-js` is **removed** (`npm uninstall @supabase/supabase-js`,
  delete `src/lib/supabase.ts`). This also fixes Myanmar access: browsers
  never touch `*.supabase.co` again. Media URLs are `res.cloudinary.com`.
- env: delete `VITE_SUPABASE_URL` / `VITE_SUPABASE_ANON_KEY`, add
  `VITE_API_URL=https://<railway-app>.up.railway.app` (no trailing slash).
- backend CORS must list the frontend origin
  (`Cors__AllowedOrigins__0` on the server).

## 1. Global conventions (read first — biggest source of bugs)

| Topic | Backend rule |
|---|---|
| Base | `VITE_API_URL + "/api/..."`, e.g. `GET {API}/api/activities` |
| Casing | **camelCase** JSON. Every snake_case DB column is renamed: `activity_date` → `activityDate`, `cover_image_url` → `coverImageUrl`, `start_date` → `startDate`, `end_date` → `endDate`, `registration_url` → `registrationUrl`, `contact_information` → `contactInformation`, `published_at` → `publishedAt`, `is_pinned` → `isPinned`, `activity_id` → `activityId`, `thumbnail_url` → `thumbnailUrl`, `sort_order` → `sortOrder`, `uploaded_by` → `uploadedBy`, `created_by` → `createdBy`, `created_at` → `createdAt`, `updated_at` → `updatedAt`, `display_name` → `displayName`, `avatar_url` → `avatarUrl`, `user_id` → `userId`, `role_title` → `roleTitle`, `photo_url` → `photoUrl`, `is_visible` → `isVisible`, `church_name` → `churchName`, `youth_name` → `youthName`, `logo_url` → `logoUrl`, `hero_image_url` → `heroImageUrl`, `hero_images` → `heroImages`, `tagline/address/phone/email` unchanged, `facebook_url` → `facebookUrl`, `youtube_url` → `youtubeUrl`, `instagram_url` → `instagramUrl` |
| Lists | paged envelope `{ items: T[], page, pageSize, totalCount }` — **not** a bare array. Exceptions: `GET /api/leaders` (bare array), `GET /api/activities/{id}/media` (bare array), `GET /api/settings` (single object), `GET /api/categories` (string array) |
| Detail | single object (no envelope). Missing → `404` with ProblemDetails body |
| Errors | ASP.NET ProblemDetails: `{ title, detail, status }`. `400` validation, `401` no/bad token, `403` someone else's content, `404` missing, `429` rate-limited. There is no `{ data, error }` wrapper — rewrite `rest.ts`-style handling |
| Auth header | `Authorization: Bearer <accessToken>` (raw JWT, no prefix tricks) |
| Dates | activity `activityDate` is `"YYYY-MM-DD"` (`DateOnly` — `"20260923"` fails, `"2026-09-23"` works). Event `startDate`/`endDate` are full ISO datetimes (`"2026-12-20T08:00:00Z"`) |
| IDs | GUID strings. `activityId` must be a valid GUID or `null` (never `""` or placeholder text) |
| Slugs | optional on create — backend slugifies `title` and dedupes (`x`, `x-2`, …). Keep the client's slugify or drop it; never require the user to type one |
| Server-set fields | `createdBy`/`uploadedBy` come from the JWT; `status` is always `draft` on create. Do **not** send them — they are not accepted |

## 2. Auth (`src/hooks/useAuth.tsx` rewrite)

Backend:

```text
POST {API}/api/auth/login    { email, password } -> { accessToken, refreshToken, expiresIn }
POST {API}/api/auth/refresh  { refreshToken }    -> { accessToken, refreshToken, expiresIn }
POST {API}/api/auth/logout   (Authorization header) -> 204
GET  {API}/api/auth/me       (Authorization header) -> { id, email, displayName, role }
```

- `signIn`: POST login → persist both tokens in localStorage → `GET /me`
  for `{ role }` (`isAdmin = role === "admin"`; profile row no longer read
  from Supabase).
- `useAuth` init (page refresh): if a stored refresh token exists, call
  refresh once → store the new pair → `GET /me`. Refresh 401 → clear both
  tokens → logged out → `/login`.
- `signOut`: best-effort POST logout with the access token, then clear
  storage. No `supabase.auth.signOut()` anymore.
- All auth endpoints are rate-limited (5/min/IP) — one refresh per page
  load, no retry loops.
- Attach the access token to every admin fetch (`Authorization` header).
  On 401 from any admin call: try one refresh + retry once, else log out.

## 3. Public reads (`src/hooks/useContent.ts` → backend)

Keep hook names/signatures; swap `restQuery(...)` for backend fetches
(no auth header needed; anonymous callers see `published` only):

| Hook | Backend |
|---|---|
| `useSiteSettings` | `GET /api/settings` |
| `useUpcomingEvents(limit)` | `GET /api/events?filter=upcoming&pageSize={limit}` |
| `usePastEvents(limit)` | `GET /api/events?filter=past&pageSize={limit}` |
| `useActivities(limit)` | `GET /api/activities?pageSize={limit}` |
| `useArchive(limit)` | `GET /api/activities?status=archived&pageSize={limit}` plus published list, merged newest-first (backend has no combined published+archived list for anonymous) |
| `useAnnouncements(limit)` | `GET /api/announcements?pageSize={limit}` (pinned-first ordering preserved server-side) |
| `useLeaders` | `GET /api/leaders` |
| `useLatestVideos(limit)` | `GET /api/media?type=video&pageSize={limit}` |
| `useActivityBySlug` | `GET /api/activities/slug/{slug}` (404 → null, same as today's 406 handling) |
| `useEventBySlug` | `GET /api/events/slug/{slug}` |
| `useAnnouncementBySlug` | `GET /api/announcements/slug/{slug}` |
| `useActivityMedia(activityId)` | `GET /api/activities/{id}/media` |
| `useAllImages(limit)` | `GET /api/media?type=image&pageSize={limit}` |
| categories (`lib/categories.ts`) | `GET /api/categories` (same 9 values; can stay hardcoded or be fetched) |
| `groupByYear` / `splitMedia` | unchanged (adapt field names to camelCase: `activity_date` → `activityDate`) |

Unwrap `items` from the paged envelope in each hook. Year filter exists
(`GET /api/activities?year=2026`), as do `category`, `search`, `page`.

## 4. Admin reads (`src/hooks/useAdminData.ts` → backend, auth header on)

| Hook | Backend |
|---|---|
| `useAllActivities` | `GET /api/activities?pageSize=100` (signed-in: sees published + archived + own drafts; admins see everything incl. drafts) |
| `useAllEvents` | `GET /api/events?filter=all&pageSize=100` |
| `useAllAnnouncements` | `GET /api/announcements?pageSize=100` |
| `useAllLeaders` | `GET /api/leaders/all` |
| `useActivityById/EventById/AnnouncementById` | `GET /api/activities/{id}` etc. |
| `useMediaByActivity` | `GET /api/activities/{id}/media` |
| `useDashboardStats` | no stats endpoint — derive from `totalCount` with `pageSize=1`: drafts `GET /api/activities?status=draft`, upcoming `GET /api/events?filter=upcoming`, published activities `GET /api/activities?status=published`, photos `GET /api/media?type=image`, videos `GET /api/media?type=video` |

## 5. Admin writes (forms in `src/pages/admin/`)

General rules: send camelCase JSON **without** `id/createdBy/uploadedBy/
createdAt/updatedAt`; create always starts as `draft`; publish = separate
`PATCH {id}/status { status }`; only the owner or an admin can edit/delete
(backend returns `403` otherwise — surface that message).

| Page | Backend calls |
|---|---|
| `ActivityForm` (new) | `POST /api/activities` → optional media creates → Publish button: `PATCH /api/activities/{id}/status { status: "published" }`; Save Draft: nothing more to do |
| `ActivityForm` (edit) | `PUT /api/activities/{id}` (no slug/status fields); status changes only via the status endpoint (`draft`/`published`/`archived`) |
| `ActivityList` actions | view → detail route; publish/archive → status endpoint; delete → `DELETE /api/activities/{id}` |
| `EventForm` / `EventList` | same pattern on `/api/events` (`filter=all` for the list) |
| `AnnouncementForm` / `AnnouncementList` | same pattern on `/api/announcements`; pin toggle → `PATCH /api/announcements/{id}/pin { isPinned }` |
| `Leaders` (admin only) | `POST /api/leaders`, `PUT /api/leaders/{id}`, `DELETE /api/leaders/{id}` (`userId` optional link to a profile; usually `null`) |
| `Settings` (admin only) | `GET /api/settings` prefill → `PUT /api/settings` full object (`heroImages: string[]`) |
| `Users` (admin only) | `GET /api/users?search&page&pageSize` (paged envelope) → role change `PUT /api/users/{id}/role { role: "admin" \| "leader" }` |
| `Login` | section 2 |

Media rows for an activity: after uploading each file (section 6),
`POST /api/media { activityId, type: "image"\|"video",
source: "storage"\|"youtube"\|"external", url, thumbnailUrl, title,
description, sortOrder }`. YouTube/external videos skip upload entirely.
Reorder/retitle → `PUT /api/media/{id} { thumbnailUrl, title, description,
sortOrder }`; remove → `DELETE /api/media/{id}` (deletes the row; see
section 6 for file cleanup, which is now best-effort/absent).

## 6. Uploads (`src/lib/upload.ts` rewrite — important)

Supabase Storage is unreachable from Myanmar without VPN, so uploads now
go **browser → backend → Cloudinary** and public URLs are
`res.cloudinary.com`. `supabase.storage.*` calls must go.

- Keep the canvas WebP compression (`toWebp`) — the backend accepts the
  bytes as-is, so compressing client-side still saves bandwidth.
- Replace `uploadPhoto` internals: build `FormData { bucket: "images",
  file: <webp blob> }` → `POST {API}/api/uploads` (auth header) →
  response `{ url }`. Upload the thumbnail the same way with
  `bucket: "thumbnails"` (two calls, same as today), **or** upload once
  and derive the thumbnail URL client-side by inserting a transformation
  after `/upload/`: `url.replace("/upload/", "/upload/w_480,q_auto,f_auto/")`.
- `uploadSingleImage`: same endpoint with `bucket: "branding"` (or
  `"images"`); folder param no longer exists.
- `deletePhotoFiles`: Cloudinary files have no delete endpoint exposed —
  drop this function (orphaned files are harmless; tidy-up happens in the
  Cloudinary dashboard). `DELETE /api/media/{id}` removes the row only.
- Buckets allowed: `branding`, `images`, `thumbnails`, `videos`
  (`GET /api/uploads/buckets`). Images `jpeg/png/webp/gif`, videos
  `mp4/webm/quicktime`, 50 MB max — backend returns `400` otherwise.
- `coverImageUrl`/`photoUrl`/`logoUrl` fields take the returned
  Cloudinary URL verbatim.

## 7. Types (`src/types/db.ts`)

Keep the type names but convert to camelCase to match the API, e.g.
`activity_date` → `activityDate: string`, `is_pinned` → `isPinned:
boolean`, `sort_order` → `sortOrder: number`. Add backend envelopes:

```ts
export type Paged<T> = { items: T[]; page: number; pageSize: number; totalCount: number };
export type Problem = { title: string; detail: string; status: number };
```

`Database`/`Table`/`Row` supabase-js helper types can be deleted with the SDK.

## 8. Verification checklist (do in order)

1. `GET {API}/api/health` → `{"status":"ok",...}` (also add the Railway
   domain to backend CORS first or every browser call fails).
2. Public page loads with data (activities/events/announcements/leaders).
3. `/login` → dashboard; refresh the page → stays logged in (section 2).
4. Create activity (draft) → invisible on public site → publish → visible.
5. Upload photo → Cloudinary URL → appears in gallery; YouTube URL media works.
6. Leader account: can edit own activity, gets `403` on another's.
7. Admin-only pages (`/admin/users`, leaders write, settings) redirect or
   403 for leaders.
8. Leader flow without VPN (the whole point): upload + publish works.

## 9. Non-goals / do-not-touch

- Backend base URL, route shapes and status workflow (`draft →
  published → archived`, archived never republishable) are fixed — adapt
  the frontend, don't ask for backend changes for these.
- `site_settings` is a singleton (`id = 1`); there is exactly one row.
- No public registration endpoint exists; users are created in Supabase
  Auth and assigned roles via `/api/users/{id}/role` (admin).
