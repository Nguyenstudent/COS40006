# Role 3 - Implementation and local test handover

Updated: 15 November 2026 (UTC+7). Local results below were recorded on 14 November 2026.

I have prepared the application and pipeline source in the separate [frog-management-swe40006 coursework repository](https://github.com/NhatNguyen2407/frog-management-swe40006). This records my progress against the [Role 3 guide](role-3-application-testing.md) and the [team testing/report plan](testing-and-report-plan.md).

The application and local checks are ready for H4. Jenkins integration, test/production deployment and Level 4 acceptance remain pending.

## Source changes

| Commit | Changes |
| --- | --- |
| [d2e21e6](https://github.com/NhatNguyen2407/frog-management-swe40006/commit/d2e21e60577bbb93adf9ba01dea9d2398cdbd62c) | Applied Prettier and added the LF rule in .gitattributes. |
| [4bd958a](https://github.com/NhatNguyen2407/frog-management-swe40006/commit/4bd958acaa31886d3459f8d2150fb3bf9ce761fe) | Added Docker packaging, standalone output, health, proxy exclusion, smoke tests and a coursework-specific database container name. |
| [4abfda4](https://github.com/NhatNguyen2407/frog-management-swe40006/commit/4abfda4c3a2e23a8f9584113fb92cf2376f09c13) | Added Jenkinsfile, Compose and local/remote deployment scripts. |

The application repository's main branch is at 4abfda4. The three commits can also be reviewed as [one application diff](https://github.com/NhatNguyen2407/frog-management-swe40006/compare/d182cead05c6e39caa7625d4d6fbc85ec406f2aa...4abfda4c3a2e23a8f9584113fb92cf2376f09c13).

### Implemented files

| File in the application repository | What I changed |
| --- | --- |
| .gitattributes and existing source/tests | Normalized LF and formatting. |
| next.config.ts | Enabled standalone output. |
| Dockerfile and .dockerignore | Added the multi-stage image, non-root runtime, health check and build-context exclusions. |
| src/app/api/health/route.ts and src/proxy.ts | Added status/release/commit metadata and excluded API routes from locale handling. |
| scripts/test-database.mjs | Changed the isolated test container name to frog-swe40006-db-tests. |
| deploy/smoke-test.sh | Checks health status, the expected commit, VI/EN, the prototype page and app.js. Both URL and expected commit are required. |
| deploy/compose.yaml | Runs IMAGE:TAG; maps host port 80 to container port 3000 by default. I used port 8091 locally. |
| deploy/deploy.sh and deploy/remote-deploy.sh | Prepared release history, health/commit verification, recovery and SSH/SCP deployment. Server execution remains unverified. |
| Jenkinsfile | Prepared quality gates, JUnit publication, image build/push, test deployment, approval and production verification. The full pipeline has not run. |

## Recorded local results

| Check | Result |
| --- | --- |
| npm ci, typecheck, format check and production build | PASS LOCAL, exit 0. |
| Unit tests and JUnit | 55 passed; XML has 55 cases, 0 failures, 0 errors and 0 skipped; exit 0. |
| Database suite | PostgreSQL 17 schema/permission assertions passed; test changes rolled back; exit 0. |
| Docker build/runtime | frog-app:local built and the container became healthy on port 8090. |
| Prototype order flow | FG-0008: two items, 50,000 VND, Paid; details remained after reload. No console errors were observed during this check. |
| Smoke success/failure checks | Six assertions passed. Wrong expected commit and a stopped server each returned 1; restart restored six PASS results and exit 0. |
| Compose configuration/runtime | Port 8091 mapped to 3000; container healthy; Compose and smoke exit 0. |
| Deployment script syntax | Both scripts passed bash -n; the deployment files use LF. |

The prototype uses browser localStorage. The PostgreSQL suite tests the backend independently; hosted Supabase authentication and Realtime were not tested.

The image still reports APP_COMMIT=local. The checks were performed as the working tree developed, rather than as one clean-checkout run of 4abfda4. Matching the built image to the final Git SHA remains an integration check. Stop/start recovery is not evidence of deployment rollback.

### Evidence held locally

Logs and screenshots are held under evidence/role-3/ in my application checkout. They have not been uploaded to this PR. Key logs are:

- final-checks-20261114-142933.txt and pipeline-files-20261114-152704.txt.
- junit-20261114-150826.txt and evidence/role-3/unit-tests.xml.
- database-test-20261114-135334.txt.
- smoke-test-20261114-141204.txt and smoke-negative-20261114-141829.txt.
- compose-runtime-20261114-154226.txt.

I still need to map the original logs/screenshots to Console/Figure captions and the team's shared issue IDs. The [section 4 draft](report/role-3-application-testing-draft.md) is included for review.

## Issues resolved locally

| Issue | Action and re-check |
| --- | --- |
| Format/line endings | Added the LF rule and applied Prettier; later format checks passed. |
| Missing Next CLI shim | npm ci restored the executable; the standalone build then passed. The cause of the missing shim was not established. |
| Docker Engine connection | The first build could not reach the engine; a later build connected and passed. |

## H4 inputs for Role 2

| Item | Value |
| --- | --- |
| Application SCM | https://github.com/NhatNguyen2407/frog-management-swe40006.git |
| Branch and prepared source | main, commit 4abfda4 |
| Working directory / Script Path | Repository root / Jenkinsfile |
| Jenkins variables | DOCKERHUB_REPO, TEST_HOST, PROD_HOST |
| Credential IDs | dockerhub, app-servers-ssh |
| JUnit report | evidence/role-3/unit-tests.xml |
| Smoke contract | bash deploy/smoke-test.sh <url> <expected-commit> |

Role 2 will configure the actual values and credentials in Jenkins. The SCM repository is private, so Jenkins and team reviewers need access to it.

## Remaining work from the guide

- Validate Jenkinsfile on Jenkins, run the first complete pipeline and confirm its Test Result report.
- Verify test/production smoke, approval and Git SHA/image/health traceability with Roles 1 and 2.
- Rehearse Level 4 A/B: change both setupBadge values and RELEASE, show deployment, deliberately fail a unit assertion, then revert the exact failing commit.
- Complete A3 rollback evidence on the test server and coordinate A1-A4, C2 and S1 acceptance with Role 4.
- Complete the evidence captions, shared issue entries and report after integration.

The report draft and this status update do not mark Jenkins, Level 3 deployment or Level 4 as complete.
