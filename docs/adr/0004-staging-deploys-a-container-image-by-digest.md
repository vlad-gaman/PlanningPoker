# Staging runs a container image, deployed and rolled back by digest

Staging was a zip deployed to a Windows App Service slot, with each build kept as a `site.zip` release asset for rollback. Anyone able to write releases could replace an asset or pre-create a `staging-N` release, and a later rollback would deploy those bytes without CI (ticket 119). The Node and .NET versions also lived in the runner and the App Service, not in the repo (ticket 13).

Decision: the app is built and shipped as a container image.

- A multi-stage `Dockerfile` builds the Angular client (Node 24), publishes the API with the client in `wwwroot` (.NET 10 SDK), and runs it on the `aspnet` runtime image, port 8080.
- `deploy-staging.yml` runs CI, builds the image and pushes it to private GHCR as `staging-<run_number>`, then deploys it to the staging slot by digest (`image@sha256:...`). Only the build job has `packages: write`; the deploy job has no token permissions.
- `rollback-staging.yml` takes a tag, checks it against `^staging-[0-9]+$` (the input goes through `env:`, never into `run:`), resolves it to its digest in GHCR and deploys that digest.
- The image sets `Persistence__ConnectionString` to `Data Source=/home/data/planningpoker.db`. On App Service for Linux `/home` is the persistent mount (`WEBSITES_ENABLE_APP_SERVICE_STORAGE=true`), so rooms survive a deploy, a rollback and a restart, and the staging and production apps keep separate files because each has its own `/home`.
- The existing staging app is replaced by a new Linux plan and Web App (an app's OS cannot be switched); the new one takes over the hostname.
- No `site.zip` release assets are made or read. CI still uploads `site.zip` as a short-lived run artifact for its own end-to-end job.

Consequences:
- A tag in GHCR can be moved by anyone with package write access, but the deploy records the digest it pushed and rollback pins the digest it resolved, so the running bytes are the ones named in the run's summary. Rollback trusts the tag-to-digest mapping at the time it runs; it is no stronger than who can write the package.
- The deployed image is the one that passed CI for that commit only if the build job builds from the same commit, which it does (`needs: test`, same `github.sha`); the e2e job tests the zip, not the image, so the image itself is exercised only by running it.
- The image runs as root, because the `/home` mount's ownership under App Service is not under our control; a non-root user would need the mount to be writable by it.
- The Production environment redirects HTTP to HTTPS, so running the image locally needs `Https__Redirect=false`. `appsettings.Production.json` trusts any forwarder (ADR 0001), so the container must only be reachable through App Service's front end or another proxy that sets the forwarded headers.
- The Web App needs GHCR pull credentials as app settings; the package stays private.
- Images are never deleted by the workflows, so old `staging-N` tags remain rollback targets until pruned by hand.

Considered options:
- **Keep the zip deploy and add checksums or attestation** (the fix proposed in ticket 119): fixes rollback integrity only, and leaves the runtime versions unpinned.
- **Azure Container Registry**: an extra paid Azure service; GHCR is enough for one private image.
- **Pushing with `docker/build-push-action`**: adds three third-party actions to pin and review for what `docker build` and `docker push` already do on the runner.
