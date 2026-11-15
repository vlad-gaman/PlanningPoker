# Anonymous person and room creation is rate limited per client address, with no cap per owner

Nobody signs in to create a person (`PUT /api/me`) or a room (`POST /api/rooms`), so there was no limit on how many either could be made, and an unjoined room stayed in memory until the 24 hour abandoned sweep. Room ids are shareable codes, not secrets, so guessing them is not prevented by entropy either (ticket 117).

Decision: fixed-window limits per client address, as read from `Connection.RemoteIpAddress` after the forwarded headers are applied (ADR 0001; `UseRateLimiting` runs after `UseForwardedHeaders`):

- `PUT /api/me`: 10 per minute
- `POST /api/rooms`: 10 per minute
- `GET /api/rooms/{id}`: 30 per minute
- failed `JoinRoom` calls on the hub: 10 per minute. Only failures count, so joining rooms that exist is never limited; once the failures are used up, every join from the address is refused until the window ends.

A refused request gets 429 with the usual `ApiResponse` body (error code `rate_limited`) and a `Retry-After` header. The limits and an `Enabled` switch are the `RateLimiting` appsettings section; the defaults are the numbers above. The end-to-end run and the integration tests make far more requests from one address than that, so they set `RateLimiting:Enabled=false` (`playwright.config.ts`, `ApiFactory`); `RateLimitedApiFactory` tests the real defaults.

A room nobody has joined is deleted `Presence:NeverJoinedRoomLifetime` (1 minute) after it was created, by the presence sweep, instead of waiting for the 24 hour abandoned sweep. The creator's browser joins within seconds of creating it.

Considered options:
- **A cap on rooms per owner**: an owner is an anonymous person who can be minted again at will, so the cap would only bind honest users. Rejected.
- **One global limiter**: it would also throttle the room page itself and the static files, and one abusive address would starve others. Rejected for per-address limits on the few endpoints that need them.

Consequences:
- Everyone behind one shared address (an office NAT) shares these limits. 10 new people or rooms a minute is far above normal use, but a large workshop starting at once from one address could hit it.
- The counters are in memory per instance, so they reset on a restart and are not shared between scaled-out instances.
- Behind a front end, the limits are only as good as the forwarded headers: with `TrustAnySender` true on a host that is directly reachable, a client could pick its own address and dodge them (ADR 0001).
