# Phase 2 spec: voting (slice 2)

Hand-off spec for slice 2 of the Angular migration of PlanningPoker: cast, change and reveal a vote, the reveal countdown, clearing votes, and the per-participant vote status. Source of truth for the *why* is the wayfinder map, [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` and the layer and concurrency rules in [`phase-1-spec.md`](phase-1-spec.md) (section 1.3 and 2.3) apply unchanged.

Scope is **parity only**: no new features. Branch: `slice-2-voting`, one pull request into `angular-migration`, reviewed by the owner, CI green before merge. The old `PlanningPokerUi` project is not touched.

## 0. Where this slice sits

[Issue #15](https://github.com/vlad-gaman/PlanningPoker/issues/15) orders the slices: walking skeleton, create/join, **voting**, results, room management, shell/polish, cutover. This slice is "voting". The boundary with its neighbours:

| In slice 2 (this spec) | Later |
|---|---|
| Card picker, `Vote`, changing a vote, `ClearVotes`, `ForceShowVotes` ("Show votes") | Statistics, the three charts, average marks, fireworks: slice 3 (results) |
| Auto-reveal countdown (`CountdownUpdate`, `CountdownReset`), voting rules on join and exit | `ChangePersonType`, `UpdateRoomConfiguration` and its "clear votes if shown" rule, `TransferOwnership`: slice 4 (room management) |
| Per-participant vote status (hidden mark / revealed mark), `hasEveryoneVoted` | Theme control, credits, shell: slice 5 |
| `VotesShown` carries the revealed marks | `VotesShown` gains `statistics` in slice 3 (additive) |

Slice 1 already built the groundwork this slice uses: `RoomEntity.Votes` and `VotingEnabled`, `VoteStatus` / `VoteStatusMapper`, `JoinRoomResult.ResetCountdown`, the `CountdownReset` event, and removal of a leaver's vote.

## 1. Old behaviour to match

From `RoomHub` (`Vote`, `ClearVotes`, `ForceShowVotes`, `JoinRoom`, `PersonExit`), `Room`, `MyTimer` and `site.js`:

- **Cards**: each room has a card set; the cards of a set are in `Models/CardSets.cs` (value + optional display: `½`, `☕`, traffic-light emoji, or a PNG for fist-of-five). One card is selected at a time (radio buttons). Observers have no vote; the cards are shown to them but a vote from them is ignored.
- **Vote**: `Vote(mark)` stores the mark under the caller's guid, replacing an earlier one (a vote can be changed any time before the reveal). Rejected silently when voting is closed (votes revealed) or the caller is an observer. Then everyone gets `VoteCast(personGuid)` and `VoteStatusUpdate(voteResultInfo)`.
- **Vote status (hidden)**: until the reveal, every participant who voted shows a hidden marker (`▮`); a participant who has not voted shows nothing. Marks are masked as `"hide"` on the wire. `hasEveryoneVoted` is true when every non-observer in the room has voted.
- **Auto-reveal**: when a vote makes everyone voted (`hasEveryoneVoted`), a countdown of `countdownSeconds` starts. The first `CountdownUpdate { countdown: N }` goes out at once; then `N-1 ... 1` each second; at 0 the votes are revealed (`VotesShown`) and no `CountdownUpdate 0` is sent. Clients show "Showing results in N!". **Changing a vote during the countdown restarts it from N.**
- **Countdown 0**: the old form allowed 0 and the server then reveals immediately (see decision 2).
- **Show votes** (button): reveals at once, whatever the vote state, only while voting is open. Any member may press it.
- **Clear votes** (button): drops all votes, reopens voting, cancels any countdown, sends `VotesCleared`. Any member may press it, whether or not votes are revealed.
- **Reveal**: voting closes (`VotingEnabled` false); `VotesShown` carries each non-observer's real mark; the cards are disabled until votes are cleared. Late joiners and reconnecting clients get the revealed marks in their join snapshot.
- **Countdown reset on join**: a person who joins (not an observer, voting open) cancels a running countdown; the others get `CountdownReset` and hide the countdown. Slice 1 sends the event; this slice also cancels the countdown on the server. A second tab or a reconnect of someone already in the room changes nothing.
- **Exit**: a leaver's vote is removed. If voting is open and everyone remaining has voted, the countdown (re)starts (or reveals at once with countdown 0). A running countdown is cancelled first, so it restarts from N.
- **Client**: marks fill the participant row (`▮` hidden, card display revealed); after `VotesCleared` the selected card, the marks and the countdown are cleared and the cards are enabled again. `VoteStatusUpdate` / `VotesShown` / `PersonJoined.vote` all set row marks.

## 2. REST and hub contract

### 2.1 REST

No new endpoints. `GET /api/card-sets` keeps listing the set names (settings dialog). The cards of the room's set arrive with the join response (decision 5).

### 2.2 Hub methods on `/hubs/room`

All return the `ApiResponse` shape. The caller's room comes from the connection index (`PersonEntity.Connections`), as `Exit` does; there is no `roomId` argument.

| Method | Arguments | Success data | Errors |
|---|---|---|---|
| `Vote` | `mark` (string) | none (`data` null, `errors` null) | `not_in_room`; `validation_failed` (`field` = `mark`, `reason` = `empty` / `too_long` / `invalid_characters` / `unknown_card`); `observer_cannot_vote`; `voting_closed` |
| `ClearVotes` | none | none | `not_in_room` |
| `ForceShowVotes` | none | none | `not_in_room` |

`no_session` is handled at connect (401), as in slice 1. `observer_cannot_vote` and `voting_closed` are hub-only codes, added to `ErrorCodes` beside `not_in_room`; they have no REST status mapping. Revealing when votes are already revealed is a no-op success (two members pressing "Show votes" together is not an error).

### 2.3 Join response and vote status

`VoteStatus` (service, response, and TypeScript) gains two fields; it stays the one shape used by the join snapshot, `VoteStatusUpdate` and `VotesShown`:

```json
{
  "votes": { "<personGuid>": "hide" },
  "hasEveryoneVoted": false,
  "votingFinished": false,
  "countdown": null
}
```

- `votes`: guid -> mark. Masked as `"hide"` while `votingFinished` is false. When true, the real marks, observers excluded.
- `votingFinished`: true after the reveal until `ClearVotes`.
- `countdown`: seconds remaining while a countdown is running, otherwise `null`.

`JoinRoomResponse` gains `cards: [ { value, display } ]` for the room's card set (display null when the value is shown as is). A room's set is fixed in this slice (changing it is slice 4).

### 2.4 Events

Names unchanged from the old hub; payloads are typed DTOs in the Controller project, mirrored in `models/`. Sent with `RoomEventPublisher` only.

| Event | Payload | Sent when |
|---|---|---|
| `VoteCast` | `{ personGuid }` | A vote was stored or changed. To the whole group, caller included |
| `VoteStatusUpdate` | `VoteStatus` | After `VoteCast`. Whole group |
| `CountdownUpdate` | `{ countdown, reset: false }` | The countdown starts (N), and every second after (N-1 ... 1). Whole group |
| `VotesShown` | `VoteStatus` (`votingFinished` true) | Reveal by countdown or by `ForceShowVotes`. Whole group |
| `VotesCleared` | none | After `ClearVotes`. Whole group |
| `CountdownReset` | `{ countdown, reset: true }` | Slice 1 event, unchanged payload; now also cancels the countdown on the server |

`CountdownResetEvent` is generalised to `CountdownEvent` (same two fields) with one mapper. Order guarantees: `VoteCast` before `VoteStatusUpdate` before the first `CountdownUpdate`; `PersonExited` before any `CountdownUpdate` or `VotesShown` that the exit causes.

## 3. Service rules

### 3.1 Room state

`RoomEntity` gains `Revision` (long), `CountdownRevision` (long?) and `CountdownRemaining` (int). `VotingEnabled` and `Votes` already exist. `RoomEntityCopyMapper` copies the new fields.

- **Revision** is bumped, inside the room `Update`, by every change that cancels a countdown in the old code: a stored vote, `ClearVotes`, a reveal, a join that adds a non-observer while voting is open, and a person leaving. (Slice 4 adds person-type and settings changes.) A second tab, a reconnect and an observer joining do not bump it.
- A countdown **is running** when `CountdownRevision == Revision`. Starting one sets `CountdownRevision = Revision` and `CountdownRemaining = N`. Any later bump makes it stale at once, so cancelling needs no extra state and a stale tick can never reveal.

### 3.2 `IRoomService` additions (all decisions under the room lock)

- `Vote(personGuid, connectionId, mark)`: (1) person lock: resolve the room from `Connections` and read the person type; no room -> `not_in_room`; observer -> `observer_cannot_vote`. (2) room `Update`: the connection must be in `MemberConnections` (else `not_in_room`); `!VotingEnabled` -> `voting_closed`; mark must be in the room's card set; store the vote; bump `Revision`; return the revision. (3) no lock held: build the `VoteStatus` from the snapshot (member types via per-person locks, as `Join` does). (4) if everyone voted, a second room `Update` starts the countdown **only if `Revision` still equals the revision from step 2**; otherwise a newer operation owns the decision and this one stops. Returns `VoteResult { RoomId, PersonGuid, VoteStatus, CountdownStarted (N or none), RevealNow }`.
- `ClearVotes(personGuid, connectionId)`: one room `Update`: clear `Votes`, `VotingEnabled = true`, bump `Revision`.
- `ForceShowVotes(personGuid, connectionId)`: one room `Update`: if `VotingEnabled`, set false and bump `Revision`; already revealed -> success with `Revealed = false` (nothing published).
- `Join` additions: `ResetCountdown` (already computed) also bumps `Revision` in the join's room `Update` when it is true; the join snapshot's `VoteStatus` now reflects `VotingEnabled` and a running countdown.
- `Exit` addition: when `PersonLeft` and voting is open, the exit's room `Update` bumps `Revision`; then, outside the lock, the same everyone-voted check as `Vote` step 3 and 4 decides whether to start the countdown. `ExitRoomResult` gains `CountdownStarted`.
- **Everyone voted** keeps slice 1's rule (`voters.Count > 0 && all voted`); see decision 3.
- `RoomIdentifier`-style validation for the mark is a service validation: `VoteMarkValidation` (empty, over 20 characters, any of `< > " ' & \0 \r \n`, not in the room's card set, equal to the reserved `hide`). Card lookups come from a new `ICardSetService.GetCards(setValue)`.
- Cards: `Common/Constants/CardSets.cs` gains the card lists of the seven sets (value + display), moved verbatim from `PlanningPokerUi/Models/CardSets.cs`. `CardSetService` stays the single owner.

### 3.3 Countdown ticking: `IRoomTimerService`

A Service-layer singleton (the one reserved in phase-1 section 1.3). It owns only the 1-second ticker, built on `TimeProvider` so tests drive it with `FakeTimeProvider`.

- `Start(roomId, revision)`: replaces any ticker for the room with one tied to that revision. A `Start` with a lower revision than the current ticker is ignored.
- Each tick calls `IRoomService.Tick(roomId, revision)` (not under the timer's lock): one room `Update` that returns `Stale` if the countdown is no longer running; else decrements `CountdownRemaining`; at 0 sets `VotingEnabled = false`, bumps `Revision` and returns `Revealed` with the `VoteStatus`; otherwise returns `Remaining`. `Stale` and `Revealed` stop the ticker.
- Results reach the clients through `IRoomCountdownNotifier` (interface in the Service project, implemented by `RoomEventPublisher`, registered in DI) so the Service layer never references SignalR. Its methods: `CountdownStartedAsync` / `CountdownTickedAsync` (-> `CountdownUpdate`) and `VotesRevealedAsync` (-> `VotesShown`).
- Timer callbacks catch and log exceptions (`ILogger`); a failure stops that ticker only.
- Mappers, named after their output: `VoteResultMapper`, `VoteStatusResponseMapper` (exists, extended), `VoteCastEventMapper`, `CountdownEventMapper`, `CardResponseMapper`. `VoteStatusMapper` takes the room's `VotingEnabled` and the countdown.

### 3.4 Publisher and hub

`IRoomEventPublisher` gains `PublishVoteCastAsync(VoteResult)`, `PublishVotesClearedAsync(roomId)`, `PublishVotesShownAsync(roomId, VoteStatus)`. After a successful `Vote` the hub publishes `VoteCast` then `VoteStatusUpdate`, then calls `IRoomTimerService.Start` and sends the first `CountdownUpdate` (or, with countdown 0, `VotesShown` at once). After an exit, `PublishPersonExitedAsync` is followed by the same start/reveal publishing when `CountdownStarted`. The hub method bodies stay workflow only.

## 4. Angular pieces (`PlanningPoker.Client`)

- **Models**: `VoteStatus` gains `votingFinished` and `countdown`; `JoinRoomResponse.cards`; `CardOption`, `VoteCastEvent`, `CountdownEvent`; `HUB_EVENTS` gains `VoteCast`, `VoteStatusUpdate`, `CountdownUpdate`, `VotesShown`, `VotesCleared`.
- **`RoomStore`** (same buffering and generation rules as slice 1): new signals `cards`, `votingFinished`, `countdown`, `hasEveryoneVoted`, `myMark`. Join result sets them; events go through `apply()`, so they are queued while a join is in flight and replayed after its snapshot. `VoteStatusUpdate` and `VotesShown` replace the vote state; `VoteCast` adds a hidden entry for that person unless votes are revealed; `CountdownUpdate` sets `countdown`; `CountdownReset` and `VotesCleared` clear it; `VotesCleared` also clears `myMark` and the votes. Commands `vote(mark)`, `clearVotes()`, `showVotes()` invoke the hub, record `generation` before the call and drop the result if it changed; `vote` sets `myMark` immediately and restores the previous value if the hub returns an error. `PersonJoined.vote` and `PersonExited` keep updating the vote map (slice 1).
- **`features/room/card-picker`** (new, presentational): radio group of the room's cards, native radio semantics, PNG displays rendered as `<img alt=value>`, disabled when `votingFinished` or the person is an observer. Emits `markSelected`.
- **`features/room`**: toolbar adds "Clear votes" and "Show votes" (all members), the picker, and "Showing results in N!" when `countdown` is set. The participant table gains a mark column: `▮` for a hidden vote, the card's display for a revealed one, empty for no vote and for observers. Rows keep the slice 1 order.
- **Assets**: the six fist-of-five PNGs are copied from `PlanningPokerUi/wwwroot` into `PlanningPoker.Client/public` (copy only; the old project is not changed).
- **Styles**: cards, selected and disabled states, mark cells and the countdown line use the site colour variables so light and dark both work; Material tokens are not overridden. Phone layout per [#18](https://github.com/vlad-gaman/PlanningPoker/issues/18): cards wrap and stay tappable at 360px, no horizontal overflow.
- **Out of the client** in this slice: statistics, charts, fireworks, person type switch, room settings.

## 5. Tests (TDD: write each failing test first)

### 5.1 xUnit

- `CardSetService.GetCards` for all seven sets (values, displays, fallback unknown -> error).
- `VoteMarkValidation`: empty, 21 chars, each forbidden character, `hide`, not in card set, valid.
- `RoomService.Vote`: stores and replaces a vote; observer refused; revealed room refused; unknown connection `not_in_room`; countdown starts only when everyone voted; a stale revision at step 4 starts nothing.
- `RoomService`: `ClearVotes` reopens voting and bumps the revision; `ForceShowVotes` reveals once and is a no-op the second time; a non-observer join bumps the revision (and an observer or second tab does not); `Exit` removes the vote, bumps the revision and reports `CountdownStarted` when the rest have voted.
- `RoomService.Tick`: decrements, reveals at 0, returns `Stale` after any bump (vote, clear, join, exit).
- `RoomTimerService` with `FakeTimeProvider`: N, N-1 ... 1 then reveal; restart from N on a changed vote; a lower-revision `Start` is ignored; a stopped ticker sends nothing; an exception in a tick is logged and stops only that room.
- Concurrency via the `Interleaving*Repository` fakes: a join between a vote's step 2 and step 4; a clear between two ticks; two simultaneous `ForceShowVotes`; two last votes at once. In each, the room ends consistent and exactly one `VotesShown` is published.
- Mappers: every new or changed one (`VoteStatusMapper` incl. revealed and countdown, response and event mappers, `CardResponseMapper`).
- Hub (`RoomHubWorkflowTests` style): `Vote`, `ClearVotes`, `ForceShowVotes` success and each error; publisher call order; `FakeRoomEventPublisher` extended. `RoomEventPublisher`: payload per event, caller included for `VoteCast`.
- Integration (`RoomHubIntegrationTests` style): two connections vote, see `VoteCast` / `VoteStatusUpdate`, receive `CountdownUpdate` ticks and `VotesShown` with real marks; late joiner after reveal gets revealed marks and `cards`.

### 5.2 Vitest

- `RoomStore`: join snapshot (hidden, revealed, countdown running); each new event; events buffered during join are replayed after the snapshot; a command resolving after `close()` or a room switch changes nothing (generation guard); `vote` rolls back `myMark` on error.
- `card-picker`: renders cards and PNG images, disabled when revealed and for observers, emits the chosen mark.
- Room component: mark cell states (hidden, revealed, empty, observer), countdown line, buttons call the store.

### 5.3 Playwright

Three contexts, countdown 2 seconds in the test room settings:

1. A and B in a room; A picks a card: B sees `▮` on A's row, not the value.
2. B picks too: both see "Showing results in 2!", then the real marks; cards disabled.
3. "Clear votes": marks, countdown and selection are gone, cards enabled again.
4. A changes a vote during the countdown: the countdown restarts at 2.
5. "Show votes" with one vote reveals at once; an observer sees the marks and cannot vote.
6. A third person joins during the countdown: it is hidden for everyone and does not reveal until they vote.
7. A leaves while B had voted and A had not: the countdown starts for B.
8. A late joiner after the reveal sees the real marks with disabled cards.
9. Phone viewport (375x812): cards usable, no horizontal overflow. Dark mode: one screenshot check of the card picker and mark column.

## 6. Acceptance criteria

1. `dotnet test`, `ng test`, `ng build` and the Playwright flows above pass in CI.
2. No unused code: no `ShowVotes` hub method, no `HealthCheck`, no per-room `Timer` objects, no event or DTO without a consumer.
3. Swagger is unchanged for REST (no new endpoints); the hub contract in section 2 is the reference and is covered by tests.
4. Membership and countdown decisions are made only under the room lock; the person and room locks are never nested; all hub events go through `RoomEventPublisher`; client hub events are buffered during a join; async client steps are guarded by generation counters.
5. Light and dark mode and the 360px layout of the new styles were checked in a browser.
6. Items for slice 3 (statistics, charts, fireworks) are not present.

## 7. Decisions for review

Places where the old code is unclear or inconsistent, or the map is ambiguous. The spec makes a call; change it if you disagree.

1. **Slice boundary.** The map lists "voting" and "results" as separate slices; the old `VotesShown` payload mixes votes and statistics. The spec reveals marks in slice 2 and adds `statistics` in slice 3 (additive), so no statistics code exists unused for a slice. Person-type change, settings and ownership transfer stay in slice 4. If you want `ChangePersonType` here (it also re-evaluates "everyone voted"), it is small; say so.
2. **Countdown 0. Decided: allow 0-30.** The old form allowed 0-30 and the server revealed at once for 0; slice 1 enforced 1-30. This slice restores 0-30 (changes `RoomConstraints.CountdownMinSeconds`, `CreateRoomRequestValidation`, the home form and their tests). With 0, the vote that makes everyone voted, or an exit that leaves everyone voted, reveals at once: no ticker, no `CountdownUpdate`, just `VotesShown`.
3. **"Everyone voted" with no voters.** The old `DidEveryoneVote` is true when the room has only observers, so the last voter leaving, or an observer-only room, would auto-reveal an empty result. Slice 1's mapper returns false for that case; the spec keeps it (no empty reveal).
4. **Who may clear and show. Decided: any member.** The map says commands are owner-only "enforced", but the old UI shows "Clear votes" and "Show votes" to everyone and the hub does not check. The spec keeps any-member for these two (only settings and transfer are owner-only, slice 4).
5. **Card definitions. Decided: in the join response.** The new API exposes only set names. The spec sends the room's `cards` with the join response rather than a new endpoint, because the cards are only needed inside a room. Extends the contract from [#7](https://github.com/vlad-gaman/PlanningPoker/issues/7).
6. **Mark validation is stricter.** The old hub accepted any string up to 20 characters without `< > " ' & \0 \r \n`. The spec also requires the mark to be a card of the room's set and not the reserved `hide`. It only differs from the old behaviour for hand-crafted calls.
7. **Errors instead of silent ignores.** The old hub dropped votes from observers or after the reveal without a reply. The hub now returns `observer_cannot_vote` / `voting_closed`; the UI already prevents both, so users never see them. Repeated "Show votes" and "Clear votes" are no-op successes.
8. **Vote change restarts the countdown.** Old behaviour, kept (timer reset on each vote).
9. **Stale countdown in `VoteStatusUpdate`.** The old status carried the timer's last value (so the label could briefly show a stale number before the first `CountdownUpdate`). The spec sends `countdown` only while a countdown runs; no flash of a wrong number.
10. **Timer cleared on a second tab's disconnect.** Old code cleared the voting timer on any disconnect, even of a second tab. The spec cancels only when a person actually leaves (slice 1's `PersonLeft`).
11. **`VoteCast` plus `VoteStatusUpdate`.** The status already contains the voter, so `VoteCast` is redundant. The map fixes 12 typed events, so both stay; the client uses `VoteCast` as an incremental update and `VoteStatusUpdate` as the full replacement.
12. **Own selection after a refresh.** The old client does not restore the highlighted card after a reload or reconnect (own mark is masked, the radio is unchecked). Kept; the server does not send the caller their own mark.
13. **Leaver's vote after the reveal.** The old frozen result kept a leaver's vote after the reveal (invisible, since their row is gone). The spec derives revealed marks from the live votes, so a late joiner never sees it; slice 3 freezes the statistics at the reveal.
14. **Mark column for observers.** The old page had three tables (dev, test, observers; no mark column for observers). Slice 1 uses one table with a role column; the spec keeps that and leaves the mark cell empty for observers.
