---
name: ship-pr
description: "Ship the current branch: open a PR into main, wait for CI, deploy to the staging slot, merge, clean up. Use when the user says 'create PR', 'merge and deploy', 'ship it', or 'deploy to staging and merge'."
---

Ship the current branch. Do only the steps the user asked for ("create PR" stops after step 2; "merge PR" starts at step 5).

Base branch is `main`. Repo: `vlad-gaman/PlanningPoker`.

1. **Check the branch.** Run `git status` and `git log main..HEAD --oneline`. If the tree is dirty, commit with a sensible message first. If the branch has no commits ahead of `main`, or looks like the wrong branch, stop and say so.
2. **Open the PR.** Push, then `gh pr create --base main`. Write the body with the `pr` skill. Bind it with `mcp__ccd_pr__get_status` / `bind_pr`. Turn on Auto-fix only if the user asked.
3. **Wait for CI.** Use `gh pr checks <n> --watch` in the background, never a polling loop. If a check fails, read the log, fix, push. If the failure looks like a flake, rerun once and say so.
4. **Deploy to the staging slot.** Every PR run has a `deploy-staging` job in `ci.yml` that waits for manual approval on the `staging` environment (Required reviewers). Don't dispatch `deploy-staging.yml` for a PR branch. Tell the user the job is waiting for their approval ("Review deployments" on the run), then watch it; you cannot approve it yourself. It publishes to the `staging` slot (`https://planningpokerui-staging.azurewebsites.net/`). Smoke-test the page in the built-in browser when the change is visible. Only wait on it if the user asked for a deploy; it needs green CI.
   Manual `deploy-staging.yml` / `deploy-production.yml` (`gh workflow run ... --ref main`) reuse the `site` artifact of a passing CI run on the same commit and only rerun CI when there is none, so run them from `main` after merge when a deploy of main is wanted.
5. **Merge.** Only when CI is green and the user asked to merge: `gh pr merge <n> --merge`. If it conflicts, merge `origin/main` into the branch, resolve, push, and return to step 3.
6. **Clean up.** After merge: delete the remote and local branch, remove its worktree, and `git fetch --prune`. Never delete a branch whose PR is unmerged.

Report PR link, CI result, deploy run link and merge state in a few lines. Anything skipped or unverified gets named.
