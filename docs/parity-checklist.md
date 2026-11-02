# Parity checklist

Visual parity of the Angular client with the Razor UI at tag `razor-final`. Spec: [phase-7-spec.md](phase-7-spec.md). Slice 6 (cutover) starts only when every row is ticked by the owner or carries an accepted-difference note.

How to read it:

- **Automated** is the result of `npm run parity` in `PlanningPoker.Client` for that row (`ok`, or the failing property). Reference shots are in [parity/reference](parity/reference); image pairs (old | new | diff) are written to `PlanningPoker.Client/parity/out/`.
- **Owner** is ticked (`x`) by the owner only, after looking at the image pair.
- **Note** records an accepted difference or what is left. Differences accepted for all rows are listed in the spec (section 2). Per-row style exceptions live in `PlanningPoker.Client/parity/accepted.json`.
- **Slice** is the fix slice that owns the row. Phone rows belong to 6d.

## Probed states (8 states x desktop/phone x light/dark)

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
| room-lobby | desktop | light | 6c | | | |
| room-lobby | desktop | dark | 6c | | | |
| room-lobby | phone | light | 6d | | | |
| room-lobby | phone | dark | 6d | | | |
| room-voting | desktop | light | 6c | | | |
| room-voting | desktop | dark | 6c | | | |
| room-voting | phone | light | 6d | | | |
| room-voting | phone | dark | 6d | | | |
| room-revealed | desktop | light | 6c | | | |
| room-revealed | desktop | dark | 6c | | | |
| room-revealed | phone | light | 6d | | | |
| room-revealed | phone | dark | 6d | | | |
| credits | desktop | light | 6d | | | |
| credits | desktop | dark | 6d | | | |
| credits | phone | light | 6d | | | |
| credits | phone | dark | 6d | | | |

## Not yet in the script

Rows of the inventory table (`docs/research/ui-inventory.md` on branch `research/ui-inventory`) that the script does not reach yet. Slice 6a adds those it can reach; each is checked in the slice that owns it, in both sizes and schemes.

| Inventory row | State | Slice | Owner | Note |
|---|---|---|---|---|
| 3 | join action and validation messages | 6b | | |
| 5 | room loading | 6c | | |
| 8 | countdown running | 6c | | |
| 10 | fireworks on consensus (motion, owner check) | 6c | | |
| 11 | observer view | 6c | | |
| 13 | non-owner view | 6c | | |
| 15 | ownership transfer | 6b | | |
| 16 | role change in the room | 6c | | |
| 17 | leave room | 6c | | |
| 19 | error and not-found | 6b | | |
| 20 | connection status and reconnect | 6c | | |

## Owner checks without automation

- [ ] Motion: hover, card selection, chart animation, fireworks timing (6c)
- [ ] One real phone (6d)
- [ ] Firefox once (6d)
