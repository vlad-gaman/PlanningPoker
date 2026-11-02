# Phase 6 spec: cutover (slice 6)

Hand-off spec for slice 6 of the Angular migration of PlanningPoker: sign off parity, retire the Razor app `PlanningPokerUi`, point CI and deploy at `PlanningPoker.Api`, merge `angular-migration` into `main` and put the new app live. Source of truth for the *why* is the wayfinder map, [issue #3](https://github.com/vlad-gaman/PlanningPoker/issues/3); this file repeats the facts a builder needs. Repo rules in `CLAUDE.md` apply.

Scope is **cutover only**: no new features, no behaviour change in the new app. Most of the slice is deletion and configuration; the only app code touched is the docs, the workflows and the solution/IDE files. Branch: `slice-6-cutover`, one pull request into `angular-migration`, reviewed by the owner, CI green before merge. A second, separate step merges `angular-migration` into `main` (section 5) and is the owner's call.

## 0. Where this slice sits

[Issue #15](https://github.com/vlad-gaman/PlanningPoker/issues/15) orders the slices; this is the last **functional** one, and it comes after the visual-parity slices 6a to 6d ([phase-7-spec.md](phase-7-spec.md), wayfinder map [issue #27](https://github.com/vlad-gaman/PlanningPoker/issues/27)). Slices 0 to 5 are built (voting, results, room management, shell and credits) and every slice spec listed "cutover" as its remaining later item. **This slice starts only after 6a to 6d are merged and every row of `docs/parity-checklist.md` is ticked by the owner or carries an accepted-difference note.**

Already in place: `PlanningPoker.Api` serving the built SPA from `wwwroot` (`MapFallbackToFile`), `PlanningPoker.sln` (the new projects only), `PlanningPokerUi.sln` (the old project only), CI (`.github/workflows/ci.yml`: backend, client, package and Playwright against the published app, `site.zip` artifact), `deploy-staging.yml` (manual, `staging` slot, tags `staging-N` and keeps the zip as a pre-release; `staging-1` to `staging-4` exist), `rollback-staging.yml`.

## 1. What goes, what stays

**Deleted** (the old app and its leftovers):

- `PlanningPokerUi/` entirely (Pages, Controllers, Hubs, Models, ViewModels, Services and the name-generator CSVs, `Startup.cs`, `Program.cs`, `wwwroot` including `lib` and `site.js`, publish profiles, its `Dockerfile` for .NET Core 3.1).
- `PlanningPokerUi.sln`.
- Root-level debug and test leftovers: `FIST_OF_FIVE_FIX.md`, `PARTIAL_VOTING_FIREWORKS_FIX.md`, `VOTING_FIX_SUMMARY.md`, `VotingBehaviorTest.md`, `VotingBehaviorTests.html`, `comprehensive-voting-test.js`, `fireworks-test.js`, `voting-debug.html`.
- `ANGULAR_MIGRATION_CHECKLIST.md` once every box is ticked (decision 5).

**Rewritten**: `README.md` (it still describes `PlanningPokerUi` and its tree), `.vscode/launch.json` and `.vscode/tasks.json` (they build `PlanningPokerUi.sln` and launch `PlanningPokerUi.dll`), and the workflows (section 4).

**Kept**: all `PlanningPoker.*` projects, `docs/` (the phase specs are history worth keeping), `.dockerignore` (check it, decision 6).

Before deleting, confirm nothing in `PlanningPoker.*` references the old project or its files (`grep -ri PlanningPokerUi` outside the folder; the `README.md`, `.vscode` and `PlanningPokerUi.sln` hits are known).

## 2. Parity checklist (owner sign-off)

The owner walks this against the **staging slot** running the CI-built `site.zip` of the slice branch, side by side with the old app where wanted. A box is ticked only by the owner. Anything unticked is either fixed in a follow-up slice before the merge to `main` or recorded as an accepted difference in the checklist note (visual) or in the PR description (functional). The **visual** side of every item (look, spacing, colours, phone layout, motion) is covered by `docs/parity-checklist.md` from slices 6a to 6d, not here; items below keep their functional parts.

1. Create a room with each of the **7 card sets**; join, vote, clear, reveal, force reveal work on each; custom card values where the old app had them.
2. **Ownership and transfer**: owner leaves, ownership moves; owner transfers manually; the settings dialog is owner-only; "clear votes if shown" behaves as the old app.
3. **Countdown**: starts, shows to everyone, ends in reveal; cancelled by a new vote or clear as before.
4. **Results**: statistics, chart, fireworks (including no fireworks for a one-person vote, the old bug fix).
5. **Person type**: voter / observer switch, observers cannot vote.
6. **Dark mode** (functional part): auto / light / dark, preference kept across reloads, no flash of the wrong theme. The colours are in the parity checklist.
7. **Credits**: every attribution of the old page present, links work, Back to Home works.
8. **Join / leave / reconnect**: refresh the room page and rejoin as the same person; close a tab and the others see the leave; drop the connection (offline in dev tools) and reconnect; open a room link in a fresh browser (join dialog); an unknown room id shows the not-found path.
9. **Phone** (375x812, or a real phone), functional part: create, join and vote work in light and dark. The layout is in the parity checklist; the real-phone look is checked once in step 3 of section 5.
10. **Old links**: `/Room/<id>` and `/Credits` still land on the right pages (the redirects from slices 1 and 5).

## 3. Contract

No REST, hub or Swagger change. One thing to settle before production, because the old and new apps are different programs on the same host (decision 3): **rooms are in memory in both**, so a deploy ends every running room. The cutover therefore happens at a quiet time, and the PR description says so. No data migration exists or is needed.

## 4. Build and deploy changes

- **CI** (`ci.yml`): `push` and `pull_request` trigger on `angular-migration` today; after the merge they must trigger on `main` (keep `angular-migration` until it is deleted, so this slice's own PR still gets CI). `workflow_call` stays for the deploy workflows. Jobs and the `site.zip` artifact are unchanged.
- **Deploy**: `deploy-staging.yml` and `rollback-staging.yml` already target the zip built from `PlanningPoker.Api`; they stay as they are. Add `deploy-production.yml`, manual (`workflow_dispatch`), `needs: test` via `workflow_call`, environment `production`, which deploys the zip to the **staging slot** and not to production directly when the plan has slots; the swap is the owner's click in the portal or an `az webapp deployment slot swap` step behind a second environment approval. If the plan has **no slots** (open question 1), the workflow deploys straight to production and the rollback workflow is the safety net. Secrets and variables: reuse `AZURE_WEBAPP_NAME`; the publish profile is per slot or app, so the production environment gets its own `AZURE_WEBAPP_PUBLISH_PROFILE`.
- **Rollback**: the same pattern as `rollback-staging.yml` for production (redeploy a kept zip by tag; production tags `prod-N`), and the `razor-final` release (section 5) is the way back to the old app.
- **Pre-deploy checks on App Service**, done by the owner before the first production deploy and written into the PR description as a checked list: runtime stack **.NET 10** (the old app targeted an older framework, so this may be the one change in the portal), **WebSockets on** (SignalR falls back to long polling without it, which works but is slower), **ARR affinity** off or on is irrelevant while the app is one instance, so confirm the plan runs **one instance** (in-memory rooms break on scale-out), HTTPS only, startup command empty (the zip is framework-dependent), the app name and slot match the workflow variables.
- **Dockerfile**: the old one targets .NET Core 3.1 and `PlanningPokerUi`; it goes with the project. No replacement is added unless the owner wants containers (it is not in the current deploy path).
- **Solution and IDE files**: `PlanningPoker.sln` is the solution; `.vscode/launch.json` launches `PlanningPoker.Api` (and the Angular dev server per the client README), `.vscode/tasks.json` builds `PlanningPoker.sln`.
- **README.md**: describe the new app (what it is, how to run API and client for development, how to test, how CI and deploy work, project layout of the five `PlanningPoker.*` projects and the client). The old screenshots and `PlanningPokerUi/` tree are removed; do not invent features.

## 5. Cutover sequence

Order matters; each step has a way back.

1. **Tag and keep the old build.** On `angular-migration`, at the last commit before the slice 6 deletions (`main` is older and lacks dark mode, settings and the credits page, so it is not the old app to keep): tag `razor-final`, and keep the last old build as a GitHub release. There is no CI zip for the old app, so build it once locally (`dotnet publish PlanningPokerUi -c Release`), zip it and attach it to the release `razor-final`. Done by the owner or by an agent only on the owner's go-ahead (it publishes a release).
2. **Slice PR** (`slice-6-cutover` into `angular-migration`): deletions, README, IDE files, workflow changes, parity checklist added to the PR description for the owner to tick. CI green.
3. **Deploy the `angular-migration` build to the staging slot** (`Deploy to staging`), owner walks the parity checklist (section 2) against it, and checks the look once on **one real phone** and once in **Firefox**.
4. **Merge `angular-migration` into `main`** (owner, merge commit, not squash, so the slice history stays; the long-lived branch also carries the earlier slice PRs). CI runs on `main`.
5. **Production deploy** with the new workflow; owner swaps the slot (or the direct deploy runs), checks the live site (create a room, join from a second browser, vote, reveal), and keeps the old slot content in place until the check passes: the swap is its own rollback.
6. **After a quiet week** (owner's call): delete the `angular-migration` branch and the other merged `slice-*` branches; archive or close the migration issues (#3, #15) with a link to the release notes; remove the `staging-N` pre-releases older than the last three.

## 6. Tests

No new application behaviour, so no new unit tests (CLAUDE.md asks for a test per method implementation; no method is added). The slice is verified by what already exists plus:

- `dotnet build PlanningPoker.sln` and `dotnet test PlanningPoker.UnitTests` pass with `PlanningPokerUi` gone (proves nothing depended on it).
- `ng lint`, `ng test` and the full Playwright suite pass against the published app in CI.
- A clean clone builds with only the documented commands in the new README (the owner or an agent follows the README literally once).
- `grep -ri PlanningPokerUi` over the tracked files finds nothing outside `docs/` (historic specs) and the migration checklist's history.

## 7. Acceptance criteria

1. Every box of the parity checklist (section 2) is ticked by the owner, or the difference is listed here as accepted before the merge to `main`; and `docs/parity-checklist.md` is complete (every row ticked or noted) before this slice starts.
2. `PlanningPokerUi`, its solution, Dockerfile and the root-level debug files are gone; nothing references them (except `docs/`).
3. CI runs on `main`; `Deploy to staging` and the production deploy both work from `main`; rollback is documented and was dry-run once on staging.
4. `razor-final` tag and release exist before the old project is deleted from `main`.
5. The README matches reality and a clean clone follows it to a running app.
6. The App Service pre-deploy list is checked off in the PR description.
7. No change to the REST/hub/Swagger contract or to client behaviour.

## 8. Open questions for review

Each has a recommendation; the spec above already follows it.

1. **Does the production App Service plan have deployment slots?** Unknown from the repo (staging uses a slot named `staging`, which suggests the production app has one, but production has never been deployed from this repo's workflows). If yes: deploy to the slot, swap. If no: direct deploy. The owner confirms; the spec assumes slots.
2. **Merge `angular-migration` into `main` with a merge commit. Recommended.** It keeps the per-slice history (the same style as PRs #19 to #25). Alternative: squash, one big commit, simpler `git log`, lost slice history.
3. **Deploy at a quiet time and accept that running rooms end. Recommended.** Rooms live in memory in both apps, so there is nothing to migrate; announcing a window is enough for a small team tool. Alternative: none that is cheap (persisting rooms is a feature, not a cutover task).
4. **Keep the old build as a release asset, built once by hand. Recommended.** The old app has no CI, so `razor-final` plus a zip is the cheapest way back. Alternative: tag only, and rebuild from the tag if ever needed (.NET SDK version drift makes that riskier).
5. **Delete `ANGULAR_MIGRATION_CHECKLIST.md` at the end. Recommended.** It is a plan for a finished job; the phase specs and the closed issues keep the history. Alternative: keep it with every box ticked.
6. **Drop the Dockerfile with the old project, add none. Recommended.** The deploy path is a zip to App Service. Check `.dockerignore` for references to deleted paths and trim them; if the owner wants containers, that is a new feature slice.
7. **Delete `docs/phase-*-spec.md`? No. Recommended.** They are cheap, referenced from the issues, and explain the why of decisions. Only the root-level debug files and the migration checklist go.
8. **Production environment approvals.** Recommended: GitHub environment `production` with the owner as required reviewer, so the production deploy and the swap need a click. Alternative: no reviewer (the workflow is already manual).
