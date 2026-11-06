# Role 3 – Application and testing lead

**You own:** the changes to the frog-management repository that make it deployable, the quality gates
(type check, format check, unit tests, database tests), the smoke tests, and the Level 4 demonstration.
**Levels:** 1 (test stages), 3 (application deployed and verified), 4 (code change → new deployment).
**You need:** Node.js 24 and Git on your laptop; write access to the GitHub repository; Docker Desktop for
step 5 (optional).
**You hand over:** H4 – the changes merged into `main`, so Role 2 can create the pipeline job.

---

## 1. Run the app and its checks locally

```powershell
git config --global core.autocrlf input          # keep LF endings (see step 2)
git clone https://github.com/NhatNguyen2407/frog-management.git
cd frog-management
node --version                                    # must be v24.x  (nodejs.org → 24 LTS installer)
npm ci
npm run typecheck                                 # passes
npm test                                          # 55 tests, all pass
npm run build                                     # passes
npm run dev                                       # http://localhost:3000/vi → "Your workspace is getting ready"
```
📷 the test summary (`ℹ pass 55`) and the setup screen.

## 2. Fix the formatting baseline (do this first – the pipeline fails without it)

`npm run format:check` currently **fails on `main`**: 21 existing files (mostly `public/prototype/*` and
`tests/prototype/*`, plus `docs/FRONTEND_PLAN.md`) are not Prettier-formatted. The pipeline runs this check, so the
very first build would go red.

> ⚠ **Confirm with the repo owner first** – this commit only re-formats their files (spacing, quotes, line
> breaks), no behaviour changes, but it touches 21 files.

```powershell
git switch -c chore/format-baseline
npm run format                 # prettier --write .
git diff --stat                # only the formatting changes
npm test                       # still 55 passing
npm run format:check           # "All matched files use Prettier code style!"
git commit -am "style: apply Prettier to existing files"
git push -u origin chore/format-baseline        # open a pull request, merge it
```
If `format:check` still flags *every* file on Windows, your checkout has CRLF endings: run step 3 first (it adds
`.gitattributes`), then `git rm --cached -r . -q && git reset --hard` to re-checkout with LF.

## 3. Add the pipeline files to the repository

```powershell
git switch main && git pull
git switch -c feat/devops-pipeline
# download devops-pipeline.patch from https://github.com/Nguyenstudent/COS40006/tree/main/group-project
git apply ..\devops-pipeline.patch            # path to wherever you saved it
#   (or copy everything from group-project/repo-files/ into the repo folder, keeping the paths)
git status                      # 19 new files, 2 modified (next.config.ts, src/proxy.ts)
npm run typecheck ; npm test ; npm run build ; npm run format:check
git add -A
git commit -m "feat: add Docker image, health endpoint and Jenkins pipeline"
git push -u origin feat/devops-pipeline           # pull request → review with the team → merge to main
```
Tell Role 2 when it is merged (H4).

### What the changes do (you will explain these in the presentation)

| File | Purpose |
|---|---|
| `next.config.ts` | `output: "standalone"` – `next build` produces a self-contained Node server in `.next/standalone`, so the image does not need `node_modules` |
| `src/app/api/health/route.ts` | `GET /api/health` → `{"status":"ok","release":"1.0.0","commit":"<sha>",...}`; used by the Docker HEALTHCHECK, the smoke tests and the Prometheus probe. Never touches the database |
| `src/proxy.ts` | the locale middleware now skips `/api/*`; without this `/api/health` was redirected to `/vi/api/health` (404) |
| `Dockerfile` | 3 stages: dependencies (cached) → `npm run build` → slim `node:24-slim` runtime as the non-root `node` user, port 3000, HEALTHCHECK. `NEXT_PUBLIC_*` are build arguments because Next.js compiles them into the bundle |
| `.dockerignore` | keeps `.git`, `node_modules`, `.env*` and the DevOps folders out of the image |
| `.gitattributes` | LF line endings everywhere (shell scripts and Prettier) |
| `Jenkinsfile` | the pipeline (see Role 2) |
| `deploy/compose.yaml` | runs `IMAGE:TAG` on the server, host port 80 → container 3000 |
| `deploy/deploy.sh` | on the server: records current/previous release, `docker compose pull && up -d`, waits for `/api/health`, **rolls back automatically** if it never becomes healthy; `--rollback` goes back one release |
| `deploy/remote-deploy.sh` | on Jenkins: copies `compose.yaml` + `deploy.sh` to `~/frog` on the target and runs `deploy.sh` |
| `deploy/smoke-test.sh` | checks `/api/health`, `/vi`, `/en` return 200 with the expected content; exit code 1 on any failure |
| `monitoring/`, `infra/` | used by Roles 4 and 1 |

Already verified on Node 24.22.2: typecheck ✓, 55 unit tests ✓, standalone build ✓, standalone server passes
`smoke-test.sh` ✓ (and the smoke test fails correctly against a dead address).

## 4. The test stages you own

| Jenkins stage | Command | What it proves |
|---|---|---|
| Static checks | `npm run typecheck`, `npm run format:check` | the code compiles and follows the agreed style |
| Unit tests | `node --test` with spec + JUnit reporters (same files as `npm test`) | business rules (finance, stock, formatting, prototype flows); results appear under *Test Result* in Jenkins |
| Database tests | `npm run test:db` | migrations + row-level-security rules work on a real `postgres:17` (disposable container, changes rolled back) |
| Smoke test: test / Verify production | `bash deploy/smoke-test.sh http://<host>` | the deployed container serves the app |

Run the database test once locally if Docker Desktop works: `docker pull postgres:17` then `npm run test:db`
→ "Database schema and permission assertions passed". 📷 it.

## 5. (Optional) Build and run the image locally

Needs Docker Desktop. On Kiet's PC port 8080 is taken by PostgreSQL's Apache, so use 8090:
```powershell
docker build --build-arg GIT_SHA=local -t frog-app:local .
docker run --rm -d --name frog -p 8090:3000 frog-app:local
docker ps                                  # STATUS shows (healthy) after ~30 s
bash deploy/smoke-test.sh http://localhost:8090      # Git Bash
docker rm -f frog
```
📷 `docker ps` showing *(healthy)* and the smoke test PASS lines.

## 6. Level 4 demonstration (rehearse before Week 11)

**A. A code change flows to production automatically**
1. Edit the visible setup-screen badge in **both** language files (the translation-parity test checks both have
   the same keys – change only the values):
   - `messages/en.json` → `"setupBadge": "Connection pending · release 1.0.1"`
   - `messages/vi.json` → its `setupBadge` value + ` · release 1.0.1`
2. Bump `RELEASE` in `src/app/api/health/route.ts` to `"1.0.1"`.
3. `npm test` → commit → push (or merge a PR) to `main`.
4. Show: GitHub webhook → Jenkins build "Started by GitHub push" → stages run → test server shows the new badge
   → **Approve** → production shows it; `/api/health` shows `"release":"1.0.1"` and the new commit.
📷 before/after of the page and `/api/health`, and the build.

**B. A broken change is stopped**
1. Make one assertion fail, e.g. in `tests/domain/formatting.test.ts`, and push to `main`.
2. Show the build red at **Unit tests**, the *Test Result* page naming the failing test, and production still on
   the previous release.
3. `git revert HEAD` → push → green again.

**C. Automatic rollback (optional)**
On the **test** server only: `ssh ec2-user@<test-ip> "cd ~/frog && ./deploy.sh <repo> doesnotexist"` →
the pull fails / never becomes healthy → `deploy.sh` reports *Automatic rollback* and the old release is
serving again. Or demonstrate `./deploy.sh --rollback` on production.

## Troubleshooting

| Problem | Fix |
|---|---|
| `npm ci` warns about install scripts (`@parcel/watcher`, `@swc/core`) | npm 11 warning only; the build still works |
| `.ts` test files not found / syntax error | Node older than 24 – install Node 24 |
| `/api/health` returns 404 or redirects | `src/proxy.ts` change missing (matcher must start with `(?!api|_next`) |
| Translation-parity test fails after the demo edit | you changed a key or edited only one language file |

## Evidence checklist (for your report section)

Save console output as text as well as screenshots, and log every error in the shared issues log when it happens
([testing-and-report-plan.md](testing-and-report-plan.md)). Role 4 runs the acceptance tests on your part afterwards.

- [ ] Local test run (55 pass) · formatting commit · the devops pull request and its diff
- [ ] `/api/health` JSON in the browser · local image *(healthy)* · smoke test PASS output
- [ ] Level 4 A before/after · Level 4 B red build + Test Result page · revert to green

## Done when

- [ ] `npm run format:check` passes on `main` and the devops files are merged
- [ ] The pipeline is green up to production, and the Level 4 A and B demos work end to end
