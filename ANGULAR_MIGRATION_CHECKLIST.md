# Angular Migration Checklist for Planning Poker

The migration plan is decided in the wayfinder map: [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3). The old horizontal phases (backend first, hybrid Razor + API, then Angular) were replaced by vertical slices on the long-lived `angular-migration` branch. The old `PlanningPokerUi` project stays untouched until the cutover.

Build details for the first two slices: [docs/phase-1-spec.md](docs/phase-1-spec.md).

## Slices (one pull request each, CI green before merge)

- [ ] **Phase 1** ([spec](docs/phase-1-spec.md))
  - [ ] Slice 0: walking skeleton (projects, middleware, session cookie, hub, CI, first Playwright test)
  - [ ] Slice 1: create and join
- [ ] Slice 2: voting (cast, clear, reveal, force reveal, the 7 card sets)
- [ ] Slice 3: results (statistics, chart, fireworks)
- [ ] Slice 4: room management (settings update, ownership transfer, countdown)
- [ ] Slice 5: shell and polish (theming, dark mode, Credits, Material look, mobile pass)
- [ ] Slice 6: cutover
  - [ ] Parity checklist signed off by the owner (7 card sets, ownership and transfer, countdown, fireworks, dark mode, Credits, join/leave/reconnect)
  - [ ] CI green; pre-deploy checks pass (.NET 10 on App Service, Web App settings)
  - [ ] Tag the last old commit `razor-final` and keep the last old build as a release
  - [ ] Delete the old project (`Pages`, `Controllers`, `ViewModels`, `site.js`, `wwwroot/lib`, root-level debug and test files)
  - [ ] Deploy workflow targets `PlanningPoker.Api`; merge `angular-migration` into `main`; slot swap (or direct deploy if the plan has no slots)
