# Identity and session approach for a same-origin SPA

- Ticket: GitHub issue #5, "Identity and session approach for a same-origin SPA" (https://github.com/vlad-gaman/PlanningPoker/issues/5)
- Map: GitHub issue #3, "Map: Angular migration of PlanningPoker"
- Code basis: branch `origin/angular-migration`, project `PlanningPokerUi` (ASP.NET Core 8, `net8.0`)
- Captured: 2026-10-28
- Scope: facts only. No decision is made here.

## Question

With Angular served from the same Azure Web App as the ASP.NET Core 8 API and SignalR hub, how is user identity kept? Today identity is an ASP.NET session cookie holding a GUID (users are not logged in). Options under study: keep the session cookie (SameSite, antiforgery/XSRF with Angular `HttpClient`, SignalR connections reading the session), or use another approach.

## Source key

- [S1] Session in ASP.NET Core. https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0 (page content updated 2026-10-14 per its metadata)
- [S2] Anti-request forgery in ASP.NET Core. https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0
- [S3] AntiforgeryOptions Class. https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.antiforgery.antiforgeryoptions?view=aspnetcore-8.0
- [S4] SignalR authentication and authorization. https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-8.0
- [S5] Work with SameSite cookies in ASP.NET Core. https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-8.0
- [S6] Angular security guide (HttpClient XSRF). https://angular.dev/best-practices/security
- [S7] Angular `withXsrfConfiguration`. https://angular.dev/api/common/http/withXsrfConfiguration
- [S8] Angular XSRF interceptor source, `packages/common/http/src/xsrf.ts`. https://raw.githubusercontent.com/angular/angular/main/packages/common/http/src/xsrf.ts
- [S9] ASP.NET Core antiforgery source, `src/Antiforgery/src/AntiforgeryOptions.cs`. https://raw.githubusercontent.com/dotnet/aspnetcore/main/src/Antiforgery/src/AntiforgeryOptions.cs
- [S10] `HubCallerContext.GetHttpContext`. Page returned HTTP 404 when fetched, so it is not cited.
- [C] Code on `origin/angular-migration`, paths relative to the repo root.

Note on [S1] and [S4]: the fetched text came from the MS Learn pages (the `-8.0` URL redirects to the current doc content, which states monikers from 1.0 to 12.0). Quotes below are from that text.

## 1. Current use of identity and session (code)

Source: [C] on `origin/angular-migration`.

- `PlanningPokerUi/Startup.cs`: `services.AddSession();` and `app.UseSession();` (before `UseStaticFiles`/`UseRouting`). `AddSignalR` with `AddJsonProtocol`. `MapHub<RoomHub>("/roomhub")`. No `AddCors`/`UseCors`, no `AddAntiforgery`, no `AddAuthentication`/`AddAuthorization` policy for identity. `services.AddSingleton<PeopleManagerService>()` and `RoomsManagerService` hold all state in memory.
- `PlanningPokerUi/Services/PeopleManagerService.cs`: `GetPerson(HttpContext)` reads `httpContext.Session.GetString("Guid")` and parses it; `CreatePerson(HttpContext)` calls `Session.SetString("Guid", newGuid)` and stores the `Person` in a `ConcurrentDictionary<Guid, Person>`. The GUID in session is the only identity key.
- `PlanningPokerUi/Controllers/IndexController.cs`:
  - `GET api/cardsets` (no session).
  - `POST api/updatename` with `[FromBody] UpdateNameRequest` sets the session person's name (creates one if missing).
  - `POST CreateRoom` and `POST JoinRoom` take `[FromForm]`/model binding, call `CreatePerson`, and redirect.
  - No `[ValidateAntiForgeryToken]`, `[AutoValidateAntiforgeryToken]` or `[IgnoreAntiforgeryToken]` attribute appears in the controller.
- `PlanningPokerUi/Controllers/RoomController.cs`: `GET Room/{guid}` reads the session person; `HttpContext.Session.Remove("Guid")` when `clearSession=true`; `POST Room` calls `CreatePerson`.
- `PlanningPokerUi/Hubs/RoomHub.cs`: every hub method calls `Context.GetHttpContext()` and then `_peopleManagerService.GetPerson(httpContext)`. This covers `OnConnectedAsync`, `OnDisconnectedAsync`, `JoinRoom`, `Vote`, `ClearVotes`, `ShowVotes`, `ForceShowVotes`, `ChangePersonType`, `HealthCheck`, `UpdateRoomConfiguration`, `TransferOwnership`. `JoinRoom` also reads `httpContext?.Session?.Id` and `httpContext?.Session?.GetString("Guid")` and logs them, and on a null person returns `Error = "Session expired or not found. SessionId: ..."`. So the hub reads the session directly today.
- `PlanningPokerUi/wwwroot/js/site.js`:
  - SignalR client: `new signalR.HubConnectionBuilder().withUrl("/roomhub")` (no `accessTokenFactory`, no `withCredentials` option set).
  - `updateServerSideName()` calls `fetch('/api/updatename', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: ... })`. No antiforgery header is sent.
  - `fetch('/api/cardsets')` is a GET.
- `PlanningPokerUi/Pages/Shared/RoomJoin.cshtml` and `site.js` store the user name in `localStorage` (`planningpoker_user_name`, `planningpoker_user_name_simple`) as a client-side cache. The server session is the only server-side identity.

Observations from the code (not verified at runtime in this research):
- The app does not configure a distributed cache for session in `Startup.cs`. Session behavior across multiple instances therefore depends on framework defaults and on `PeopleManagerService` being in-memory per process. Not verified here.
- The RoomHub reads the session in every method, which the session doc (below) says is unsupported.

## 2. Does SignalR (WebSockets) access `HttpContext.Session`?

Primary-source statement:

- [S1] Section "SignalR/Blazor Server and HTTP context-based state management": "SignalR apps shouldn't use session state and other state management approaches that rely upon a stable HTTP context to store information. SignalR apps can store per-connection state in `Context.Items` in the hub."
- [S1] Section "Session": "Session isn't supported in SignalR apps because a SignalR Hub may execute independent of an HTTP context. For example, this can occur when a long polling request is held open by a hub beyond the lifetime of the request's HTTP context."

What this means for the question (facts only):
- Microsoft Learn says session is not supported in SignalR hubs and recommends `Context.Items` for per-connection state. The stated reason is that a hub can run beyond the HTTP context's lifetime.
- The doc's example names long polling. It does not separately document session behavior for the WebSockets transport. The `HttpContext` a hub sees is the one from the connection's establishing request; the doc does not state whether session is loaded for it on WebSockets.
- The current `RoomHub` depends on `Context.GetHttpContext().Session` for identity in every method. This is the pattern [S1] labels unsupported. Whether it works reliably on the WebSockets transport in practice is not established by these sources. An empirical test on the target app would be needed (not done here).
- [S1] also says session is loaded asynchronously only if `ISession.LoadAsync` is called explicitly before `TryGetValue`/`GetString`. The current code calls `GetString` synchronously in `PeopleManagerService` and the hub. Quote: "The default session provider in ASP.NET Core loads session records from the underlying IDistributedCache backing store asynchronously only if the ISession.LoadAsync method is explicitly called before the TryGetValue..." (the rest of that sentence was cut in the fetched text).

SignalR auth for cookies ([S4]):
- Section "Cookie authentication": "In a browser-based app, cookie authentication allows existing user credentials to automatically flow to SignalR connections. When the browser client is used, no extra configuration is needed."
- Section "Cookies versus bearer tokens": "Cookie authentication isn't recommended unless the app only needs to authenticate users from the browser client. Bearer token authentication is the recommended approach when using clients other than the browser client."
- [S4] also says: "SignalR captures the authenticated user when a connection is established and caches it for the lifetime of the connection." This is about `Context.User` (authentication), not session.
- [S4] on bearer tokens over WebSockets: "browsers can't set custom headers, such as `Authorization`, on the WebSocket and Server-Sent Events APIs. ... SignalR sends the token as a query string parameter for those transports when the client runs in a browser." (Relevant only if a bearer scheme is chosen.)

## 3. Does Angular's built-in XSRF support match ASP.NET's antiforgery cookie and header names?

Angular defaults (confirmed in source):
- [S8] `XSRF_DEFAULT_COOKIE_NAME = 'XSRF-TOKEN'`, `XSRF_DEFAULT_HEADER_NAME = 'X-XSRF-TOKEN'`.
- [S8] Interceptor skips `GET` and `HEAD` (`if (!inject(XSRF_ENABLED) || req.method === 'GET' || req.method === 'HEAD')`).
- [S8] It normalizes relative URLs against the page origin, and skips cross-origin requests (`if (locationOrigin !== requestOrigin)`).
- [S8] It sets the header only if the token is non-null and the request does not already have that header.
- [S6] "an interceptor reads a token from a cookie, by default `XSRF-TOKEN`, and sets it as an HTTP header, `X-XSRF-TOKEN`." And: "By default, an interceptor sends this header on all mutating requests (such as `POST`) to relative and same origin URLs ... but not on `GET` or `HEAD` requests."
- [S6] "HttpClient supports only the client half of the XSRF protection scheme. The backend must set the cookie and verify the header on eligible requests. Without that, Angular's default protection is ineffective."
- [S7] `withXsrfConfiguration` customizes `cookieName` and `headerName` for an `HttpClient` instance. The reference page does not state the defaults; those come from [S8].
- [S6] `withNoXsrfProtection` disables it.

ASP.NET antiforgery defaults (confirmed in source):
- [S9] `HeaderName` default: `"RequestVerificationToken"` (the `AntiforgeryTokenHeaderName` constant).
- [S9] `FormFieldName` default: `"__RequestVerificationToken"`.
- [S9] Cookie name prefix `DefaultCookiePrefix = ".AspNetCore.Antiforgery."`. No cookie name is set by default, so the cookie name is that prefix plus a suffix.
- [S9] `Cookie.HttpOnly = true` by default. `Cookie.SecurePolicy = CookieSecurePolicy.None` by default (the doc states ASP.NET Core relaxes the secure policy for the antiforgery cookie by setting `CookieSecurePolicy.None`).
- [S3] `HeaderName`: "Specifies the name of the header value that is used by the antiforgery system. If `null` then antiforgery validation will only consider form data." `FormFieldName`: "Specifies the name of the antiforgery token field that is used by the antiforgery system."
- [S2] Angular-compatible pattern in the doc (not the default): set a cookie named `XSRF-TOKEN` with `HttpOnly = false`, and configure `options.HeaderName = "X-XSRF-TOKEN"`. Quote: "Assuming the script sends the token in a request header called `X-XSRF-TOKEN`, configure the antiforgery service to look for the `X-XSRF-TOKEN` header."
- [S2] Quote on cookie-based auth: "If cookies are used to store authentication tokens and to authenticate API requests on the server, CSRF is a potential problem."
- [S2] Quote: "For example, Angular includes built-in XSRF protection that reads a cookie named `XSRF-TOKEN` by default."

Comparison (facts):
- Default names do not match. Angular expects cookie `XSRF-TOKEN` and header `X-XSRF-TOKEN`. ASP.NET defaults to cookie `.AspNetCore.Antiforgery.*` (HttpOnly, so script cannot read it) and header `RequestVerificationToken`.
- They can be made to match with explicit configuration on both sides (server: issue an `XSRF-TOKEN` cookie with `HttpOnly = false` and set `HeaderName = "X-XSRF-TOKEN"`; client: Angular defaults, or `withXsrfConfiguration`). [S2] describes this pattern.
- Angular's protection only covers `POST`/`PUT`/`DELETE`-style calls to relative or same-origin URLs. This is the same-origin setup, so the Angular interceptor would apply to the app's calls.
- The current code does not enable antiforgery on any endpoint (see section 1). The `fetch('/api/updatename')` POST sends no token.
- Razor Pages and MVC protection ([S2]): "Razor Pages are automatically protected from XSRF/CSRF" (note in the 8.0-10.0 doc range). For MVC, "AddControllers does not enable antiforgery tokens. AddControllersWithViews must be called to have built-in antiforgery token support." The app calls `AddRazorPages()`; the doc does not state whether `AddRazorPages` alone enables the MVC action filters for `IndexController` POST actions. Not established here. The `IndexController` actions have no explicit attribute.
- Minimal APIs (not used here) validate form-binding endpoints by default, after `UseAntiforgery()` ([S2]).
- [S2] states SignalR is not mentioned on the antiforgery page. The hub is not covered by antiforgery filters; antiforgery is a request-level filter/middleware concern. Not stated explicitly by any source fetched.

## 4. Is CORS or `SameSite=None` needed when same-origin?

- [S5] `SameSite=None` is for cross-site delivery: "`SameSite=None` must be used to allow cross-site cookie use." It also says: "Cookies that assert `SameSite=None` must also be marked as `Secure`."
- [S5] Cookies without a SameSite attribute are treated as `SameSite=Lax` by default by browsers.
- [S5] ASP.NET Core defaults: `Response.Cookies.Append` uses `SameSiteMode.Unspecified` (no attribute). Session cookie: `SessionOptions.Cookie` SameSite is `Lax` (table row "Session | SessionOptions.Cookie | Lax").
- Inference (flagged, not stated by the sources): if the Angular app and the API are served from the same origin (scheme, host, port), requests are same-origin, so `SameSite=None` is not needed for the session cookie to be sent. The sources state `None` is for cross-site use; they do not explicitly say "same-origin needs no None".
- CORS: the sources fetched did not cover CORS policy. The app has no `AddCors`/`UseCors` in `Startup.cs` (code fact). A same-origin SPA does not need a CORS policy for its own API calls under the browser same-origin rules; this is inferred from the browser model and not from a fetched source here.

## 5. Other facts from the session doc relevant to the hosting constraint

- [S1] "Session state cookies aren't marked essential by default. Session state isn't functional unless tracking is permitted by the site visitor." (GDPR note; the app does not set `IsEssential`.)
- [S1] "The session cookie is encrypted via IDataProtector. Data Protection must be properly configured to read session cookies on each machine." Relevant to any multi-instance Azure hosting.
- [S1] "Don't store sensitive data in session state."
- [S1] Session defaults: cookie name `.AspNetCore.Session`, path `/`, IdleTimeout default 20 minutes (each access resets it). Cookie `HttpOnly` is set by the session config examples; the doc states the default cookie is not available to client script.
- [S1] "Empty sessions aren't retained. The session must have at least one value set to persist the session across requests."
- [S1] Cookies: "The user is only identified and not authenticated in most cases. The cookie can store the user's name, account name, or unique user ID such as a GUID." (This matches the current pattern.)

## Summary of primary-source positions

1. SignalR hubs: Microsoft Learn says session is not supported in SignalR apps and recommends `Context.Items` for per-connection state [S1]. The current `RoomHub` reads session in every method [C].
2. Cookie-based identity flows to the SignalR browser client without extra configuration [S4]. Bearer tokens are recommended for non-browser clients [S4].
3. Angular's XSRF defaults (`XSRF-TOKEN` / `X-XSRF-TOKEN`) do not match ASP.NET defaults (`.AspNetCore.Antiforgery.*` HttpOnly / `RequestVerificationToken`); they match only with explicit configuration [S2][S3][S8][S9].
4. `SameSite=None` is for cross-site cookies and requires `Secure` [S5]. Session cookies default to `Lax` [S5]. Same-origin needs no `None` (inference, flagged).
5. The app has no antiforgery, CORS or auth configuration; the only identity is a session GUID cookie, and `updatename` POST sends no XSRF token [C].

## Open items (not resolved by these sources)

- Whether session is actually populated for hub methods on the WebSockets transport in this app (needs a test on the deployed Azure Web App).
- Whether `AddRazorPages` enables MVC antiforgery filters for `IndexController` POST actions (no explicit attribute in code).
- Whether the session provider is backed by memory or a distributed store in this app (no `IDistributedCache` registration in `Startup.cs`).
- `Context.GetHttpContext()` contract on WebSockets (the MS Learn API page returned 404 during research).
