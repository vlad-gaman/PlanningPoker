# UI inventory: old Razor UI and new Angular client

- Ticket: GitHub issue #29, "Inventory screens, states and styling of the old and new UI" (https://github.com/vlad-gaman/PlanningPoker/issues/29)
- Map: GitHub issue #27, "Make the Angular UI look the same as the Razor UI" (https://github.com/vlad-gaman/PlanningPoker/issues/27)
- Captured: 2026-11-01
- Scope: facts only. No decisions, no fixes, no screenshots (everything is read from source; nothing was run in a browser).

## Where the sources live

The branch this file is committed on (`research/ui-inventory`, cut from `main` at `8b67596`) contains only the **old Razor UI**. The **new Angular client** and the **phase specs** exist only on `slice-5-shell-and-credits` (the newest slice branch; `angular-migration` holds only the slice 0 client). They were read with `git archive` from that branch; no branch was checked out or changed.

Two old Razor revisions exist, and they differ a lot. The Razor UI on `main` is older than the one the Angular specs describe. Anything marked [O5] is **not on main**.

## Source key

Paths are repo-relative.

- [M] Old Razor UI on `main` (`8b67596`): `PlanningPokerUi/...`
- [O5] Old Razor UI on `slice-5-shell-and-credits` (the later revision the phase specs cite, e.g. `Pages/Credits.cshtml`, `Pages/Shared/_RoomSettings.cshtml`)
- [N] New Angular client on `slice-5-shell-and-credits`: `PlanningPoker.Client/...`
- [P1] to [P5] Phase specs `docs/phase-1-spec.md` to `docs/phase-5-spec.md` on `slice-5-shell-and-credits`
- [V] Versions: `PlanningPoker.Client/package.json` [N]; `PlanningPokerUi/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` and `.../js/bootstrap.bundle.min.js` (Bootstrap v5.1.3) [M]; `PlanningPokerUi/wwwroot/lib/jquery` (jQuery v3.5.1) [M]; `PlanningPokerUi/PlanningPokerUi.csproj` (`net6.0`) [M]
- [S] Server card sets: `PlanningPoker.Common/Constants/CardSets.cs` on `slice-5-shell-and-credits`

---

## 1. Screens and states

| # | Screen or state | Old, main [M] | Old revision [O5] | New client [N] |
|---|---|---|---|---|
| 1 | Home: name, role, create, join | `Pages/Index.cshtml`: "Your Name:" text input (minlength 2, maxlength 20); role radios Dev / Tester / Observer; "Use fun room name?:" checkbox with the text "Caution! It may generate weird names!!!"; "Create room" submit; "Room Name:" input and "Join room" submit. Page title "Planning Poker". No `h1`. | Same fields, plus a "More Settings" button that opens the creation panel; the caution text sits under the fun-name checkbox; a `TempData` error div (`#server-error`). [O5] `Pages/Index.cshtml` | `features/home/home.html`: `h1` "Planning Poker"; Material outline fields "Your name" and "Role" (select: Developer / Tester / Observer); flat "Create room" button; "Room name or link" field and stroked "Join room" button. No fun-name checkbox on this page (it is in the dialog). |
| 2 | Create-room settings | None. Card set is fixed in `ViewModels/RoomViewModel.cs` (0, ½, 1, 2, 3, 5, 8, 13, 20, 40, 100, ?, coffee). Countdown fixed at 5 s by `Models/MyTimer.cs` (`MaxTriggers = 5`). No fireworks or participant setting. [M] | `Pages/Shared/_RoomCreationSettings.cshtml`: fixed-position panel: Card Set select; Countdown 0 to 30 (default 5); Use fun room name; Show fireworks (checked); Max Participants 2 to 100 (default 50); "Apply Settings" and "Cancel". [O5] | `features/home/room-settings-dialog.html` in create mode (Material dialog): Card set select (the 7 sets from the API); Countdown (seconds) 0 to 30; Maximum participants (min 1); Show fireworks; "Use a fun room name" (default checked, `useFunRoomName: true`); "Cancel" and "Create room". Opens from "Create room" after the name is valid. |
| 3 | Join from home | "Room Name:" input (minlength 2) and "Join room" posts to `/JoinRoom`, which redirects to `/Room/{name}`. [M] `Controllers/IndexController.cs` | Same, plus an inline `#room-name-error`. [O5] | "Room name or link" field; `extractRoomId()` accepts a bare id or a pasted link; "Enter a room name or link." (mat-error). `features/home/home.ts`, `home.html` |
| 4 | Room entry: no name yet | Separate view `Pages/Shared/RoomJoin.cshtml`: "Room:" with the id, "Your Name:", role radios, "Join room". [M] | Same view. [O5] | Name entry inside `/room/:id` (`room.html`, phase `name`): text "Enter your name to join this room.", name and role Material fields, "Join room" flat button. |
| 5 | Room loading | Server-rendered; no loading state. [M] | Same. [O5] | Phase `loading` has no template case: only the heading "Room {id}" renders while it waits (`features/room/room.html`, `room.ts`). |
| 6 | Room, empty (joined, no votes) | `Pages/Shared/Room.cshtml`: "Room:" with the GUID; "Clear votes" and "Show votes" buttons; role radios; card radios; three empty tables (Developers, Testers, Observers). Countdown and statistics hidden on load (`wwwroot/js/site.js`, `document.ready`). [M] | Same, plus the Settings button for the owner. [O5] | "Room {id}" heading; card picker enabled; participant table with Name / Role / Vote headers (`aria-label="Participants"`); "Clear votes" and "Show votes"; "Your role" radio group; "Leave room". No countdown, no results. |
| 7 | Room, voting (some votes cast, not revealed) | A voter's row shows the hidden marker (U+25AE, a filled rectangle) from the `AVote` message; own card selected by its radio. [M] | Same via `VoteCast`. [O5] | `markOf()` shows the hidden marker for `hide` while not finished; the selected card has the `.selected` background (`card-picker.scss`). |
| 8 | Room, countdown running | `#show-votes-countdown`: "Showing results in " + N + "!" (`Countdown` message; hidden on reset). [M] | Same. [O5] | `<p class="countdown" role="status">Showing results in {{ seconds }}!</p>`, bold, primary colour (`room.html`, `room.scss`). Shown while `countdown` is non-null. |
| 9 | Room, revealed | Statistics block: canvases "All votes", "Developers votes", "Testers votes", each with "Average:". Cards disabled (`disableVoting(true)`). "Show votes" is never disabled in `site.js`. [M] | Same. [O5] | `app-results` beside the table: three `figure.chart` boxes (1px border, 6px radius, 10rem canvas), each with "Average: x". Cards disabled. "Show votes" disabled once `votingFinished` (`room.html`). |
| 10 | Revealed with consensus (fireworks) | `site.js` `setVoteResultInfo` / `setStatistics`: all votes on one mark at 100%, everyone voted, and more than one row in the dev and tester tables. Then 10 launches of 10 rockets, one per second. The room's `showFireworks` setting is never read. Also fires on a join snapshot (refresh). [M] | Trigger evaluated in the same function; `showFireworks` gate not verified in code. [O5] | `FireworksOverlay`: 10 rounds, 1 s apart, 10 rockets per round, removed 5 s after the last round (`fireworks-overlay.ts`). Starts only on a live `VotesShown` with `statistics.consensus` and `configuration.showFireworks` (`room.ts`). Stops on clear, show, leave. |
| 11 | Room, observer | Observers table, no mark column. Radios still clickable (no client gate; server ignores the vote). [M] | Same. [O5] | Card picker disabled for observers (`isObserver`); mark cell empty; the role radio can still change type. |
| 12 | Room, owner | No owner concept in the UI. [M] | Settings button shown only when `Model.IsCurrentUserOwner` (`Pages/Shared/Room.cshtml`, line 15). [O5] | "(owner)" after the name in the table (`room.html`); "Settings" button only for the owner (`isOwner`). |
| 13 | Room, non-owner | No owner UI. [M] | No Settings button. [O5] | No Settings button; "Leave room" shown to everyone. |
| 14 | Room settings (edit) | None. [M] | Fixed-position panel `_RoomSettings.cshtml`, 400 px wide, centred, with an "×" close button (CSS for a `.room-settings-overlay` also exists in `site.css`; its use in markup was not checked). Card Set; Countdown 0 to 30 with "Set to 0 for instant reveal when everyone votes"; Show fireworks; Max Participants 2 to 100; transfer section; "Save Settings" and "Cancel". [O5] | `RoomSettingsDialog` in room mode (Material dialog, title "Room settings"): Card set; Countdown 0 to 30 ("Use 0 to 30 seconds."); Maximum participants ("At least 1 participant."); Show fireworks; "Cancel" and "Save settings". Server validation errors shown inline (`role="alert"`). |
| 15 | Ownership transfer | None. [M] | Inside the settings panel: a select "Select person to transfer ownership to..." and a warning-coloured "Transfer Ownership" button. [O5] | Inside the same dialog, below the fields: "New owner" select listing the other members, and a stroked "Transfer ownership" button disabled until a choice is made; the section is hidden with no other members. The owner label moves on `OwnershipTransferred`. |
| 16 | Role change in the room | `_PersonType` radios in the toolbar above the cards: Dev / Tester / Observer. [M] | Same. [O5] | "Your role" radio group under the table (`room.html`), follows the store, not the click. |
| 17 | Leave room | None (navigate away). [M] | None. [O5] | "Leave room" stroked button returns to `/` (`room.ts`, `room-store.ts` `leave()`). |
| 18 | Credits | None. The footer "Privacy" link points to `/Privacy`, which has no page in `Pages/`. [M] | `Pages/Credits.cshtml`: title "Credits & Attributions"; card "Icons" with seven Freepik / Flaticon links; "Back to Home" button. [O5] | `features/credits/credits.html`: `h1` "Credits &amp; Attributions"; cards "Icons" (seven links) and "Libraries" (Chart.js, fireworks-js, both MIT); flat "Back to Home" button. Data in `credits.data.ts`. |
| 19 | Error and not-found | `Pages/Error.cshtml`: "Error." and "An error occurred while processing your request." in red, plus development-only text. A room that does not exist: `RedirectPermanent("/")` with no message (`Controllers/RoomController.cs`). [M] | `#server-error` text on home; `alert()` for a closed room and failed settings save (`site.js`). [O5] | No error page component. Unknown room: redirect to `/` with a notice "That room does not exist (any more)." Room closed: "The room was closed because everyone left." Unknown route: redirect to `/` (`app.routes.ts`). API errors shown inline with `role="alert"`. |
| 20 | Connection status and reconnect | Server message `Disconnected` sends the browser to `/` (`site.js`). No status UI. [M] | Fixed bottom-right pill `.connection-status`: green when connected, yellow and pulsing when reconnecting, red and pulsing when disconnected (`site.css`). [O5] | No connection status UI (none in any template). Hub auto-reconnect re-runs `JoinRoom` silently; a failed rejoin shows a notice and leaves (`room-store.ts`, `rejoin()`/`abandon()`). |
| 21 | Light mode | White only. `site.css` has no theme variables. [M] | Theme variables on `:root` and `[data-theme="dark"]`, plus `prefers-color-scheme` rules. [O5] | `styles.scss` `:root` variables; pre-paint script in `index.html`; `ThemeService` with auto / light / dark stored as `pp-theme`. [N] |
| 22 | Dark mode | None. [M] | Dark variables; dark image rules (card and chart images, a light backing for finger images); select option colours. [O5] | Same palette values as [O5]; card and mark images get a light backing (`card-picker.scss`, `room.scss`); chart colours re-read on theme change (`results-chart.ts`). |
| 23 | Phone (below 768 px) | Bootstrap grid `col-sm-12 col-md-6` stacks the two room columns below 768 px. `html` font-size 14 px below 768 px, 16 px from 768 px (`site.css`). [M] | Same grid and font rules. [O5] | No `@media` rule anywhere in `PlanningPoker.Client/src` (grep). Layout relies on flex wrap (`.shell`, `.layout`, `.cards`, `.actions`). The settings dialog has only `min-width: 18rem` (no full-screen rule). [P2] §4 and [P4] §4 say phone layouts and a full-screen dialog; the code does not contain them. |

---

## 2. Visual building blocks

### 2.1 Components

- Old, main [M]: Bootstrap 5.1.3 navbar, container, grid, table and spacing (`m-1`) classes. `form-row` is not defined by the vendored Bootstrap 5.1.3 CSS; its only rule is `position: relative; z-index: 0` in `site.css`. `noselect` is defined only in `site.css`. Radios styled as buttons through `div.table label`. jQuery handlers. Raw WebSocket (`/ws/{guid}`), not SignalR. Chart.js 2.9.4 and fireworks-js 2.x from CDN (`Pages/Shared/Room.cshtml`).
- Old revision, [O5]: adds Bootstrap buttons (`btn`, `btn-primary`, `btn-secondary`, `btn-warning`), `form-control`, `form-check`, `card`, a panel for settings, a connection status pill. SignalR 7.0.0 from CDN (`Pages/Shared/Room.cshtml`).
- Bootstrap's navbar toggler: the markup uses `data-toggle` (Bootstrap 4). The vendored Bootstrap 5.1.3 bundle reads `data-bs-toggle` only (0 `data-toggle` matches, 8 `data-bs-toggle` matches in `bootstrap.bundle.min.js`), so the toggler has no effect. [P5] §1 says the same.
- New [N]: Angular Material 22.2.5 (`@angular/material` `^22.2.5`) for: `MatButton` (flat, stroked, text), `MatFormField` (outline) with `MatInput` and `MatError`, `MatSelect`, `MatCheckbox`, `MatDialog`, `MatTable` [N] `package.json`, `src/app/**/*.ts` imports. Native radio group (`app-card-picker`), native select (`app-theme-select`), Chart.js 4.6.1 (`chart.js` `4.6.1`) behind `ChartFactory`, fireworks-js 2.10.8, `@microsoft/signalr` `^10.0.11` (`package.json`). Bootstrap is not used ([P1] §1.4).

### 2.2 Fonts

- Old, main [M]: Bootstrap's sans-serif stack (`--bs-font-sans-serif: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Noto Sans", "Liberation Sans", sans-serif, plus emoji fonts`), body `1rem`, line-height `1.5` (`bootstrap.min.css`). Root size 14 px below 768 px and 16 px from 768 px (`site.css`).
- Old revision [O5]: no font-family rule found in `site.css`; same Bootstrap stack.
- New [N]: `body { font-family: Roboto, 'Helvetica Neue', sans-serif }` (`styles.scss`). `index.html` loads no font file, so Roboto applies only where installed locally. Material typography is set to Roboto in `mat.theme` (`styles.scss`). No root font size is set.

### 2.3 Spacing and sizes

| Item | Old, main [M] | New [N] |
|---|---|---|
| Page container | Bootstrap `.container`, max-width 1140 px (`bootstrap.min.css`) | `.shell`, max-width 1140 px (`app.scss`); `.page` margin 2rem auto, padding 0 1rem (`styles.scss`) |
| Navbar and footer padding | Bootstrap navbar classes | `.navbar`, `.footer`: 0.5rem 1rem (`app.scss`) |
| Gaps and helpers | `m-1` (0.25rem), `.form-row` no spacing rule | gaps 0.25rem to 1.5rem; `.actions` gap 0.5rem; `.role` gap 1rem; `.layout` gap 1.5rem (`room.scss`) |
| Column widths | Grid 50% from 768 px | `.main` max 40rem; `app-results` min 18rem; `.room` max 64rem (`room.scss`) |
| Home and dialog | Auto | `.home` max 28rem; dialog `min-width: 18rem` (`home.scss`, `room-settings-dialog.scss`) |
| Credits | No page on main. [O5] uses Bootstrap `col-md-8` | `.credits` max 44rem; sections padded 1rem 1.25rem (`credits.scss`) |
| Card buttons | `div.table label`: 4em wide, 1.8em vertical padding, 10px radius, 1px #212529 border, 2px margin [M] | `.card`: min 3rem square, 1px `--card-border`, 6px radius, 1.25rem text, 0.5rem gap (`card-picker.scss`) |
| Chart box | Canvas of Chart.js default size [M] | `.canvas-box` height 10rem; chart padding 0.75rem; 6px radius (`results-chart.scss`, `results.scss`) |
| Footer | Absolute, `line-height: 60px`, body `margin-bottom: 60px` [M] | Flex column, `min-height: 100vh`, footer in flow (`app.scss`) |

### 2.4 Colours

Light and dark values are identical in [N] `styles.scss` and [O5] `site.css`. [M] has only the light values, plus the hard-coded colours listed below.

| Token | Light | Dark | Used for |
|---|---|---|---|
| `--bg-color` | #ffffff | #1a1a1a | page background |
| `--text-color` | #212529 | #e9ecef | text |
| `--border-color` | #e5e5e5 | #404040 | navbar and footer rules, dividers |
| `--card-bg` | #ffffff | #2d2d2d | cards, table, select |
| `--card-hover-bg` | rgba(0,0,0,0.075) | rgba(255,255,255,0.1) | hover |
| `--card-selected-bg` | rgba(0,0,0,0.3) | rgba(255,255,255,0.3) | selected card |
| `--card-disabled-bg` | rgba(0,0,0,0.1) | rgba(255,255,255,0.1) | disabled cards |
| `--card-border` | #a8a8a8 | #565656 | card border |
| `--primary-color` | #1b6ec2 | #4dabf7 | links, countdown, notice border, focus ring |
| `--primary-hover` | #1861ac | #339af0 | link hover |
| `--shadow-color` | rgba(0,0,0,0.05) | rgba(0,0,0,0.3) | navbar shadow |
| `--navbar-bg`, `--footer-bg` | #ffffff | #1a1a1a | bars |
| `--chart-color-primary` | #007bff | #4dabf7 | bars |
| `--chart-color-success` | #28a745 | #51cf66 | highest bar |

- Old, main [M] hard-coded colours: links #0366d6; `.btn-primary` #1b6ec2 with border #1861ac; borders #e5e5e5; shadow `rgba(0,0,0,.05)`; checked person-type label `lightgray`; chart bars named `green` (highest) and `blue` (others) in `site.js` `setIndividualStatistics`. The main `site.css` uses no variables.
- New [N] Material mapping (`styles.scss`): `--mat-sys-primary`, `--mat-sys-background`, `--mat-sys-surface`, `--mat-sys-on-background`, `--mat-sys-on-surface`, `--mat-sys-outline`, `--mat-sys-outline-variant` point at the site variables. `--mat-sys-error` is not mapped: `.form-error` uses Material's default error colour for the azure palette (`color: var(--mat-sys-error)`). Palette: `mat.$azure-palette`, density 0.

### 2.5 Icons and images

- Old, main [M]: favicon set (`favicon.ico`, 16 and 32 px PNG, 192 and 512 px android icons, `apple-touch-icon.png`). Text glyphs in `site.js`: coffee ☕ (U+2615), half ½ (U+00BD), hidden marker ▮ (U+25AE). No finger PNGs in `wwwroot` (the fist-of-five set is not offered on main).
- Old revision, [O5]: adds a palette emoji for the theme control, a gear emoji on the Settings buttons, six finger PNGs (`wwwroot/*-finger-512x512.png`) shown at 40 px (`.card-image`), and card images drawn under chart axes (`drawImagesOnChart`).
- New, [N]: favicon set copied into `public/`; logo `favicon-32x32.png` at 28 by 28 px in the navbar; palette emoji in `theme-select.html`; card display text from the server (`CardSets.cs`, [S]) for the text sets (including emoji cards: green, yellow, red circles for traffic lights), and PNG names for fist of five; card images 2.25rem (36 px) in the picker, 1.5rem (24 px) in the table, 24 px chart labels (`chart-config.ts`); Buy Me a Coffee logo, remote SVG at 12 by 12 px in the footer. No icon font and no `mat-icon`.
- Attributions: Freepik / Flaticon sets (poker, fist, finger, two, three, four, fingers) on the Credits page [O5] and [N] (`credits.data.ts`).

### 2.6 Motion

| Motion | Old, main [M] | Old revision [O5] | New [N] |
|---|---|---|---|
| Card / label hover | none (no transition) | `transition: all 0.3s ease` on buttons and labels | Hover background on cards, no transition (`card-picker.scss`) |
| Theme control | n/a | background and border transitions | `transition: background 0.3s, border-color 0.3s` (`theme-select.scss`) |
| Footer coffee icon | n/a | hover, transition 0.2s | hover `scale(1.05)`, `opacity .9`, 0.2s (`app.scss`) |
| Connection status | n/a | pulse 1.5 s (opacity 1 to 0.6 to 1) when disconnected or reconnecting | none (no status UI) |
| Chart | Chart.js 2.9.4; `site.js` does not set `animation` (library default applies) | same | `animation: false` (`chart-config.ts`) |
| Fireworks | 10 rounds of 10 rockets, 1 s apart; skipped when the tab is hidden (`site.js`) | same | 10 rounds, 1 s apart, 10 per round, 5 s linger; skipped when `document.hidden` (`fireworks-overlay.ts`) |

---

## 3. Side-by-side: obvious differences

| Area | Old, main [M] | Old revision [O5] | New client [N] |
|---|---|---|---|
| Look | Bootstrap 5.1.3 defaults, white | Same plus site palette | Own palette; Material outline fields and buttons |
| Name in the brand | "PlanningPokerUi" text | Logo and "PlanningPokerUi" | Logo and "Planning Poker" |
| Theme | None | Select with palette emoji: Auto, Light, Dark | Same control (native select), stored as `pp-theme` |
| Dark mode | None | Yes | Yes |
| Home heading | None | None | `h1` "Planning Poker" |
| Form fields | Native inputs with "Label:" text | Native inputs | Material outline fields with floating labels |
| Role choice on home | Radio buttons | Radio buttons | Select (Developer / Tester / Observer) |
| Fun room name | Inline checkbox with caution text | In the creation panel, with caution text | In the dialog; no caution text |
| Card set | One fixed set | Choice in the panel | Choice in the dialog (7 sets) |
| Countdown | Fixed 5 s | 0 to 30 s, set in the panel | 0 to 30 s, set in the dialog |
| Fireworks setting | None | Show fireworks checkbox | Show fireworks checkbox; honoured on a live consensus reveal |
| Card buttons | About 64 px wide, 10 px radius, text | Same, with finger PNGs at 40 px | 48 px minimum, 6 px radius, 1.25rem text, focus ring; finger PNGs at 36 px |
| People layout | Three tables: Developers, Testers, Observers | Same | One table: Name / Role / Vote, "(owner)" tag |
| Room buttons | "Clear votes", "Show votes" | Plus a gear "Settings" for the owner | "Clear votes", "Show votes" (disabled after reveal), "Settings" (owner), "Leave room" (all) |
| Settings | None | Fixed-position panel with overlay | Material dialog, shared with creation |
| Ownership transfer | None | In the settings panel | In the settings dialog, disabled until a choice |
| Results charts | Chart.js 2.9.4 from CDN; green and blue bars | Chart.js 2.9.4; site variables | Chart.js 4.6.1 bundled; site variables; no animation |
| Averages | Unrounded (spec [P3] §1 describes the old values; main `site.js` writes the raw value) | Same | Server rounds to 2 decimals; 0 shows 0 (`VoteStatisticsMapper.cs`) |
| Mark order in charts | Numeric, then ?, coffee, then unordered | Same | Card-set order ([P3] §7 #4) |
| Connection status | None | Pill bottom-right | None |
| Unknown room | Silent redirect to `/` | Error text or alert | Redirect to `/` with a notice |
| Error page | Default error page text | Default | None; inline errors |
| Credits | None (broken Privacy link) | Icons only | Icons and Libraries |
| Leave | None | None | "Leave room" button ([P1] §2.5) |
| Page titles | "Planning Poker - PlanningPokerUi" | Same | "Page - Planning Poker" (`page-title.strategy.ts`) |
| Phone layout | Grid stacks at 768 px | Same | Flex wrap; no media queries |

---

## 4. Deliberate differences introduced in slices 1 to 5

The phase specs state these as calls the spec made. The "Spec" column gives the section where each is stated, so it can be checked against the wording there.

**Framework-level (from the map and slice 0)**

| Difference | Spec |
|---|---|
| Bootstrap dropped; Angular Material for standard controls | [P1] §1.4 |
| Colour variables moved to the global stylesheet; Material tokens mapped to them; one `ThemeService` | [P1] §1.5 |

**Slice 1: create and join**

| Difference | Spec |
|---|---|
| Name entry inside the room route; the old app used a separate `RoomJoin` view | [P1] §2.1, §2.5 |
| Unknown room: redirect to `/` with a message (old redirect was silent on main) | [P1] §2.1 |
| Name length enforced 2 to 20 with `validation_failed` (old: browser attribute, server truncation) | [P1] §3 #3 |
| Non-ASCII names stored as typed; the old server rewrote them to `%uXXXX` | [P1] §3 #2 |
| Max participants stored but not enforced (kept; listed for completeness) | [P1] §3 #1 |
| Create-room settings in a Material dialog | [P1] §2.5 |
| One participant table with a role column (old: three tables) | [P2] §7 #14 |
| Hub reconnect re-runs `JoinRoom`; the old health check is removed | [P1] §2.5 |
| "Leave room" button returns to `/` | [P1] §2.5 |
| Old links `Room/:id` and `Credits` redirect to the new routes | [P1] §1.4 |

**Slice 2: voting**

| Difference | Spec |
|---|---|
| Countdown 0 to 30 (the old form allowed 0 to 30; the old main fixed 5 s) | [P2] §7 #2 |
| Cards sent with the join response, not a new endpoint | [P2] §7 #5 |
| Marks checked against the room's card set and the reserved `hide` | [P2] §7 #6 |
| Observer or post-reveal vote returns an error, not a silent drop (UI prevents both) | [P2] §7 #7 |
| Countdown status sent only while running (no stale number) | [P2] §7 #9 |
| Timer cancelled only when a person leaves (not when a second tab closes) | [P2] §7 #10 |
| Own selected card not restored after a refresh (kept from old) | [P2] §7 #12 |
| Card picker is a native radio group; PNG cards as `<img alt=value>` | [P2] §4 |

**Slice 3: results**

| Difference | Spec |
|---|---|
| Chart.js 4.6.1 from npm, bundled, instead of the 2.9.4 CDN build | [P3] §7 #7 |
| Marks ordered by card set, not numeric / ? / coffee | [P3] §7 #4 |
| Averages rounded to 2 decimals; 0 shows 0 (code: `VoteStatisticsMapper.cs`) | [P3] §7 #5 |
| Percentages one decimal on the wire | [P3] §7 #6 |
| Nested statistics shape (`all`, `developers`, `testers`) | [P3] §7 #3 |
| Fireworks honour `showFireworks` | [P3] §7 #8 |
| Fireworks only on a live reveal, not a join snapshot | [P3] §7 #9 |
| Consensus needs at least two votes, checked on the server (`MinimumConsensusVotes`) | [P3] §7 #10 |
| Chart canvas has `role="img"` and an aria-label listing the marks (no visual change) | [P3] §4 |
| Chart animation off (`animation: false`); not stated in the spec | Code only: `chart-config.ts` |

**Slice 4: room management**

| Difference | Spec |
|---|---|
| Settings update checked on the server for the owner (spec: the old hub trusted the hidden button and checked only transfer) | [P4] §7 #1 |
| Clear votes when the card set changes (old cleared only when results showed) | [P4] §7 #2 |
| Whole configuration sent every time; max participants at least 1 (old dialog said 2 to 100 in HTML only) | [P4] §7 #3 |
| `voteStatus` in both new events | [P4] §7 #4 |
| Transfer message and `updatedBy` dropped | [P4] §7 #8 |
| No-op changes publish nothing | [P4] §7 #7 |
| One settings dialog for create and room; fun-name field only in create mode | [P4] §7 #9, §4 |
| Client no longer calls `ClearVotes` after a settings change (old: every client did) | [P4] §4 |
| Voter who turns observer keeps their stored vote (kept from old) | [P4] §7 #5 |
| Settings dialog full-screen on phones | [P4] §4 (not in the code; see row 23) |

**Slice 5: shell and credits**

| Difference | Spec |
|---|---|
| Product name "Planning Poker", not "PlanningPokerUi" | [P5] §6 #2 |
| Copyright year from the clock (old: hard-coded 2025) | [P5] §6 #3 |
| Theme preference not migrated from `planningpoker_theme_preference` (new key `pp-theme`) | [P5] §6 #4 |
| Buy Me a Coffee logo stays a remote image (kept from old) | [P5] §6 #5 |
| Standards-conforming head tags (no `image/x-icon` on PNG, no `-precomposed`) | [P5] §6 #6 |
| No global dark-mode image filter; only card and mark images get treatment | [P5] §6 #7 |
| No web app manifest | [P5] §6 #8 |
| Per-route page titles through a `TitleStrategy` | [P5] §6 #9 |
| Navbar collapse toggler not carried over | [P5] §1 |
| Credits page keeps the Libraries section from slice 3 | [P3] §4, [P5] §3 |

---

## 5. What was not checked

- Nothing was run in a browser: no screenshots, no pixel measurements, no phone check. Statements about looks come from CSS and templates.
- The tag `razor-final` named in the map does not exist; `git tag -l` lists `staging-1` to `staging-4` only.
- The Razor UI on `main` and the revision on `slice-5-shell-and-credits` disagree. Which one is "the old UI" for issue #27 is a decision for the owner; this file reports both.
- `angular-migration` holds only the slice 0 client; the new-client facts above come from `slice-5-shell-and-credits`.
