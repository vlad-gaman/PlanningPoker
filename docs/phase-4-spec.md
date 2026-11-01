# Phase 4 spec: room management (slice 4)

Hand-off spec for slice 4 of the Angular migration of PlanningPoker: changing your person type inside a room, the owner's room settings (including the card set), and handing ownership to someone else. Source of truth for the *why* is the wayfinder map, [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` and the layer and concurrency rules in [`phase-1-spec.md`](phase-1-spec.md) (sections 1.3 and 2.3), [`phase-2-spec.md`](phase-2-spec.md) (sections 3.1 to 3.3) and [`phase-3-spec.md`](phase-3-spec.md) (section 3.1) apply unchanged.

Scope is **parity only**: no new features. Branch: `slice-4-room-management`, one pull request into `angular-migration`, reviewed by the owner, CI green before merge. The old `PlanningPokerUi` project is not touched.

## 0. Where this slice sits

[Issue #15](https://github.com/vlad-gaman/PlanningPoker/issues/15) orders the slices; this one is "room management". The boundary, from the slice 2 and 3 tables:

| In slice 4 (this spec) | Later |
|---|---|
| Hub `ChangePersonType` and event `PersonTypeChanged` | Theme control, credits page icons, footer link, shell polish: slice 5 |
| Hub `UpdateRoomConfiguration` (owner only), event `RoomConfigurationUpdated`, "clear votes if shown" | Cutover (parity checklist, merge to `main`): slice 6 |
| Hub `TransferOwnership` (owner only), reusing the slice 1 `OwnershipTransferred` event | |
| Room page: person-type control, owner-only Settings dialog with transfer ownership | |
| Settings changes and type changes cancel or restart the countdown (the `Revision` rule left open in slice 2) | |

Already in place: the per-room `Revision` / countdown machinery, `FollowUpAfterChange` and `Decide` in `RoomService`, the frozen `Revealed` input of the statistics, `OwnershipTransferred` (sent on owner exit since slice 1), the `not_room_owner` code (403 in the REST table, unused so far), `PersonTypes.IsValid`, `RoomConstraints`, the creation settings dialog and the `cards` list on the join response.

## 1. Old behaviour to match

From `RoomHub` (`ChangePersonType`, `UpdateRoomConfiguration`, `TransferOwnership`), `Room` (`UpdateConfiguration`, `TransferOwnership`), `_RoomSettings.cshtml` and `site.js` (`PersonTypeChanged`, `RoomConfigurationUpdated`, `OwnershipTransferred` handlers, `showRoomSettings`, `saveRoomSettings`):

**Person type**

- Any member can switch between developer, tester and observer with a radio group on the room page. Anything else is ignored.
- The person's type is set, then everyone gets `PersonTypeChanged { person, voteResultInfo }`; the row moves to its new table.
- While voting is open: if everyone (non-observers) has now voted, the reveal countdown starts (or reveals at once with countdown 0); otherwise a running countdown is cancelled. This is how an observer joining a stuck round, or a non-voter turning observer, can finish the round.
- After the reveal nothing else happens: the votes and the frozen statistics do not change (slice 3 decision 2).
- A voter who becomes an observer keeps their entry in the vote dictionary; observers are filtered out wherever votes are read, so switching back restores the mark.

**Room settings**

- The Settings button exists only for the owner. The dialog edits card set, countdown seconds (0 to 30, 0 = instant reveal), show fireworks and max participants (stored, never enforced, phase 1 question 1), and holds the transfer-ownership control.
- The hub method takes the whole configuration. **The old hub does not check that the caller is the owner**: only the button is hidden. The map says commands are owner-only "enforced".
- `Room.UpdateConfiguration` only applies fields that look set (card set non-empty, countdown >= 0, max participants > 0; fireworks always) and moves the timer's trigger count.
- Everyone gets `RoomConfigurationUpdated { configuration, updatedBy, cardSetDetails }`. Every client then updates its card list, and **every client that has the results visible calls `ClearVotes`** (so N clients clear N times). The card set may change at any time, also while votes are cast.

**Ownership**

- Only the current owner can transfer, to a person currently in the room. On success everyone gets `OwnershipTransferred { newOwner, message }`; the client only reads `newOwner` and shows or hides the Settings button for the right person.
- The old dropdown lists everyone but the caller; the hub itself accepts the caller as target (a silent self-transfer that still broadcasts).
- Automatic transfer when the owner leaves is slice 1 and unchanged.

## 2. Contract

### 2.1 REST

No new endpoints, Swagger unchanged for REST. `PUT /api/me` still changes the session person's type outside a room; nothing in this slice calls it from inside a room.

### 2.2 Hub methods on `/hubs/room`

All return the `ApiResponse` shape. As in slice 2 the caller's room comes from the connection index; there is no room argument.

| Method | Arguments | Success data | Errors |
|---|---|---|---|
| `ChangePersonType` | `personType` (`dev`, `test`, `obs`) | none | `not_in_room`; `validation_failed` (`field` = `personType`, `reason` as `UpdateMeRequestValidation`) |
| `UpdateRoomConfiguration` | `configuration { cardSet, countdownSeconds, showFireworks, maxParticipants }` | none | `not_in_room`; `not_room_owner`; `validation_failed` (`field` = the offending property, `reason` as the creation validation) |
| `TransferOwnership` | `targetPersonGuid` (string) | none | `not_in_room`; `not_room_owner`; `validation_failed` (`field` = `targetPersonGuid`, `reason` = `invalid_guid` / `not_a_member`) |

`not_room_owner` is the existing code; it is returned in the hub response like the other hub codes (the 403 in the REST table stays unused by REST). Repeating a change that changes nothing (same type, same ownership target as the current owner) is a no-op success and publishes nothing, as repeated "Show votes" is in slice 2.

### 2.3 Events

Names are the old hub's; payloads are typed DTOs in the Controller project mirrored in `models/`. Sent with `RoomEventPublisher` only.

| Event | Payload | Sent when |
|---|---|---|
| `PersonTypeChanged` (new) | `{ person: { guid, name, personType }, voteStatus }` | A member's type changed. Whole group, caller included |
| `RoomConfigurationUpdated` (new) | `{ configuration, cards: [ { value, display } ], updatedBy, voteStatus }` | The configuration was saved. Whole group, caller included |
| `VotesCleared` (slice 2) | none | After `RoomConfigurationUpdated` when the save cleared the votes (decision 2) |
| `OwnershipTransferred` (slice 1) | `{ newOwner }` unchanged | Whole group, caller included |
| `CountdownUpdate` / `VotesShown` (slices 2, 3) | unchanged | When the type change leaves everyone voted (follow-up, section 3.1) |

- `voteStatus` in the first two is the same `VoteStatus` as everywhere (decision 4); its `hasEveryoneVoted` and `countdown` tell the client that a running countdown was cancelled, so there is no extra `CountdownReset` for these causes.
- `updatedBy` is the owner's display name (the old client logged it and showed nothing; it stays in the payload only if the client shows it, see decision 8).
- Order guarantees: `PersonTypeChanged` before any `CountdownUpdate` or `VotesShown` it causes; `RoomConfigurationUpdated` before the `VotesCleared` it causes (the card list is new before the selection is reset).
- `HubEvents` gains the two constants (twelve typed events in total, as the map fixes).

## 3. Service rules

### 3.1 `ChangePersonType(personGuid, connectionId, personType)`

The person type lives behind the person lock and the countdown decision behind the room lock, which are never nested. Steps:

1. resolve the caller's room from the connection (`not_in_room`), validate the type;
2. person repository `Update`: set `PersonType`. Same value -> no-op success, nothing published. This is the source of truth read by the reveal freeze (phase 3, section 3.1), so a reveal after this point sees the new type;
3. room `Update`: if `VotingEnabled`, bump `Revision` (cancels a running countdown, slice 2 section 3.1). After the reveal nothing changes: `Revealed`, the statistics and `Revision` stay;
4. outside any lock, the follow-up of `Exit`: `FollowUpAfterChange` (snapshot, `Decide`): everyone voted -> start the countdown or, with countdown 0, reveal at once; otherwise nothing. The previous countdown was already cancelled in step 3, so it restarts from N, as in the old code;
5. the hub publishes `PersonTypeChanged` (status read after step 3, so `countdown` is null), then the follow-up.

A reveal that raced between steps 2 and 3 is harmless: it froze with the old type, the type change then finds `VotingEnabled` false and does nothing more (the old app had the same ordering freedom). A voter who turns observer keeps their stored vote (decision 5); `VoteStatusMapper` already excludes observers from `votes` and from `hasEveryoneVoted`.

### 3.2 `UpdateRoomConfiguration(personGuid, connectionId, configuration)`

1. resolve the room, `not_in_room`; the caller must be the owner (`not_room_owner`), checked inside the room `Update` so a concurrent transfer cannot slip through;
2. validate with one `RoomConfigurationValidation` shared with room creation (Controller project, request shape): card set exists, countdown 0 to 30, max participants >= 1. The old partial-update rules are dropped: the request is the whole configuration (decision 3);
3. one room `Update`: replace `Configuration`; bump `Revision` when voting is open (cancels the countdown, the rule slice 2 deferred to this slice); **clear the votes** (`Votes` emptied, `Revealed` null, `VotingEnabled` true, `Revision` bumped) when the votes are currently shown (the old rule) or the card set changed (decision 2);
4. outside the lock, when the votes were not cleared and voting is open: `FollowUpAfterChange`, so a round that was complete restarts its countdown with the new length (decision 6);
5. publish `RoomConfigurationUpdated { configuration, cards, updatedBy, voteStatus }`, then `VotesCleared` if step 3 cleared, then the follow-up.

The configuration is also what `JoinRoomResponse` carries, so a later joiner sees the new values; `cards` on the join response already follows `Configuration.CardSet`. Nothing is kept of the old timer's `MaxTriggers`: the countdown length is read from the room's configuration when it starts (`CountdownStart.Seconds`).

### 3.3 `TransferOwnership(personGuid, connectionId, targetPersonGuid)`

One room `Update`: owner check (`not_room_owner`), target must be in `Members` (`not_a_member`); set `OwnerGuid`. Target already the owner (this includes the caller) -> no-op success, nothing published. Publish `OwnershipTransferred` with the same mapper as the owner-exit transfer; the payload does not change.

### 3.4 Mappers and validations

New mappers, named after their output: `PersonTypeChangedEventMapper`, `RoomConfigurationUpdatedEventMapper`, `RoomConfigurationUpdatedEventMapper` reuses the existing `RoomConfigurationRequestMapper` for the request DTO and the room's configuration response mapper. Validations: `RoomConfigurationValidation` (Controller, request shape, shared with `CreateRoomRequestValidation`, which delegates to it), `PersonTypeValidation` (Controller), `TransferTargetValidation` (guid format, Controller); the owner check lives inside `RoomService` as `not_room_owner`. All registered in dependency injection. `RoomConstraints` is the single place for the countdown and max participants bounds.

### 3.5 Publisher and hub

`RoomEventPublisher` gains `PublishPersonTypeChangedAsync`, `PublishRoomConfigurationUpdatedAsync` (it also publishes the optional `VotesCleared`) and `PublishOwnershipTransferredAsync` (new; it sends the same event through the same mapper that `PublishPersonExitedAsync` uses for the owner-exit transfer). Hub bodies stay workflow only: session person, service call, publish, map to `ApiResponse`.

## 4. Angular pieces (`PlanningPoker.Client`)

- **Models and hub client**: `PersonTypeChangedEvent`, `RoomConfigurationUpdatedEvent`; `RoomHubClient` gains `changePersonType`, `updateRoomConfiguration`, `transferOwnership` and the two event streams.
- **`RoomStore`**: `PersonTypeChanged` updates the person's row (the table is one list with a role column, slice 1) and replaces the vote status; `RoomConfigurationUpdated` replaces `configuration` and `cards`, clears the selected card when it is not in the new set or when a `VotesCleared` follows, and replaces the vote status; `OwnershipTransferred` already sets `ownerGuid`. `revealCount` is not bumped by any of these (fireworks only on a live `VotesShown`). Generation guards unchanged; buffered events during a join replay in order.
- **`features/room`**: a person-type control (three-way radio, same labels as the name entry) bound to the caller's own type; changing it calls `changePersonType` and the control follows the store, not the click. A "Settings" button shown only when `ownerGuid` is the caller's guid, so it appears and disappears on `OwnershipTransferred` without a reload.
- **Settings dialog**: the creation dialog `home/room-settings-dialog` becomes a shared `RoomSettingsDialog` in `shared/` with two modes. Create mode is unchanged. Room mode drops the fun-name field, prefills from `configuration`, saves with `updateRoomConfiguration`, shows the server's `validation_failed` errors inline and closes on success; it adds a "Transfer ownership" section: a select of the other members and a button, disabled with no choice. Full-screen on phones per [#18](https://github.com/vlad-gaman/PlanningPoker/issues/18).
- **Removed on the client side**: nothing; the old jQuery handlers are not touched. The client no longer calls `ClearVotes` when settings change (the server clears once).
- **Styles**: the person-type control, the Settings button and the dialog sections use the site colour variables so light and dark both work; 360 px layout with no horizontal overflow.
- **Out of the client**: theme control, credits icons, footer link (slice 5).

## 5. Tests (TDD: write each failing test first)

### 5.1 xUnit

- `RoomService.ChangePersonType`: sets the type; same type is a no-op; `not_in_room`; invalid type; voting open + complete round starts the countdown (or reveals at once with 0); incomplete round cancels a running countdown (stale tick returns `Stale`); after the reveal `Revealed`, statistics and `Revision` are untouched; a voter turning observer keeps the stored vote, a returning observer shows it again; a reveal racing with the change (via an `Interleaving*Repository` fake).
- `RoomService.UpdateRoomConfiguration`: owner only (`not_room_owner`, also after a concurrent transfer); every validation row; replaces the configuration; clears revealed votes; clears open votes on a card-set change but not on another change; bumps `Revision` and cancels a running countdown; restarts the countdown with the new length when the round was complete; join response and later `Tick` use the new countdown.
- `RoomService.TransferOwnership`: owner only; target not a member; invalid guid; self or current owner is a no-op; success changes `OwnerGuid`; the next owner exit transfers again as in slice 1.
- Validations and mappers: every new or changed one, including the shared `RoomConfigurationValidation` through `CreateRoomRequestValidation`.
- Hub: each method's success and each error row; `RoomEventPublisher` payloads and the order guarantees of section 2.3.
- Integration (`RoomHubIntegrationTests` style): two connections; B switches to observer and the round completes; the owner changes the card set after a reveal and both see `RoomConfigurationUpdated` then `VotesCleared`; a non-owner is refused; ownership moves and the new owner can save settings.

### 5.2 Vitest

- `RoomStore`: each of the three events (rows, ownership, configuration, cards, selection reset, status replacement, `revealCount` untouched), in order with buffering during a join.
- Room component: person-type control follows the store; Settings button only for the owner and follows an ownership change.
- `RoomSettingsDialog`: create mode unchanged; room mode prefill, validation messages, server errors, save payload, transfer select lists the other members only and is disabled with no choice.
- `RoomHubClient` fake: new methods and streams.

### 5.3 Playwright

Two or three contexts, countdown 2 seconds:

1. A votes, B (not yet voted) switches to observer: the countdown starts and the reveal follows; B switching back after the reveal changes nothing in the charts.
2. The owner changes the card set after a reveal: results disappear for everyone, the new cards appear, votes are open again.
3. The owner changes the countdown to 0: the next complete round reveals at once.
4. A non-owner has no Settings button; the owner transfers ownership to B: A's button disappears, B's appears, B can save settings.
5. Invalid settings (countdown 31) show the inline error and keep the dialog open.
6. Dark mode and phone viewport (375x812): the dialog fits, no horizontal overflow.

## 6. Acceptance criteria

1. `dotnet test`, `ng test`, `ng build` and the Playwright flows above pass in CI.
2. No unused code; no hub method, event or constant without a consumer; no partial-update path left from the old `UpdateConfiguration`.
3. Swagger unchanged for REST; the hub contract in section 2 is covered by tests.
4. Person and room locks are never nested; every decision (owner check, clear, bump) happens in one room `Update`; all hub events go through `RoomEventPublisher`; client async steps remain generation-guarded.
5. Light and dark mode and the 360 px layout of the new UI were checked in a browser.
6. Settings and transfer are refused for a non-owner on the server, not just hidden.
7. Items for slice 5 (theme control, credits icons, footer link) are not present.

## 7. Open questions for review

Each has a recommendation; the spec above already follows it. Change it if you disagree.

1. **Owner-only is enforced server-side. Recommended.** The old hub trusts the hidden button for settings (and checks only transfer). The map says owner-only commands are enforced, so `UpdateRoomConfiguration` returns `not_room_owner`. Clear and show votes stay any-member (slice 2 decision 4).
2. **Clear votes on a card-set change, not only when shown. Recommended.** Old: clears only when the results are visible, so a card-set change while voting is open leaves marks from the old set in `Votes`. Slice 2 validates marks against the room's set and slice 3 orders the charts by the set's card order, so stale marks would break both at the reveal. Clearing on a card-set change is the smallest consistent rule; alternative is rejecting card-set changes while votes exist (a new error, worse UX).
3. **Whole configuration, no partial update. Recommended.** Old: fields that "look unset" are skipped, which cannot be expressed in the new typed request and lets `maxParticipants: 0` through silently. The client always sends all four fields, validated like creation. Consequence: max participants must be >= 1 (the old dialog said 2 to 100, but only in HTML attributes) and the countdown is 0 to 30 (settled in slice 2 decision 2).
4. **`voteStatus` in both new events. Recommended.** Old `PersonTypeChanged` carried it; `RoomConfigurationUpdated` did not. A countdown can be cancelled by either, and the client already knows how to hide it from `VoteStatus.countdown`; the alternative is an extra `CountdownReset` for these causes.
5. **A voter turning observer keeps the stored vote. Recommended (parity).** The mapper hides it while they are an observer; switching back restores it. Alternative: drop the vote on the switch, which is cleaner but a visible change (a mis-click would cost the vote).
6. **Settings change restarts a completed round's countdown. Recommended.** Old: the running timer was left alone and only its length was updated, which could never take effect for the running countdown. The spec cancels (the slice 2 `Revision` rule) and, because everyone had voted, immediately restarts with the new length so the round is never stuck. Clearing makes this moot.
7. **No-op changes publish nothing. Recommended.** Old: switching to the type you already have, or transferring to yourself, still broadcast (and the type switch restarted the countdown). Not observable by users who use the UI, which prevents both.
8. **`updatedBy` and the transfer message.** Old payloads carried them; the old client only logged `updatedBy` and ignored the transfer message. Recommended: drop the transfer message, keep `updatedBy` only if you want a "Settings updated by X" toast (no toast existed, so the default is to drop it too). Say if you want the toast.
9. **Dialog shared with creation. Recommended.** One `RoomSettingsDialog` with a mode, because the fields, bounds and messages are the same. Alternative is two components, which duplicates the form.
10. **Issue [#23](https://github.com/vlad-gaman/PlanningPoker/issues/23)** (second tab shows no selected card). Not part of this slice; but this slice resets or replaces the selected card on a card-set change, so the "own mark in the join response" fix, if done, should land before or together with the card-set code in the store. Recommended: do #23 as its own small change first.
