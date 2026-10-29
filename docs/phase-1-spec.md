# Phase 1 spec: walking skeleton + create/join

Hand-off spec for building slice 0 and slice 1 of the Angular migration of PlanningPoker. Source of truth for the *why* is the wayfinder map, [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` apply throughout (Controller-Service-Repository as separate projects, `ServiceResponse` / `ApiResponse`, mappers, one class per file, `Models` folders, `= default!`, xUnit, Swagger on every endpoint and response).

Scope is **parity only**: no new features. Branch: `angular-migration` (long-lived). One pull request per slice, reviewed by the owner, CI green before merge. The old `PlanningPokerUi` project is not touched until the cutover.

## 0. Verify first

Failing any item changes the plan; none blocks starting the skeleton.

| Check | Fallback if it fails |
|---|---|
| .NET 10 runs on the Windows App Service `PlanningPokerUi` (UK South, S1) | Target .NET 8, plan the upgrade (.NET 8 support ends Nov 2026) |
| The plan supports a **staging deployment slot** (portal: Web App > Deployment > Deployment slots) | No pre-production deploy: verify locally + Playwright, deploy at cutover, roll back via the `razor-final` tag and kept build |
| An Entra app registration can be created for OIDC deploy credentials | Environment-scoped **publish profile** secret (basic-auth SCM must be enabled on the app) |
| Node 24 installed locally (dev PC has Node 20.14; Angular 22 needs ^22.22.3, ^24.15 or >=26) | Upgrade Node; no workaround |

Record the outcome of each in the slice 0 pull request description.

## 1. Slice 0: walking skeleton

Goal: the whole pipe exists and is green end to end, with nothing room-specific yet.

### 1.1 Projects (all under the repo root, added to a new `PlanningPoker.sln`)

| Project | Contents | References |
|---|---|---|
| `PlanningPoker.Common` | `ServiceResponse<T>` / `ServiceResponse`, `ServiceError` (code, message, metadata dictionary), error-code constants, shared constants | none |
| `PlanningPoker.Repository` | entities, in-memory repositories | Common |
| `PlanningPoker.Service` | services, service models, mappers, `*Validation` classes | Common, Repository |
| `PlanningPoker.Controller` | REST controllers, `RoomHub`, `ApiResponse<T>`, request `*Validation` classes, middleware, API contracts, `ApiResponseMapper` | Common, Service |
| `PlanningPoker.Api` | thin host: minimal-hosting `Program.cs`, DI wiring, Swagger, SPA static files | Controller, Service, Repository |
| `PlanningPoker.UnitTests` | xUnit tests | all of the above |
| `PlanningPoker.Client` | Angular 22 app (folder, not a .NET project) | n/a |

Target framework .NET 10 (see section 0). Each layer registers its own classes in DI through an `AddXxx` extension method. Mappers are named after their output object, singleton, with interfaces; they may assume non-null parameters.

### 1.2 Backend pieces

- **`ApiResponse<T>`**: `{ "data": ..., "errors": [ { "code", "message", "metadata": { "k": "v" } } ] }`. `data` is for success, `errors` for failure; either may be null. 204 is the only response without a body. Repeated errors index their metadata keys (`person_id_1`, `person_id_2`).
- **`ApiResponseMapper`** (singleton, interface): maps a `ServiceResponse` to `ApiResponse`. A static table keyed by error code gives the HTTP status. Slice 0 seeds `internal_error` -> 500, `validation_failed` -> 400, `unsupported_media_type` -> 415, `no_session` -> 401; slice 1 adds the rows listed in section 2.
- **Middleware** (in the Controller project, registered by the Api host):
  1. Global exception handler: logs the exception with `ILogger` and returns an `ApiResponse` with code `internal_error`.
  2. JSON-only guard: state-changing routes (`POST`/`PUT`/`PATCH`/`DELETE`) reject non-`application/json` bodies with `unsupported_media_type` (CSRF defence with `SameSite=Lax`; no antiforgery tokens).
  3. Session cookie (see below).
- **Logging**: built-in `ILogger` to console only (App Service captures it). No Application Insights or Serilog. Hub command failures are logged with room id and person id.
- **Session cookie** (replaces ASP.NET `Session`; `AddSession` is not used): a cookie holding the **person GUID**, HttpOnly, Secure, `SameSite=Lax`, session cookie (no expiry), name `pp_session`. A stale GUID (no matching person, for example after a restart) is treated as "no person". The cookie is minted by `PUT /api/me` when no valid person exists, and replaced when stale. REST and the hub handshake both read it (same origin, no bearer token, no CORS).
- **`GET /api/me`** is specified in slice 1; slice 0 only needs the cookie middleware and a person-resolving helper.
- **`RoomHub`** at `/hubs/room`, in the Controller project. Empty in slice 0 apart from `OnConnectedAsync` / `OnDisconnectedAsync`. A connection with no valid person is rejected with 401 at connect. Hub methods return the `ApiResponse` shape via `invoke`. JSON is camelCase on both REST and hub.
- **Swagger** (Swashbuckle) on the Api host; every endpoint documents its request and its responses (`ProducesResponseType` with `ApiResponse<T>`).
- **Static files / SPA**: serve `wwwroot` (the Angular build) with `MapFallbackToFile("index.html")`; the fallback must not swallow `/api/*`, `/hubs/*` or `/swagger*`.
- **Repositories** in Slice 0: register the singletons `IPersonRepository` and `IRoomRepository` per the layer contract below, implemented in slice 1.

### 1.3 Repository contract (implemented in slice 1)

Entities hold GUID references only; no shared object graph.

- `PersonEntity`: `Guid`, `Name`, `PersonType` (`dev` | `test` | `obs`), `Connections` (connection id -> room id). This is an index that tells an exit which room to ask; it does not decide membership.
- `RoomEntity`: `Id` (string), `OwnerGuid`, `MemberGuids`, `MemberConnections` (member GUID -> open connection ids in this room), `Votes` (person GUID -> mark), `VotingEnabled`, `IsDisposed` (set when the last member leaves; a disposed room takes no members), `Configuration` (card set, countdown seconds, show fireworks, max participants).
- `IPersonRepository` (sync): `Get` (returns the live entity, so only `Update` may mutate it), `TryAdd`, `Remove`, `Update<T>(personGuid, Func<PersonEntity, T>)` under a per-person lock.
- `IRoomRepository` (sync): `Get` (snapshot copy), `TryAdd`, `Remove`, `Update<T>(roomId, Func<RoomEntity, T>)`. `Update` is the only way to mutate a room and runs under a per-room lock so compound operations are atomic. Every membership decision (admission, leaving, disposal, ownership transfer) is made inside a room `Update`. The person and room locks are never nested.
- Both are singletons registered against their interfaces. No eviction, TTL or sweeper (tracked separately in [#12](https://github.com/vlad-gaman/PlanningPoker/issues/12)).
- Voting countdown timers are *not* in the repository: a Service-layer singleton `IRoomTimerService` (used from slice 2 onward). There is no health-check timer.

### 1.4 Frontend pieces (`PlanningPoker.Client`)

- Angular 22, Node 24 (`.nvmrc` + `engines`), standalone components only, signals, new control flow, `provideRouter`, `provideHttpClient` in `app.config.ts`, strict TypeScript.
- Structure: `src/app/core` (hub client, API client, session/me, error handling), `features/home`, `features/room`, `features/credits`, `shared`, `models` (TypeScript interfaces mirroring the API contracts).
- `ApiClient` wraps `HttpClient` and unwraps `ApiResponse`. `RoomHubClient` is a thin injectable around `@microsoft/signalr`'s `HubConnection` (typed `invoke`/`on`), so unit tests fake it. `RoomStore` is a signal-based store (introduced in slice 1).
- Angular Material 22 for standard controls; **Bootstrap is not used**. Colours come from the global theme variables (section 1.6).
- Routes: `/` (home), `/room/:id`, `/credits`, plus redirects `Room/:id` -> `room/:id` and `Credits` -> `credits` for old shared links. The `credits` route is a stub in Phase 1.
- Dev: `ng serve` on 4200 with `proxy.conf.json` forwarding `/api` and `/hubs` (`ws: true`) to `https://localhost:5001`. One origin for the browser, so no CORS. A root script or VS Code task starts both.

### 1.5 Theming in the skeleton

Move the colour variables from `PlanningPokerUi/wwwroot/css/site.css` (`:root` and `[data-theme="dark"]`) into the Angular global styles; do not copy the repeated `prefers-color-scheme` blocks. A signal-based `ThemeService` stores the three-way preference (auto / light / dark, default auto) in `localStorage` and sets the *effective* theme as `data-theme` on `<html>`; a small inline script in `index.html` sets it before first paint. One stylesheet maps Material's `--mat-sys-*` tokens to the site variables. The full theme control, chart theming and Credits content are slice 5; Phase 1 only needs the variables, `ThemeService` and the Material mapping so later slices inherit them.

### 1.6 CI and deployment

- **GitHub Actions workflow** on pushes to `angular-migration` and on pull requests: `dotnet build` + `dotnet test` (xUnit), `npm ci`, Angular lint, `ng test` (Vitest), `ng build`, `dotnet publish` of `PlanningPoker.Api` with the Angular `dist` copied into `wwwroot`, then Playwright against the published app.
- **Deploy**: separate manual workflow (`workflow_dispatch`), gated on the tests having passed, zip-deploying the published output. Credential per section 0 (OIDC or an environment-scoped publish profile in a GitHub Environment with a required reviewer; third-party actions pinned to commit hashes). With a staging slot it deploys to the slot; cutover is a slot swap. Each deploy tags the commit and keeps the zip as a release/artifact; a second manual workflow redeploys a chosen earlier build (rollback).
- **Web App settings** (owner sets in the portal; slice 0 PR checks): WebSockets on, Always On, HTTPS Only, one instance with no autoscale, `ASPNETCORE_ENVIRONMENT=Production`, no Node runtime setting. The staging slot needs the same.

### 1.7 Tests for slice 0

- xUnit: `ApiResponseMapper` status table and shape; the exception middleware returns `internal_error`; the JSON-only guard; session-cookie flags and stale-GUID handling; the SPA fallback does not shadow `/api`.
- Vitest: `ApiClient` unwrapping of success and error `ApiResponse`; `ThemeService` effective-theme resolution and persistence.
- Playwright (first e2e): the app loads at `/`, and a session cookie with the HttpOnly/Secure/`SameSite=Lax` flags is issued.

### 1.8 Acceptance criteria, slice 0

1. `dotnet test`, `ng test`, `ng build` and Playwright pass in CI on a clean checkout.
2. Swagger lists the slice 0 endpoints with documented responses.
3. `/room/xyz`, `/Room/xyz` (redirected), `/credits` and `/Credits` (redirected) all serve the SPA; `/api/unknown` returns a JSON `ApiResponse` error, not `index.html`.
4. The "verify first" outcomes are recorded.

## 2. Slice 1: create and join

Goal: a person can set a name and type, create a room (with settings) or join one by link, and every member sees the live participant list.

### 2.1 Old behaviour to match

From `IndexController`, `RoomController`, `RoomHub.JoinRoom` and `Person`:

- Name: required, 2-20 characters (`minLength="2" maxlength="20"` on the form; the server truncates to 20). Person type is `dev`, `test` or `obs`; default `dev`. Anything else is invalid.
- **Create room**: name required. Room settings: card set (default `modified-fibonacci`), countdown seconds (1-30, default 5), show fireworks (default true), max participants (default 50), and *use fun room name*. With the fun name the id is generated by `IRoomNameGenerator` (CSV word lists, loaded once via DI); otherwise a GUID. The creator becomes the owner and the first member. The id is retried up to 5 times on collision; if all fail, return `room_id_generation_failed`.
- **Join**: name and room id required; the room must exist (`room_not_found`); the person joins the room group and gets the current participants and vote status; others are told `PersonJoined`. If voting is enabled and the person is not an observer, the countdown is reset and everyone gets `CountdownReset`. A second tab or a reconnect of a person already in the room only joins the group: nobody is told anything, and the countdown is not reset.
- **Entering `/room/:id` with no name**: the SPA shows name entry (name + person type) *inside the room route*, then joins. An unknown room id redirects to `/` with the message that the room does not exist.
- **Page behaviour**: the home page shows server-style errors inline; the card-set list comes from the API.

### 2.2 REST endpoints

| Method + route | Request | Success | Errors |
|---|---|---|---|
| `GET /api/card-sets` | none | 200 `[ { value, display } ]` for the 7 sets (`fibonacci`, `modified-fibonacci`, `linear`, `powers-of-2`, `t-shirt-sizes`, `traffic-lights`, `fist-of-five`) with their display names | none |
| `GET /api/me` | none | 200 `{ guid, name, personType }` | 401 `no_session` when the cookie is absent or stale |
| `PUT /api/me` | `{ name, personType }` | 200 same shape; mints or replaces the cookie when needed | 400 `validation_failed` with metadata `field` / `reason` (name length, invalid person type) |
| `POST /api/rooms` | `{ useFunRoomName, configuration: { cardSet, countdownSeconds, showFireworks, maxParticipants } }`; the creator is the session person | 201 `{ id }` | 401 `no_session`; 400 `validation_failed` (unknown card set, countdown outside 1-30, max participants < 1); 409 `room_id_generation_failed` |
| `GET /api/rooms/{id}` | none | 200 `{ id, exists: true }` | 404 `room_not_found` |

Status table additions: `room_not_found` -> 404, `room_id_generation_failed` -> 409, `not_room_owner` -> 403 (used from slice 4).

### 2.3 Hub: `JoinRoom(roomId)` at `/hubs/room`

- Returns the `ApiResponse` shape. Success data: `{ people: [ { guid, name, personType } ], voteStatus, ownerGuid, configuration: { cardSet, countdownSeconds, showFireworks, maxParticipants } }`. `voteStatus` is the same shape used later for votes (each person's mark masked as `"hide"` until revealed; `hasEveryoneVoted`). **No `RoomJoined` event.**
- Errors: `room_not_found`; `validation_failed` (invalid room identifier: reject empty, over-long or characters outside letters, digits, `-`, `_`; or `connectionId` with reason `already_in_room` when this connection already joined a different room; joining the same room again on the same connection is a no-op); `no_session` is handled at connect (401).
- `connection_closed` (hub-only): the client disconnected while `JoinRoom` was running. The join is rolled back with an exit and nothing is announced. The group subscription is taken before the join and removed again if the join fails.
- Side effects: subscribe the connection to the SignalR group for the room (before the join, so no broadcast is lost between snapshot and subscription); index the connection in the person's `Connections` and add it to the room's `MemberConnections`; broadcast `PersonJoined` `{ person, vote, personType }` to the group *except* the caller; if voting is enabled and the person is not `obs`, broadcast `CountdownReset` `{ countdown, reset: true }` to the others. Both are skipped when the person already had a connection in the room (`PersonAdded` false on the join result).
- Disconnect (`OnDisconnectedAsync`): remove this connection from its room. If the person has another connection to that room (another tab), nothing is broadcast. Otherwise broadcast `PersonExited`; if the owner left, transfer ownership to a random member and broadcast `OwnershipTransferred`; if nobody is left, mark the room disposed, remove it and broadcast `RoomDisposed`. An unknown connection gets `not_in_room` (hub-only) and nothing is broadcast. (Voting reveal on exit and timer handling arrive in slices 2 and 4; slice 1 handles exit, ownership transfer and room disposal inside the room's `Update`.)
- Events defined in this slice, names unchanged from the old hub: `PersonJoined`, `PersonExited`, `OwnershipTransferred`, `RoomDisposed`, `CountdownReset`. Payloads are typed DTOs in the Controller project mirrored as TypeScript interfaces in `models/`.

### 2.4 Service layer

- `IPersonService`: create/update the session person; validations (`PersonNameValidation`, `PersonTypeValidation`).
- `IRoomService`: `Create`, `Exists`, `Join`, `Exit` (atomic exit + ownership transfer + disposal via `IRoomRepository.Update`).
- `IRoomNameGenerator`: replaces the static `RoomNameGenerator`; the CSVs (`Services/Csvs` in the old project) move with it and load once.
- Validations are `*Validation` classes returning `ServiceResponse`: request-shape validations in the Controller project (`CreateRoomRequestValidation`, `UpdateMeRequestValidation`), state-dependent ones in the Service project (`RoomExistsValidation`, `RoomIdentifierValidation`).
- Mappers, named after their output: `PersonEntityMapper`, `RoomEntityMapper`, `PersonResponseMapper`, `RoomCreatedResponseMapper`, `JoinRoomResponseMapper`, `PersonJoinedEventMapper`, and similar.

### 2.5 Angular pieces

- `features/home`: name field (2-20), person-type control (dev / test / observer), "Create room", a settings dialog (Material dialog) with card set (from `GET /api/card-sets`), countdown 1-30, show fireworks, max participants, fun room name; "Join room" with a room-name field. Reactive forms with Material form fields; inline server errors from `ApiResponse.errors`.
- `features/room`: route `/room/:id`. If `GET /api/me` says `no_session` or the person has no name, show name entry inside the route, then `PUT /api/me`, connect the hub and `JoinRoom`. Participant table fed by `RoomStore`. A leave action disconnects and returns to `/`.
- `RoomStore`: signals for people, owner, votes (masked), configuration; updated by `JoinRoom`'s result and the `PersonJoined` / `PersonExited` / `OwnershipTransferred` / `RoomDisposed` events. On `RoomDisposed`, navigate to `/` with a message.
- Hub reconnect: `withAutomaticReconnect`; after reconnect re-invoke `JoinRoom` (this replaces the old health check).

### 2.6 Tests for slice 1

- xUnit: `IRoomNameGenerator`, `IRoomService` (create, join, exit/transfer/dispose, collision retry then `room_id_generation_failed`), repositories (atomic `Update` under concurrent callers), every validation, every mapper, controller endpoints for each success and error row above, `RoomHub.JoinRoom` success and each error, and `OnDisconnectedAsync`.
- Vitest: `RoomStore` event handling, `ApiClient` error mapping, the home form validation, the name-entry-in-room flow with a faked `RoomHubClient`.
- Playwright (two or three browser contexts): create room with the fun name and with a GUID; second context joins by link and both see two participants; third joins as observer; owner leaves and ownership transfers; last person leaves and a later visit to that room redirects to `/`; `/Room/{id}` old-style link works; invalid name is rejected.

### 2.7 Acceptance criteria, slice 1

1. The Playwright flows above pass in CI.
2. Swagger documents every endpoint and response.
3. No `RoomJoined`, health-check, `ShowVotes` or `SetupHealthCheck` surface exists in the new hub.
4. `dotnet test` / `ng test` / `ng build` are green; no unused code remains.

## 3. Decisions for review

These are points where the old code is unclear or inconsistent. The spec makes a call; change it if you disagree.

1. **Max participants is not enforced in the old code** (`RoomsManagerService.JoinRoom` ignores it). The spec keeps parity: the value is stored and editable but no `room_full` error exists. Enforcing it would be a new behaviour.
2. **Non-ASCII names**: the old server rewrites non-ASCII characters to `%uXXXX`. The spec drops that and stores the name as typed (Angular escapes on render).
3. **Name length**: the form says 2-20, the server only truncates to 20. The spec enforces 2-20 with `validation_failed`.
4. **Staging slot**: the deploy decision in the map says "no deployment slots"; the test-strategy decision later added a staging slot if the plan supports it. This spec follows the later one.
