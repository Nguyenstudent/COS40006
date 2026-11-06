# Testing, issue tracking and report plan (whole team)

The marker's rubric gives **high weight to capturing issues, analysing them and resolving them**. This plan makes sure
every problem we hit is recorded the moment it happens, tested, explained and verified, and that each member
writes the report section for the part they built.

## 1. Who does what

| | Roles 1–3 (Infrastructure, CI/CD, Application) | Role 4 (Monitoring, testing and maintenance) |
|---|---|---|
| While building | Screenshots **and console output saved as text**, using the evidence checklist at the end of your guide | Monitoring stack on production ([role-4 guide](role-4-monitoring-documentation.md)) |
| When something fails | Add a row to the **issues log** straight away (section 3): the exact error text, the command, the time | Investigates with the owner, completes the row, and writes the full entry for the report |
| After a part is built | Fix what the acceptance tests find in your part | Runs the **acceptance tests** (section 4) on every part and records the results |
| Ongoing | Stop the servers when you finish (Role 1 step 10) | **Maintenance checks** (section 6) and the **fault tests** (section 5) |
| Report | Write **your own deployment section** (section 7) | Writes the testing, monitoring, issues and success-metrics sections |

Role 1 is already finished: write the Infrastructure section from the evidence you captured. Role 4 still re-runs
the infrastructure acceptance tests (I1–I5), because the later roles depend on them.

## 2. Capture while you work (everyone)

- **Record the terminal.** PowerShell: `Start-Transcript -Path .\logs\$(Get-Date -Format yyyyMMdd-HHmm).txt` (and
  `Stop-Transcript` at the end). On a server: `script -a ~/logs/$(date +%F).log` (`exit` to stop). If you forgot,
  copy the text from the terminal before you close it. You cannot reconstruct an exact error message later.
- **Print the time before important steps** (`date -u` or `Get-Date`) so console output, screenshots, Jenkins
  builds and Grafana graphs can be matched up.
- **Keep failed attempts.** A first fix that did not work, and why, is analysis the rubric rewards. Do not delete it.
- **Warnings count as issues** (compiler warnings, deprecation notices, a red row in a log). Log them too.
- If you fixed something before capturing it, **reproduce it on purpose** and say so in the entry
  ("I reproduced the error by …").
- Never paste secrets: blur tokens, passwords and private keys in screenshots and replace them with `[redacted]` in text.

## 3. Issues log (shared spreadsheet)

One row per issue, added by whoever hits it. Role 4 fills in the remaining columns with the owner. Header to paste:

```text
ID,Date/time (UTC),Found by,Part,Command or action,Exact error text,Investigation (what was checked + what it showed),Root cause,Fix,Rejected alternative,Verification (command + output),Prevention,Report section,Status
```

`Part` is one of: Infrastructure, CI/CD, Application, Monitoring, Team/process. `Status`: open, fixed, unresolved.

### Write-up for the report (Role 4, one per issue)

```text
E<n> – <error code>: <short message>                (where: build / deploy / runtime / monitoring)
Console: the command that failed and its verbatim output (trim with [...])
Investigation: what was checked and the output that proved it (log line, config value, metric, event)
Cause: the mechanism, and why the symptom was misleading if it was
Fix: the change made, plus the alternative we rejected and why
Verification: the command re-run, and the output showing it is fixed
Prevention: the script or config change that stops it happening again
```

The issues section ends with a **summary table** (ID · error · root cause · resolution · status). Unresolved issues
stay in, with what we tried and what would fix them.

Issues we already know about (real, from preparing the kit): upstream `main` failed `npm run format:check` on 21
files; `/api/health` was redirected by the locale middleware until `src/proxy.ts` excluded `api`; the move from the Learner Lab (in the brief) to a team AWS account; Docker Desktop was unavailable on the
laptop, so the containers were first tested on the servers; the brief's response-time metric (start page) was not
probed by the first monitoring configuration, so the `app-page` probe was added.

## 4. Acceptance tests (Role 4 runs them; the owner of the part fixes failures)

Record each as: ID, date, command, output (text), pass/fail. A failure becomes an issue in the log.

| ID | Part | Test | How | Pass when |
|---|---|---|---|---|
| I1 | Infra | SSH to all three servers by Elastic IP | `ssh -i frog-key.pem ec2-user@<eip> 'hostname; docker version --format {{.Server.Version}}'` | 3 hostnames + Docker versions |
| I2 | Infra | Only the planned ports are open | EC2 → Security groups → inbound rules of `jenkins-sg`, `app-sg`, `grafana-sg` | 22 from team IPs only; 80 on app servers; 8080 Jenkins; 3000 Grafana from team IPs; nothing else |
| I3 | Infra | Cost controls | Billing → Budgets | both budgets exist with email alerts |
| I4 | Infra | Reproducibility (metric) | Rebuild frog-test from `infra/user-data-app-server.sh`, timed | ready in < 30 min |
| I5 | Infra | IAM: members are not root | IAM → Users / groups | one user per member, MFA on |
| C1 | CI/CD | A push starts the pipeline | Push a commit; GitHub → Settings → Webhooks → Recent deliveries | delivery 200; build "Started by GitHub push" |
| C2 | CI/CD | A failing test blocks deployment | Role 3 §6 demo B (a failing assertion pushed to `main`) | build red at the test stage; test and production servers unchanged |
| C3 | CI/CD | Deferred deployment waits | A normal build | pauses at *Approve production* until someone approves |
| C4 | CI/CD | Image tag = commit | Docker Hub tags vs `git log --oneline -1` | same short hash |
| C5 | CI/CD | Secrets are masked | Search the Jenkins console log for the Docker Hub token and the key | only `****` |
| A1 | App | Smoke tests on both servers | `deploy/smoke-test.sh http://<test-eip>` and `http://<prod-eip>` | all PASS, exit 0 |
| A2 | App | The deployed build is the one we pushed | `curl http://<prod-eip>/api/health` | `"commit"` = the build's commit |
| A3 | App | Automatic rollback | Role 3 §6 demo C on the test server: `./deploy.sh <repo> doesnotexist` | *Automatic rollback*; previous release keeps serving |
| A4 | App | Level 4 change appears | Change `RELEASE` / the visible version label, push, approve | new value on production without manual copying |
| S1 | All | No secrets in Git | `git log -p \| Select-String -Pattern "token\|password\|BEGIN .*PRIVATE"` on a fresh clone | 0 real findings |
| M1 | Monitoring | Configuration is valid | `docker compose exec prometheus promtool check config /etc/prometheus/prometheus.yml`, `docker compose exec prometheus promtool check rules /etc/prometheus/alert-rules.yml`, `docker compose run --rm blackbox --config.check --config.file=/etc/blackbox/blackbox.yml` | SUCCESS / config OK |
| M2 | Monitoring | Alert timings | `docker run --rm -v "$PWD":/m -w /m --entrypoint promtool prom/prometheus:v3.5.0 test rules alert-rules.test.yml` | SUCCESS (3 tests) |
| M3 | Monitoring | Every target scraped | `curl -s localhost:9090/api/v1/targets \| grep -o '"health":"[a-z]*"'` | 5 × `"health":"up"` (prometheus, node, cadvisor, app-health, app-page) |
| M4 | Monitoring | Dashboard has data | Grafana → *Production overview*, last 1 h | no empty panels |
| M5 | Monitoring | Monitoring survives a reboot | Stop and start frog-prod | all containers back; Grafana dashboard and Prometheus history still there |

## 5. Fault tests (Role 4) – predict, break, measure, explain

Write the prediction down **before** each test, then compare it with what happened. The difference, and its
explanation, is the analysis. Run `date -u` at T0, and record the alert times with
`curl -s localhost:9090/api/v1/alerts` (shows `pending` / `firing` and `activeAt`).

| ID | Fault | How (on frog-prod unless stated) | Prediction | Measure |
|---|---|---|---|---|
| F1 | App outage | `cd ~/frog && docker compose --env-file current.env stop` → wait → `start` | AppDown firing ~1 min 15 s – 1 min 30 s after T0 (≤ 15 s to the next probe + ≤ 15 s evaluation + `for: 1m`; M2 tests the `for` part) | T0 → pending → firing → recovered; availability lost |
| F2 | CPU saturation | `docker run --rm -d --name burn1 alpine sh -c "yes > /dev/null"` (and `burn2`), then `docker rm -f burn1 burn2` | HighCpu firing ~9 min after T0, not 5: the 5-minute rate needs ~4 min to pass 80 %, then `for: 5m` | firing time; response time during the burn; CloudWatch `CPUCreditBalance` (t3 is burstable) |
| F3 | Load test | `ab -n 3000 -c 30 http://<prod-eip>/vi` from frog-jenkins | CPU and container CPU rise; start-page response time rises; does AppSlow fire? | 95th percentile response time during the test |
| F4 | Exporter down | `docker compose stop node-exporter` (in `~/monitoring`) → wait → `start` | Host panels go empty; TargetDown firing ~2 min after T0 | what you see on the dashboard while it is down |
| F5 | Bad release | With Role 3, at a quiet moment: `./deploy.sh <repo> doesnotexist` on production, then `./deploy.sh --rollback` | no `probe_success` dip for the bad tag (it is rejected before the switch); a short dip or none for the rollback | time to restore (brief metric: < 10 min) |
| F6 | Memory headroom | `docker stats --no-stream` and the Memory panel at idle and during F3 | below 80 % on the 2 GiB t3.small | headroom; does HighMemory fire? |
| F7 | External view | From your laptop: `curl -o /dev/null -s -w "%{http_code} %{time_total}\n" http://<prod-eip>/vi` during F1 | fails with the app stopped. The probes are internal (`host.docker.internal`), so only this check covers the security group and the Elastic IP | external vs internal response time |
| F8 | CloudWatch cross-check | EC2 → frog-prod → Monitoring, same window as F2/F3 | same spike, smoother (5-minute points vs 15 s) | screenshot side by side and the explanation |

## 6. Maintenance checks (Role 4)

| When | Check | Command / place |
|---|---|---|
| Every work session | Alerts page empty or explained; all targets up | `localhost:9090/alerts` (tunnel), M3 |
| Every work session | Disk space | `df -h /`, `docker system df` – prune old images with `docker image prune -a --filter "until=72h"` if > 70 % |
| Every work session | Servers stopped at the end | EC2 console; budget email inbox |
| Every pipeline change | Re-run A1, A2 and C2 | – |
| Weekly | Jenkins disk and plugin updates | Manage Jenkins → System information / Plugins (update only with the CI/CD lead) |

Log every finding in the issues log, even if it was fixed in a minute.

## 7. Report structure and owners

Captions: `Figure n:` for screenshots, `Console n:` for pasted terminal text, `Table n:` for tables. Each section
shows what was done, the evidence, and points to the issues (E-numbers) found in that part.

| # | Section | Written by |
|---|---|---|
| 1 | Introduction, final architecture (update the brief's Figure 1), change from the Learner Lab to a team account | team (agree at a meeting) |
| 2 | Level 1 – infrastructure: account, IAM, servers, security groups, Elastic IPs, cost controls | Role 1 |
| 3 | Level 1 – CI/CD: Jenkins set-up, credentials, pipeline stages, webhook | Role 2 |
| 4 | Level 1/3 – application changes, quality gates, Docker image, deployment to test and production | Role 3 |
| 5 | Level 2 – monitoring stack, dashboard, alerts | Role 4 |
| 6 | Level 4 – push → build → approval → production; blocked build; rollback | Roles 2 and 3 |
| 7 | Testing – acceptance and fault test results (sections 4 and 5) | Role 4 |
| 8 | Issues and resolutions – E-entries and the summary table | Role 4, with each owner checking their entries |
| 9 | Success metrics – target vs achieved vs evidence, for every row of the brief's Table 6, and how well each level was met | Role 4 (Role 2 provides the Jenkins figures) |
| 10 | Unresolved problems, lessons learned, future work | team |

Before submitting, one person reads the whole report for a consistent style, figure numbering and cross-references
(for example, every E-number mentioned in a section exists in section 8).
