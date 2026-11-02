# Phase 7 spec: visual parity with the Razor UI (slices 6a to 6d)

Hand-off spec for four slices, 6a to 6d, that make the Angular client look the same as the old Razor UI, ahead of the cutover (slice 6, [phase-6-spec.md](phase-6-spec.md)). Source of truth for the *why* is the wayfinder map, [issue #27](https://github.com/vlad-gaman/PlanningPoker/issues/27); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` apply (light and dark CSS together, credits page via the footer link, a test per method, one class per file).

Scope is **what the user sees**: layout, colour, type, spacing, icons, motion, charts, phone. No REST, hub or Swagger change, and no behaviour change. Branches: `slice-6a-parity-foundation`, `slice-6b-parity-home-dialogs`, `slice-6c-parity-room`, `slice-6d-parity-shell-phone`, each one pull request into `angular-migration`, in that order, CI green, reviewed by the owner.

## 0. Where these slices sit

Slices 0 to 5 are built. The cutover (slice 6) stays last and **starts only after 6a to 6d are merged and every row of `docs/parity-checklist.md` is ticked or carries an accepted-difference note**. This map overrides the pre-accepted differences of [issue #17](https://github.com/vlad-gaman/PlanningPoker/issues/17) and reopens the "Bootstrap dropped, Material for standard controls" part of [issue #8](https://github.com/vlad-gaman/PlanningPoker/issues/8).

## 1. Decisions

| Topic | Decision | Ticket |
|---|---|---|
| Reference | The old UI is the Razor app at tag `razor-final` (`83e40aa` on `angular-migration`), run on :1333 (net8.0) beside the new app on :5000. Commands are in [that ticket](https://github.com/vlad-gaman/PlanningPoker/issues/28). `main` is not the reference. | [#28](https://github.com/vlad-gaman/PlanningPoker/issues/28) |
| Styling approach | **Keep Angular Material and bend it** with design tokens (`--mat-button-outlined-*`, `--mat-table-*`, `--mat-button-toggle-*`), density and a thin override layer. No Bootstrap, no old `site.css`. The prototype is on the throwaway branch `prototype/styling-approach` (`variant-b.css`, `home.scss`); it is reference only and is never merged. Known cost: some card and button rules need `!important` where Material's styles win. | [#30](https://github.com/vlad-gaman/PlanningPoker/issues/30) |
| Automation | Automate capture, image pairs and computed-style probes, run locally. The owner judges the look. Run-to-run noise is 0.00% (old) and at most 0.25% (new), so a pixel threshold near 0.5% separates noise from change. Pixel diff is reported, never a gate. | [#31](https://github.com/vlad-gaman/PlanningPoker/issues/31) |
| Acceptance bar | Section 2. | [#32](https://github.com/vlad-gaman/PlanningPoker/issues/32) |
| Gap list | Section 5. | [#33](https://github.com/vlad-gaman/PlanningPoker/issues/33) |

## 2. The bar: what "the same" means

Applied per screen and state, in light and dark, desktop and phone.

- **Layout:** same structure, order, alignment and centring. Page height within 5% of the old page at each size.
- **Colour:** exact CSS values, including dark mode.
- **Type:** same family, and the same size, weight and colour per element. The old page uses `system-ui`.
- **Spacing and radii:** same px value, 2px slack.
- **Icons and images:** same glyph, size and position.
- **Charts:** same type, colours and labels. The Chart.js version may differ.
- **Motion:** what the old app had is restored and counts: chart animation, hover, card selection, fireworks timing.
- **Phone:** same stacking and wrapping at 375px and the same 768px breakpoint behaviour.

**Accepted differences** (kept; each is recorded in the checklist as an accepted-difference note, and none is reverted): one participant table with a Role column instead of three tables; Material dialogs for create and room settings, and a full-screen dialog on phones; name entry inside the room route; "Planning Poker" name and the copyright year from the clock; native theme select; marks ordered by card set; no global dark-mode image filter; no navbar collapse toggler; the Libraries section on credits. Behaviour changes from slices 1 to 5 are outside the bar. What exactly each accepted item covers (route, structure or look) is settled in section 8, questions 2 and 3.

## 3. Where things live

Recommended, and open question 4 in section 8:

| What | Path |
|---|---|
| Checklist | `docs/parity-checklist.md`: one row per inventory state × light/dark × desktop/phone, columns *automated result*, *owner tick*, *accepted-difference note* |
| Old reference shots and style data | `docs/parity/reference/<state>-<size>-<scheme>.png` and `.json`, committed once from `razor-final` |
| Script | `PlanningPoker.Client/parity/` (a Node script on the Playwright already in the client, plus `pixelmatch` and `pngjs` as dev dependencies), not part of the CI suite |
| Run command | `npm run parity` in `PlanningPoker.Client` (starts nothing; the new app is served by `dotnet run --project PlanningPoker.Api`, the old app only when regenerating references); output goes to `PlanningPoker.Client/parity/out/` (git-ignored): new shot, old | new | diff image, style diff table |

The prototype scripts (`prototype-compare/` on `prototype/styling-approach`, commit `41e2030`: `run.cjs`, `gaps.cjs`, `montage.cjs`) are the starting point. They drive three browser contexts per app (owner Alice, Bob dev voting 5, Carol tester voting 8), mask the room id, and probe per visible text the font, size, weight, colour, background, alignment, radius, padding and box; old and new are paired by text.

## 4. Slices

Each slice ends with the owner ticking that slice's rows in `docs/parity-checklist.md`; a row that fails is fixed or gets an accepted-difference note. Every slice: light and dark CSS together; computed-style assertions in the comparison script for the old values of the rows it covers; a unit test for any method it adds or changes (for example `ThemeService`, chart config, a mapper); existing `ng test`, `ng lint` and Playwright stay green.

| Slice | Scope | Rows of the inventory table (`docs/research/ui-inventory.md`, branch `research/ui-inventory`) |
|---|---|---|
| **6a Foundation** | Global tokens and the tooling (section 5, 6a). | 21, 22 (both themes); cross-cutting |
| **6b Home and dialogs** | Home, join, both dialogs, errors and notices. | 1, 2, 3, 4, 14, 15, 19 |
| **6c Room and results** | The room in all its states, results, charts, motion. | 5, 6, 7, 8, 9, 10, 11, 12, 13, 16, 17, 20 |
| **6d Shell, credits and phone** | Header, footer, credits, the 768px breakpoint, the phone pass. | 18, 23; header and footer on every screen |

Rows 1 to 23 are the inventory's screens and states. The probe so far covered rows 1, 2, 4, 6, 7, 9, 12, 14, 18, 21, 22 and 23 (the home, create dialog, join, lobby, voting, revealed, room-settings dialog, credits, both themes, both sizes). **Not yet probed** (11 rows): 3 (join action, validation messages), 5 (loading), 8 (countdown), 10 (fireworks), 11 (observer), 13 (non-owner), 15 (transfer), 16 (role change), 17 (leave), 19 (error and not-found), 20 (connection status). 6a extends the script to them; each is checked in the slice that owns the row.

## 5. Gaps (old -> new, from the comparison run)

Measured on the current new app against `razor-final`, desktop 1280x800 and phone 375x812, light and dark, after removing the accepted differences. The full table with values is on [issue #33](https://github.com/vlad-gaman/PlanningPoker/issues/33); the builder re-runs the script, since the first slice changes what the later ones see.

**6a Foundation**
- Font: `system-ui` -> `Roboto` on every text. Roboto is not loaded, and in the screenshots Material buttons, the table, dialogs and fields render in a serif font. Observed, cause not confirmed: 6a checks whether the font is simply not loaded or the Material typography config lacks a fallback, and fixes it by using the old `system-ui` stack everywhere (no webfont added).
- Buttons: 16px/400 -> Material 14px/500; plain buttons 4px radius, 1px border, white, 6px 12px padding -> pill or 0 radius with 0 padding; primary white outlined -> blue (27,110,194) flat pill.
- Footer: the "|" separator grey (108,117,125) -> (33,37,41); the "Credits" link grey -> blue; dark mode (77,171,247).
- Dark mode: dialog and button background (45,45,45) -> (26,26,26); table row stripes inverted ((26,26,26) -> (45,45,45)); primary button text and background differ.
- Content width: content left edge x=82 -> x=70 at 1280px.
- Tooling: reference shots, the script with an `npm run parity` command, the checklist file, every unprobed row added to the script.

**6b Home and dialogs**
- Home: centred form rows ("Your Name:" + input + Dev/Tester/Observer toggles; "⚙️ More Settings" + muted hint; "Create room"; "Room Name:" + input; "Join room") -> h1, two outlined floating-label fields, Role select, blue pill "Create room", divider, outlined pill "Join room".
- Role control: three toggle buttons (Dev selected blue) -> Role select. No heading on the old home.
- Join screen: "Room: id" row, name field, toggles, "Join room" and "Use Different Name" -> h1 "Room id", "Enter your name to join this room.", Role select, "Join room" (reading in question 2).
- Create-settings dialog: labels above plain bordered inputs, hints ("Set to 0 for instant reveal", "Caution! It may generate weird names!!!"), `×` close, "Apply Settings" and "Cancel" outlined, 8px radius -> 28px radius, floating labels, no hints, no `×`, fireworks and fun-name order swapped, "Create room" and "Cancel" text buttons (reading in question 1).
- Room-settings dialog: the same plus "Transfer ownership:" -> "New owner" and "Save settings".

**6c Room and results**
- Title: "Room: id" 16px centred label -> "Room id" 2rem bold h1, left.
- Cards: 64x84px, 10px radius, 1px grey border, 16px, centred, one row at 1280px, greyed after reveal -> 66x50px, 0 radius, grey fill, 20px, left-aligned, wrapping.
- Action row: centred "Clear votes / Show votes / ⚙️ Settings" with the role toggles in the same row, 4px radius -> left-aligned pill buttons, the role radio group on its own row (reading in question 2: "Leave room").
- Table text: 16px, left, 8px padding, light row lines, no header -> 14px, 16px horizontal padding, Name/Role/Vote header (the single table itself is accepted).
- Charts: titles 16px/400 centred -> 600, left, bordered cards; averages centred -> left.
- Chart animation on -> off (`animation: false`). Restored; `chart-config.ts` and its unit test change.
- Connected badge: green "Connected" pill bottom right (`.connection-status`; yellow pulsing when reconnecting, red when disconnected) -> none.
- Dark: table stripes (6a).
- Owner checks: hover and selection effects, fireworks timing, countdown look.

**6d Shell, credits and phone**
- Brand: plain text, no underline -> underlined blue link.
- Theme select: bold selected label, right edge x=1197 -> regular, x=1210.
- Credits: heading and section title weight 500 -> 700; "Back to Home" white outlined 4px radius -> blue flat pill.
- Phone: cards 5 per row, 64px, centred -> 4 per row, 66x50, left; tables full width with 10px margin -> 16px padding with header; charts stacked at 245px, centred -> bordered cards full width; page height revealed 1352px -> 1644px (+22%), voting 817 -> 856.
- 768px: the old app switches layout and root font size (14px below 768, 16px above) at the breakpoint; the new client has no `@media` rule, so every rule above gets a breakpoint check.

## 6. Tests

- `npm run parity` produces, per slice, the image pairs and the style table for the rows it owns; the style assertions (old values for font, size, weight, colour, background, padding, radius on the matched elements) fail the run when a value is off and no accepted-difference note exists. Page height within 5%.
- Unit tests (xUnit not involved: no backend change) in `ng test` for each client method a fix adds or changes (for example the chart config animation flag, theme handling, any new component logic), per CLAUDE.md.
- The existing Playwright e2e suite stays green; selectors that depend on Material markup are updated with the markup, not skipped.
- The owner checks, per slice: the image pairs, motion, dialogs, one real phone at the end (6d) and Firefox once.

## 7. Acceptance criteria

1. Every row of `docs/parity-checklist.md` is ticked by the owner or has an accepted-difference note, before slice 6 starts.
2. `npm run parity` runs on a clean clone and produces the pairs and style table; the reference shots and the script are committed.
3. No remaining style assertion failure without a note.
4. Light and dark both match on every row.
5. No REST, hub or Swagger change; `ng lint`, `ng test` and Playwright pass.
6. The prototype branch is not merged.

## 8. Open questions for review

Each has a recommendation; the spec above follows it unless the owner says otherwise.

1. **Dialog styling.** The Material dialogs are an accepted difference. Is the *look* inside them accepted too (labels, hints, fonts, button styling, field style)? **Recommendation: no.** Match typography, colours, hints text and button styles to the old dialog (the dialog keeps its Material shape and floating-label fields). Alternative: accept the whole dialog as built.
2. **In-route name entry and "Leave room".** Slice 1 made these deliberate differences. Is each accepted as a *route/behaviour* only (so its look still matches the old join screen and toolbar) or as a *look*? **Recommendation: accepted as route/behaviour; the look matches the old join screen ("Room: id", centred rows, toggles, "Use Different Name" stays out, as it needs no new behaviour) and "Leave room" is styled like the old buttons.** Alternative: accept the current look of both.
3. **Role toggles and select.** The old app shows Dev / Tester / Observer as toggle buttons on home, join and in the room toolbar; the new one uses a select and a radio group. Not on the accepted list. **Recommendation: restore the toggle look** in all three places, keeping the form controls and behaviour.
4. **Where the checklist, references and script live** (section 3). **Recommendation: as listed.** Alternative: keep the reference shots outside the repo (smaller repo, but they are lost with the machine and a clean clone cannot run the check).
5. **Font stack.** **Recommendation: the old `system-ui` stack everywhere, no webfont.** Alternative: ship Roboto and accept the difference.
6. **Page height bar of 5%.** The revealed room is +22% on phone today because of the table and chart cards. **Recommendation: keep 5%**, and let the owner accept a row with a note where the accepted single table is the cause.
7. **When to regenerate references.** **Recommendation: never after the first commit**, except if `razor-final` is retagged. Old app is frozen.
8. **Prototype branch.** Keep `prototype/styling-approach` after 6a to 6d? **Recommendation: yes until the cutover, then delete.**
