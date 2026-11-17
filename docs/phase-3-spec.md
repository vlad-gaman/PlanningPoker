# Phase 3 spec: results (slice 3)

Hand-off spec for slice 3 of the Angular migration of PlanningPoker: the statistics shown once votes are revealed, the three charts, the average marks and the fireworks. Source of truth for the *why* is the wayfinder map, [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` and the layer and concurrency rules in [`phase-1-spec.md`](phase-1-spec.md) (sections 1.3 and 2.3) and [`phase-2-spec.md`](phase-2-spec.md) (sections 3.1 to 3.3) apply unchanged.

Scope is **parity only**: no new features. Branch: `slice-3-results`, one pull request into `angular-migration`, reviewed by the owner, CI green before merge. The old `PlanningPokerUi` project is not touched.

## 0. Where this slice sits

[Issue #15](https://github.com/vlad-gaman/PlanningPoker/issues/15) orders the slices; this one is "results". The boundary, from the slice 2 table:

| In slice 3 (this spec) | Later |
|---|---|
| `VotesShown` / join snapshot gain `statistics` (additive) | `ChangePersonType`, `UpdateRoomConfiguration` and "clear votes if shown", `TransferOwnership`: slice 4 |
| Statistics model, frozen at the reveal | Theme control, credits page content (icons), footer link, shell: slice 5 |
| Three charts (all, developers, testers), average marks | |
| Fireworks on consensus (honouring `showFireworks`) | |
| Credits: attributions for the libraries this slice adds | |

Slice 2 already provides: the reveal (`VotingEnabled` false, `Revision` bump), `VoteStatus` (`votes`, `hasEveryoneVoted`, `votingFinished`, `countdown`), `VotesShown`, `VotesCleared`, the card lists with display values, `RoomConfiguration.showFireworks` (stored since slice 1, editable in the creation dialog) and the PNG assets.

## 1. Old behaviour to match

From `Room.GenerateResultsAndStatistics`, `Room.GetCurrentStatus`, `RoomHub` / `IndexController` (the two reveal call sites), `Room.cshtml` and `site.js` (`setVoteResultInfo`, `setStatistics`, `createChart`, fireworks):

**Statistics model**

- **What is counted**: the votes of non-observers in the room at the reveal. Observers never vote. Three groups: *all*, *developers* (`dev`), *testers* (`test`).
- **Marks**: per group, one entry per distinct mark that was cast, with `percentage = count / votes in the group * 100`. A mark nobody picked has no entry (no empty bars). An empty group has an empty list.
- **Order**: numeric marks ascending, then `?`, then `coffee`, then anything else (t-shirt sizes, traffic lights) in an arbitrary order (enumeration order of a concurrent dictionary, not defined by the code).
- **Highest mark**: per group, the mark with the strictly highest percentage. A tie for the top gives *no* highest mark (nothing is highlighted).
- **Average**: per group, the mean of the votes whose mark parses as a number (`decimal.TryParse`, so `0`, `0.5`, `13`; fist-of-five `0` to `5`). `?`, `coffee`, t-shirt sizes and traffic lights are skipped. If no vote in the group is numeric the average is null. The average is **not rounded** by the server, and the client prints it with `|| ""`, so an average of exactly 0 shows blank (JavaScript falsy) and a repeating decimal shows many digits.
- **Frozen at the reveal**: the result object is built once, stored on the room and returned unchanged until `ClearVotes`. A late joiner or reconnecting client gets it in the join snapshot (`voteResultInfo` includes `statistics`). A person who leaves after the reveal does not change it (their vote stays counted in the charts; their row is gone).
- Statistics are present only after the reveal. While voting is open, the status has only hidden marks and no statistics.

**Charts** (Chart.js 2.9, `<canvas>` each, shown only once votes are revealed, hidden by `VotesCleared`)

- Three bar charts: "All votes", "Developers votes", "Testers votes", each followed by "Average: x". All three are shown even when a group is empty (empty chart, blank average).
- X axis: the marks as labels, font 16 px, using the card display (`½`, `☕`, emoji); for the fist-of-five set the label text is hidden and the card's PNG is drawn (24 px) under the axis. Y axis: hidden ticks, starts at 0, suggested max 100, no grid lines. No legend. Tooltip: `<value>%`.
- Bar colour: the highest mark in `--chart-color-success`, all other marks in `--chart-color-primary`. Axis text uses `--text-color`, the tooltip uses `--bg-color`/`--text-color`. A theme change recolours the charts without a reload.
- `VotesCleared` empties the three charts and hides the section.

**Fireworks** (`fireworks-js` 2.x over a full-page `.fireworks` overlay, `pointer-events: none`)

- Trigger, evaluated in `setVoteResultInfo` whenever a status with `votingFinished` arrives: *all* group has exactly one mark at 100 %, **and** `hasEveryoneVoted`, **and** more than one row in the developer and tester tables. Then, once per second for 10 seconds, `launch(10)` (skipped while the browser tab is hidden). Starting fireworks stops any running ones first.
- Stopped by: "Clear votes" click, "Show votes" click, `VotesCleared`.
- The room setting `showFireworks` is stored and editable but **the old client never reads it** (the trigger above ignores it).
- The trigger also runs for the join snapshot of a late joiner or reconnect (same function), so a refresh after a consensus reveal launches fireworks again.
- No sounds anywhere in the old app.

## 2. Contract

### 2.1 REST

No new endpoints, Swagger unchanged for REST.

### 2.2 Hub

No new methods or events. `VoteStatus` gains `statistics`; it stays the one shape used by the join snapshot, `VoteStatusUpdate` and `VotesShown`:

```json
{
  "votes": { "<personGuid>": "3" },
  "hasEveryoneVoted": true,
  "votingFinished": true,
  "countdown": null,
  "statistics": {
    "all":        { "marks": [ { "mark": "3", "percentage": 66.7 }, { "mark": "?", "percentage": 33.3 } ], "highestMark": "3", "averageMark": 3 },
    "developers": { "marks": [ { "mark": "3", "percentage": 100 } ], "highestMark": "3", "averageMark": 3 },
    "testers":    { "marks": [ { "mark": "?", "percentage": 100 } ], "highestMark": "?", "averageMark": null },
    "consensus": false
  }
}
```

- `statistics` is `null` unless `votingFinished` is true (so `VoteStatusUpdate`, and the join snapshot of an open room, carry `null`). Set by `VotesShown`, and by the join snapshot of a revealed room.
- `marks`: ordered as in decision 4. `percentage`: 0 to 100, rounded to one decimal (decision 6). `highestMark`: the unique top mark, else `null`. `averageMark`: number or `null` (decision 5).
- `consensus` is the fireworks rule minus the client-only parts: true when the reveal had at least two votes, a single distinct mark across all of them, and `hasEveryoneVoted`. The client adds `showFireworks` and the live-reveal condition (decisions 8 and 9).
- Statistics are built in the Service layer (`VoteStatistics`, `MarkShare`, `GroupStatistics`) and mapped to Controller response DTOs by mappers named after their output (`StatisticsResponseMapper`, `GroupStatisticsResponseMapper`, `MarkShareResponseMapper`). `VoteStatusResponseMapper` is extended.

### 2.3 Error codes

None added.

## 3. Service rules

### 3.1 Freeze at the reveal

`RoomEntity` gains `RevealedVotes` (person GUID -> `RevealedVoteEntity { Mark, PersonType }`), null while voting is open. It is the frozen input of the statistics; the statistics themselves are computed from it by a pure `IVoteStatisticsCalculator` (singleton, interface, takes `IReadOnlyCollection<RevealedVote>` plus the card order of the room's set) and mapped by `VoteStatisticsMapper` when a `VoteStatus` is built. Freezing the input rather than the result keeps the Repository layer free of service models and makes the calculator trivially testable.

Person types live behind the per-person lock, which must never be nested inside the room lock. So the three reveal paths (`ForceShowVotes`, `Tick` reaching 0, and the zero-second countdown reveal in `Vote` and `Exit`) share one routine:

1. outside any lock, read the `PersonType` of the room's current members through the person repository (as `Join` already does for the vote status);
2. in one room `Update`: if `VotingEnabled`, set it false, bump `Revision`, and set `RevealedVotes` from `Votes` joined with the types from step 1, **excluding observers**; if a voter has no type from step 1 (they joined between the two steps), return `NeedTypes` without changing anything and the caller repeats from step 1 (at most 3 attempts, then `internal_error`);
3. the stale checks stay as in slice 2: `Tick` still returns `Stale` unless the countdown is running, a changed `Revision` still wins.

`ClearVotes` sets `RevealedVotes` to null in its existing room `Update`. `RoomEntityCopyMapper` copies the new field deeply.

`VoteStatus` for a finished room is built from `RevealedVotes` for `statistics` only; `votes` keep their slice 2 meaning (live votes of current members, decision 13 of phase 2). A leaver's vote therefore still counts in the charts and not in the rows, exactly as in the old app.

### 3.2 Calculator rules

- Group *all* = every `RevealedVote`; *developers* = type `dev`; *testers* = type `test`.
- `marks`: distinct marks present, ordered by position in the room's card set (decision 4), percentage rounded to one decimal, away from zero, invariant culture.
- `highestMark`: the single mark with the maximum count, else null.
- `averageMark`: mean over marks that parse as `decimal` with `NumberStyles.Number` and `CultureInfo.InvariantCulture`, rounded to two decimals; null when none parse (decision 5).
- `consensus` as in 2.2.

### 3.3 Publisher and hub

No new publisher methods: `PublishVotesShownAsync` and the join snapshot carry the extended `VoteStatus`. The hub bodies stay workflow only.

## 4. Angular pieces (`PlanningPoker.Client`)

- **Models**: `VoteStatus.statistics: VoteStatistics | null`; `VoteStatistics`, `GroupStatistics`, `MarkShare`.
- **`RoomStore`**: new signal `statistics` (set by the join snapshot and `VotesShown`; cleared by `VotesCleared`, `CountdownReset` does not touch it) and `revealCount`, a counter the store bumps only when a **live** `VotesShown` event is applied (not the join snapshot, decision 9). Fireworks run from an effect on `revealCount` plus `statistics().consensus` plus `configuration().showFireworks`.
- **`features/room/results`** (new, presentational): shows "All votes", "Developers votes", "Testers votes", each a `results-chart` followed by "Average: x" (blank when null, `0` shown as `0`). Hidden while `statistics` is null.
- **`features/room/results/results-chart`** (new): a thin component around Chart.js 4 (`chart.js`, tree-shaken registration of bar controller, category and linear scales, tooltip). Same look as the old one: bars, hidden y axis, 16 px x labels from the card display, PNG cards drawn under the axis by a small plugin, tooltip `<value>%`, highest bar `--chart-color-success`, others `--chart-color-primary`. Colours are read from the CSS variables and the chart is recoloured when `ThemeService`'s effective theme changes. Destroys the chart on destroy. `role="img"` with an `aria-label` that lists the marks and percentages (text alternative for the canvas; no visual change).
- **`features/room/fireworks`** (new): wraps `fireworks-js` over a fixed full-page overlay (`pointer-events: none`). `start()` stops any running show, then `launch(10)` once per second for 10 seconds, skipped while `document.hidden`; `stop()` clears the interval and the show. The room component calls `stop()` on "Clear votes", "Show votes", `VotesCleared` and destroy. It is a service-free component with an injected `FireworksLauncher` thin wrapper so tests fake the library.
- **`features/credits`**: stays the slice 0 stub plus a "Libraries" section listing Chart.js (MIT, chartjs.org) and fireworks-js (MIT, crashmax-dev/fireworks-js) with links. The icons list and the footer link are slice 5; the data lives in one `credits.data.ts` list slice 5 extends.
- **Styles**: the results section, chart wrapper and overlay use the site colour variables (`--chart-color-primary`, `--chart-color-success`, `--text-color`, `--bg-color`) so light and dark both work, as `[data-theme="dark"] canvas` did in the old CSS. The two chart colours exist in `styles.scss` since slice 0; if not, move them from `site.css`. Phone layout per [#18](https://github.com/vlad-gaman/PlanningPoker/issues/18): charts stack below the table at 360 px, no horizontal overflow.
- **Dependencies**: `chart.js` and `fireworks-js` from npm (pinned), bundled; the old CDN scripts are not used. Both are MIT; no images or sounds are added.
- **Out of the client** in this slice: sounds (the old app has none), person type switch, room settings dialog changes.

## 5. Tests (TDD: write each failing test first)

### 5.1 xUnit

- `VoteStatisticsCalculator`: single mark; several marks with the card-set order (`0`, `½`, `13`, `?`, `coffee`; t-shirt `XS`..`XXL`; traffic lights); percentages and rounding (1/3 -> 33.3, 2/3 -> 66.7); unique highest; tied top -> null; all/dev/test grouping; observers excluded; empty group; average over numeric marks only; average 0 is 0 not null; no numeric marks -> null; 0.5 handling; invariant culture under a `de-DE` current culture; consensus true/false (one voter, one mark but not everyone voted, two marks).
- `RoomService`: `ForceShowVotes`, `Tick` and the zero-second paths freeze `RevealedVotes` (types and marks, observers out); a leaver after the reveal does not change them; `ClearVotes` nulls them; join snapshot of a revealed room carries statistics, of an open room `null`; `NeedTypes` retry when a voter joins between steps 1 and 2 (via an `Interleaving*Repository` fake); two simultaneous reveals produce one frozen set and one `VotesShown`.
- Mappers: every new or changed one, including `VoteStatusResponseMapper` with and without statistics.
- Hub: `VotesShown` and the join result carry the statistics; `RoomEventPublisher` payload.
- Integration (`RoomHubIntegrationTests` style): two connections vote and see `VotesShown` with the expected statistics; a late joiner gets the same statistics; after `ClearVotes` a fresh round starts with `statistics` null.

### 5.2 Vitest

- `RoomStore`: snapshot sets/clears `statistics`; `VotesShown` sets it and bumps `revealCount`; the join snapshot does not bump it; `VotesCleared` clears it; buffered events during a join replay in order; generation guards unchanged.
- `results`: hidden without statistics; three sections; blank vs `0` vs value for the average.
- `results-chart`: builds the dataset (labels, percentages, highest-mark colour, tie -> all primary); recolours on a theme change; destroys on destroy; aria-label text; PNG marks use the image labels (Chart.js faked).
- Fireworks controller: runs ten launches one second apart, skips when hidden, `stop()` ends them, a second start restarts; the effect starts only when `consensus`, `showFireworks` and a live reveal coincide.
- `credits`: lists both libraries with links.

### 5.3 Playwright

Two or three contexts, countdown 2 seconds:

1. A and B vote differently, the reveal shows three charts and the averages; "Clear votes" removes them.
2. A and B vote the same: the fireworks overlay appears (canvas present); "Clear votes" removes it. With `showFireworks` off in the room settings, no overlay.
3. Fist-of-five room: the chart shows images, no text labels.
4. A late joiner after the reveal sees the same charts and no fireworks.
5. Dark mode: screenshot check of results and one chart; phone viewport (375x812): no horizontal overflow.

## 6. Acceptance criteria

1. `dotnet test`, `ng test`, `ng build` and the Playwright flows above pass in CI.
2. No unused code; no `statistics` field without a consumer; no CDN script.
3. Swagger unchanged for REST; the hub contract in section 2 is covered by tests.
4. Person and room locks are never nested; the reveal's decision and the freeze happen in one room `Update` with the revision re-checked there; all hub events go through `RoomEventPublisher`; client async steps remain generation-guarded.
5. Light and dark mode and the 360 px layout of the new styles were checked in a browser.
6. Credits lists the added libraries.
7. Items for slice 4 (person type, settings, ownership) are not present.

## 7. Open questions for review

Each has a recommendation; the spec above already follows it. Change it if you disagree.

1. **Reveal freeze (slice 2 question 13). Recommended: freeze the statistics inputs at the reveal.** Old behaviour: stats are frozen, so a leaver's vote stays in the charts, and a late joiner sees the same numbers. The spec keeps that (3.1). Alternative: recompute from live votes, so a leaver disappears from the charts; closer to the rows, but the numbers then change after a reveal, which the old app never did.
2. **Person type changed after the reveal (slice 4).** Frozen types, so the charts do not move. Slice 4 decides whether a type change still clears votes; nothing to do here.
3. **Statistics wire shape.** Old: flat (`marks`, `marksDev`, `marksTest`, ...). Recommended: nested `all` / `developers` / `testers` groups, since the contract is new and only our own client reads it. Alternative: keep the flat names.
4. **Order of marks. Recommended: card-set order.** The old order is numeric, `?`, `coffee`, then arbitrary; card-set order is identical for every numeric set (they already end `?`, `coffee`) and gives a defined order for t-shirt sizes and traffic lights. Small deviation, only where the old order was undefined.
5. **Average formatting. Recommended: round to two decimals, show `0` for 0.** Old: unrounded repeating decimals and a blank for exactly 0 (a falsy-value bug). Null still shows blank.
6. **Percentage formatting. Recommended: one decimal on the wire** (33.3), tooltip `33.3%`. Old: raw 33.333333333333336 in the tooltip.
7. **Chart library. Recommended: Chart.js 4 from npm**, same look, bundled instead of the 2.9 CDN build. Alternative: plain CSS/SVG bars (no dependency, simpler theming and accessibility, but a new look for the PNG labels). Say if you prefer it.
8. **`showFireworks`. Recommended: honour it.** The old client ignores the room setting although the dialog offers it ("Show fireworks on consensus"). Honouring is the evident intent; ignoring it means a checkbox that does nothing. If you want strict parity, drop the setting check (one line).
9. **Fireworks on refresh. Recommended: only on a live reveal.** Old: a reconnect or late join into a consensus reveal launches fireworks again. The spec fires only when `VotesShown` arrives live, not from the join snapshot.
10. **Fireworks minimum voters.** Old: more than one row in the dev and tester tables (counted client-side, including people who had not voted when `hasEveryoneVoted` is true anyway). The spec uses at least two voters server-side (`consensus`), which gives the same result in every case where everyone voted.
11. **Observers in statistics.** Excluded, as in the old app. Observers see the charts.
12. **Credits scope.** Only the libraries added now (Chart.js, fireworks-js) go on the stub page; the icons list and the footer link stay in slice 5. No sound or image attributions are needed since the slice adds none.
13. **Issue [#23](https://github.com/vlad-gaman/PlanningPoker/issues/23)** (second tab shows no selected card). Does **not** fall out of this slice: statistics reveal nothing about the caller's own mark while voting is open, which is the case the issue is about. After a reveal the second tab could read its own mark from `votes[myGuid]`, but that is a partial fix with different behaviour before and after the reveal. Recommended: leave it for its own change (include the caller's own mark in the join response).
14. **Accessibility text alternative for the charts** (`aria-label`) is small and invisible; it is the only addition beyond parity. Drop it if you want zero additions.
