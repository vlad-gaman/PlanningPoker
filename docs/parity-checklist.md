# Parity checklist

Visual parity of the Angular client with the Razor UI at tag `razor-final`. Spec: [phase-7-spec.md](phase-7-spec.md). Slice 6 (cutover) starts only when every row is ticked by the owner or carries an accepted-difference note.

How to read it:

- **Automated** is the result of `npm run parity` in `PlanningPoker.Client` for that row (`ok`, or the failing property). Reference shots are in [parity/reference](parity/reference); image pairs (old | new | diff) are written to `PlanningPoker.Client/parity/out/`.
- **Owner** is ticked (`x`) by the owner only, after looking at the image pair.
- **Note** records an accepted difference or what is left. Differences accepted for all rows are listed in the spec (section 2). Per-row style exceptions live in `PlanningPoker.Client/parity/accepted.json`.
- **Slice** is the fix slice that owns the row. Phone rows belong to 6d.

## Probed states (16 states x desktop/phone x light/dark)

| State | Size | Scheme | Slice | Automated | Owner | Note |
|---|---|---|---|---|---|---|
| home | desktop | light | 6b | | | |
| home | desktop | dark | 6b | | | |
| home | phone | light | 6d | | | |
| home | phone | dark | 6d | | | |
| home-dialog | desktop | light | 6b | | | |
| home-dialog | desktop | dark | 6b | | | |
| home-dialog | phone | light | 6d | | | |
| home-dialog | phone | dark | 6d | | | |
| join | desktop | light | 6b | | | |
| join | desktop | dark | 6b | | | |
| join | phone | light | 6d | | | |
| join | phone | dark | 6d | | | |
| room-settings | desktop | light | 6b | | | |
| room-settings | desktop | dark | 6b | | | |
| room-settings | phone | light | 6d | | | |
| room-settings | phone | dark | 6d | | | |
| room-lobby | desktop | light | 6c | ok | | |
| room-lobby | desktop | dark | 6c | ok | | |
| room-lobby | phone | light | 6d | | | |
| room-lobby | phone | dark | 6d | | | |
| room-voting | desktop | light | 6c | ok | | |
| room-voting | desktop | dark | 6c | ok | | |
| room-voting | phone | light | 6d | | | |
| room-voting | phone | dark | 6d | | | |
| room-revealed | desktop | light | 6c | ok | | |
| room-revealed | desktop | dark | 6c | ok | | |
| room-revealed | phone | light | 6d | | | |
| room-revealed | phone | dark | 6d | | | |
| credits | desktop | light | 6d | | | |
| credits | desktop | dark | 6d | | | |
| credits | phone | light | 6d | | | |
| credits | phone | dark | 6d | | | |
| home-join-error | desktop | light | 6b | | | |
| home-join-error | desktop | dark | 6b | | | |
| home-join-error | phone | light | 6d | | | |
| home-join-error | phone | dark | 6d | | | |
| room-not-found | desktop | light | 6b | | | |
| room-not-found | desktop | dark | 6b | | | |
| room-not-found | phone | light | 6d | | | |
| room-not-found | phone | dark | 6d | | | |
| room-settings-transfer | desktop | light | 6b | | | |
| room-settings-transfer | desktop | dark | 6b | | | |
| room-settings-transfer | phone | light | 6d | | | |
| room-settings-transfer | phone | dark | 6d | | | |
| room-observer | desktop | light | 6c | ok | | |
| room-observer | desktop | dark | 6c | ok | | |
| room-observer | phone | light | 6d | | | |
| room-observer | phone | dark | 6d | | | |
| room-nonowner | desktop | light | 6c | ok | | |
| room-nonowner | desktop | dark | 6c | ok | | |
| room-nonowner | phone | light | 6d | | | |
| room-nonowner | phone | dark | 6d | | | |
| room-role-change | desktop | light | 6c | ok | | |
| room-role-change | desktop | dark | 6c | ok | | |
| room-role-change | phone | light | 6d | | | |
| room-role-change | phone | dark | 6d | | | |
| room-countdown | desktop | light | 6c | ok | | |
| room-countdown | desktop | dark | 6c | ok | | |
| room-countdown | phone | light | 6d | | | |
| room-countdown | phone | dark | 6d | | | |
| room-fireworks | desktop | light | 6c | ok | | |
| room-fireworks | desktop | dark | 6c | ok | | |
| room-fireworks | phone | light | 6d | | | |
| room-fireworks | phone | dark | 6d | | | |

## Slice 6b notes

Home, join, both dialogs and the notices now match the old look on desktop (0.8% to 2.7% pixel noise, no font, size, weight, colour or background failure). Differences kept, for the owner to confirm:

- No "More Settings" button or hint on the home page: "Create room" opens the settings dialog (behaviour from slice 3). The dialog's submit button reads "Create room" where the old one read "Apply Settings".
- "Use fun room name?" starts checked in the new dialog (old: unchecked).
- The home page keeps a visually hidden `h1` "Planning Poker" for screen readers; the old page had none.
- Role stays a select (owner decision), now a native select next to the name; the old Dev/Tester/Observer toggles are not restored.
- The role options and the people table gain an "SRE" role, and the results gain a fourth chart, "SRE votes" (always shown, like Testers). The Razor app has neither (accepted difference, issue #42).
- The join step shows a one-line sentence "Enter your name to join this room." and has no "Use Different Name" button.
- Phone rows differ only by the 14px root font below 768px (slice 6d). Room desktop rows are done in 6c.

## Slice 6c differences

- Observers see greyed cards (they cannot vote); the old page does not grey them.
- The people table keeps its Name/Role/Vote header row and the "(owner)" marker; the old table has neither.
- The role control stays a radio group, restyled as the old toggle buttons.
- There is no connection badge in the new client, so rows 5 and 20 need nothing; row 17 (Leave room) uses the standard button style.

## Not reachable by the script

Inventory rows (`docs/research/ui-inventory.md` on branch `research/ui-inventory`) with no state to capture. Each is an owner check in the slice that owns it.

| Inventory row | State | Slice | Owner | Note |
|---|---|---|---|---|
| 5 | room loading | 6c | | The old app has no loading state (server-rendered); the new one shows only the heading for a moment, too short to capture. |
| 17 | leave room | 6c | | The old app has no Leave button; the new button is styled like the old buttons (accepted as route and behaviour only). |
| 20 | connection status, reconnecting and disconnected | 6c | | Taking the network offline did not trigger the old "Reconnecting..." pill within 3 s, so it is not captured. The connected pill shows in every room state. |

The probed rows above cover inventory rows 3 (`home-join-error`), 8 (`room-countdown`), 10 (`room-fireworks`, pixel noise expected from the particles), 11 (`room-observer`), 13 (`room-nonowner`), 15 (`room-settings-transfer`), 16 (`room-role-change`) and 19 (`room-not-found`).

## Owner checks without automation

- [ ] Motion: hover, card selection, chart animation, fireworks timing (6c)
- [ ] One real phone (6d)
- [ ] Firefox once (6d)
