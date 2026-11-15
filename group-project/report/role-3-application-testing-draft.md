# Draft section 4 - Application, quality gates and deployment

Draft updated: 14 November 2026 (UTC+7).
Scope: Role 3 contribution based on local checks. Jenkins and test/prod results remain pending.

Implementation status and source commits: [Role 3 handover](../role-3-progress.md). Evidence captions and shared issue cross-references will be added before the final report.

## Application and scope

I used a separate coursework copy of Frog Management, a workshop and cafe management application built with Next.js, React and TypeScript. Keeping the coursework repository separate allowed me to prepare deployment changes without applying them to the operational project. My work covers application packaging, quality checks and local testing, with Jenkins integration shared with Role 2.

For the current demonstration, I used the standalone browser prototype. It supports creating sample orders, checking totals and payment status, and viewing workshop tickets. Data is stored in browser localStorage. This provides a usable demonstration without hosted Supabase credentials, but it does not demonstrate authentication, shared data between devices or live database transactions. The separate SQL suite tests backend behaviour independently.

## Container packaging

I enabled Next.js standalone output and added a multi-stage Dockerfile. The dependency stage installs packages from the committed lockfile, the build stage produces the application, and the runtime stage copies the standalone server, static files and public assets. The container runs as the node user and serves the application on port 3000.

I added /api/health to return status, release and commit information, and adjusted the proxy matcher so that the endpoint is served directly without a locale redirect. The handler sets Cache-Control to no-store. I confirmed HTTP 200 and the JSON metadata locally; capturing the response header remains part of integration verification. The endpoint does not query the database, so its success confirms application availability rather than database readiness.

## Quality checks

I ran dependency installation, TypeScript checking, Prettier checking, unit tests and a production build. The recorded local checks completed with exit code 0. The unit suite passed all 55 tests. Its coverage includes finance and stock calculations, formatting, translation-key parity, and prototype order, queue and workshop behaviour.

I exported the same test run in JUnit format for Jenkins. The XML contained 55 test cases, with no failures, errors or skipped tests, matching the console summary. This confirms the local report is valid; publication in Jenkins Test Result has not yet been verified.

The database suite ran against PostgreSQL 17 in an isolated Docker container. Schema and permission assertions passed, and the test changes were rolled back. These results do not cover hosted Supabase authentication or Realtime delivery.

## Runtime and failure checks

I built frog-app:local and ran it directly on port 8090. The container became healthy. In the browser, I created order FG-0008 with two items, a total of 50,000 VND and Paid status. The details remained after reload, and the browser console showed no errors during that check.

The smoke script checked health status, the expected commit, both locale pages, the prototype page and its main JavaScript asset. All six assertions passed. I also supplied an incorrect expected commit and stopped the container. Both negative cases returned exit code 1. Restarting the container restored six passing assertions and exit code 0.

I then ran the local image through Docker Compose on port 8091. Compose completed successfully, the container became healthy and the smoke checks passed again. This verifies local container operation and stop/start recovery; deployment rollback still requires its own runtime test.

## Deployment preparation

The application changes were committed as 4bd958a, followed by Jenkinsfile and the deployment files in 4abfda4. The pipeline is configured to run quality gates before building and publishing the image, deploy to test, verify it, and request production approval by default. Its unit stage removes the old JUnit report before generating a new one. The deployment scripts check health and commit information and include a recovery path.

These files are prepared, but the complete pipeline has not run. Jenkinsfile validation, SSH deployment, production approval and rollback still depend on the Jenkins and server environments.

## Issues and remaining work

The initial formatting check failed. I added an LF rule and applied Prettier; the later check passed. A missing local Next.js executable was restored by npm ci, after which the standalone build succeeded. An initial Docker Engine connection failure was followed by a successful build once the engine was available.

The local image still reports commit local, and the checks were performed as the working tree developed. I therefore cannot yet claim that a deployed image matches the final Git commit. The next integration checks must establish that match, demonstrate a failing unit test blocking deployment, verify approval, and confirm rollback using the restored release's health response.
