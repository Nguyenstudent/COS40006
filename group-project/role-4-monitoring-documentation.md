# Role 4 – Monitoring, testing and maintenance lead

**You own:** the production instrumentation (Level 2); testing every part of the pipeline (acceptance and fault
tests); the issues log and the full write-up of every issue; routine maintenance checks; the success metrics.
**Not only yours:** Roles 1–3 take their own screenshots and console logs while they build and write their own
report sections. The split is in [testing-and-report-plan.md](testing-and-report-plan.md).
**Levels:** 2, plus testing of 1, 3 and 4.
**You need:** SSH access to frog-prod and its Elastic IP (Role 1, H3); later, Jenkins access (Role 2, H6).

What runs on the production server (all in `monitoring/compose.yaml`, next to the app):

| Container | Collects / does | Port |
|---|---|---|
| node-exporter | host CPU, memory, disk, network | 9100 (host network, not opened in the security group) |
| cadvisor | CPU and memory per container (app + monitoring) | internal |
| blackbox | probes `http://host/api/health` and the start page `/vi` every 15 s → availability + response time | internal |
| prometheus | scrapes the three above every 15 s, stores 15 days, evaluates `alert-rules.yml` | 127.0.0.1:9090 (SSH tunnel only) |
| grafana | dashboards, provisioned with the Prometheus data source and the *Frog Management – Production overview* dashboard | 3000 (`grafana-sg`) |

---

## 1. Deploy the monitoring stack

**[PS]** from your clone of the repository (after the devops files are merged), copy the folder to production:
```powershell
scp -r monitoring ec2-user@<prod-elastic-ip>:~/
ssh ec2-user@<prod-elastic-ip>
```
**[EC2]**
```bash
cd ~/monitoring
cp .env.example .env
nano .env                      # set GRAFANA_ADMIN_PASSWORD to something strong; Ctrl+O, Enter, Ctrl+X
docker compose up -d
docker compose ps              # 5 containers Up
free -h                        # memory still has headroom on the t3.small
```
📷 `docker compose ps`.

> You can do this before the app is deployed – the health probe will simply show *down* until Role 2's first
> production deployment.

## 2. Check Prometheus is scraping

**[PS]** open a tunnel (keep the window open): `ssh -L 9090:localhost:9090 ec2-user@<prod-elastic-ip>`
→ browser `http://localhost:9090/targets`: **node, cadvisor, app-health, app-page, prometheus all UP**.
`http://localhost:9090/alerts` lists AppDown, AppSlow, TargetDown, HighCpu, HighMemory, DiskAlmostFull.
📷 both pages.

**[EC2]** check the configuration and the alert timings (acceptance tests M1 and M2 in the plan). Save the output
as text; it is report evidence:
```bash
cd ~/monitoring
docker compose exec prometheus promtool check config /etc/prometheus/prometheus.yml
docker compose exec prometheus promtool check rules /etc/prometheus/alert-rules.yml
docker compose run --rm blackbox --config.check --config.file=/etc/blackbox/blackbox.yml
docker run --rm -v "$PWD":/m -w /m --entrypoint promtool prom/prometheus:v3.5.0 test rules alert-rules.test.yml
```
`alert-rules.test.yml` checks that AppDown fires 1 minute after the first failed probe, TargetDown after 2 minutes,
and HighCpu about 9 minutes after the CPU becomes fully busy. These files were checked only as valid YAML before
the hand-over, so this is their first real run: if a test fails, log it as an issue.

## 3. Grafana

Browser `http://<prod-elastic-ip>:3000` → login `admin` / your password → Dashboards → *Frog Management* →
**Frog Management – Production overview**:
App up · Availability (24 h) · Health-check response · Server uptime · CPU · Memory · Disk · Network ·
Container memory · Container CPU · Response time over time.

Extra detail (optional): Dashboards → New → **Import** → ID **1860** (*Node Exporter Full*) → data source Prometheus.
Create a viewer account per member: Administration → Users → New user.
📷 the overview dashboard with live data (after the app is deployed).

## 4. Show the instrumentation working (for the demo and report)

- **Load:** from frog-jenkins or your laptop (Apache Bench: `sudo dnf install -y httpd-tools` on Jenkins):
  `ab -n 3000 -c 30 http://<prod-private-or-elastic-ip>/vi` → CPU, network and container CPU rise on the
  dashboard; response time increases. 📷 before/during.
- **Alert:** on production `docker run --rm -d --name burn1 alpine sh -c "yes > /dev/null"` (and `burn2`, for
  2 vCPUs) → *HighCpu* is **Firing** at `localhost:9090/alerts` after **about 9 minutes**, not 5: the 5-minute rate
  needs about 4 minutes to pass 80 %, then `for: 5m` adds 5 more → `docker rm -f burn1 burn2`. 📷 firing alert.
- **Outage:** `cd ~/frog && docker compose --env-file current.env stop` → *App up* turns red, *AppDown* fires after
  about 1–1.5 min → `docker compose --env-file current.env start`. (Do this on a quiet moment – it is a real outage.)
- The full list (exporter down, bad release, memory headroom, external check) with predictions is in section 5 of
  the [plan](testing-and-report-plan.md). Write each prediction down before the test and compare it afterwards.
- **CloudWatch cross-check:** AWS console → EC2 → frog-prod → **Monitoring** tab: CPU and network graphs over the
  same period (5-minute granularity) should show the same load spike. 📷 side by side.

## 5. Collect the success metrics (brief Table 6)

| Metric | Where to read it | How |
|---|---|---|
| Lead time (commit → production, excl. approval) | Jenkins build → Pipeline Overview / stage durations | sum of stage times minus *Approve production* |
| Deployment frequency | Jenkins `frog-pipeline` history; Docker Hub tags | count successful builds that reached *Verify production* |
| Build success rate | Jenkins history (last 20 builds on main) | green ÷ total |
| Quality gate | each build's stage view + *Test Result* | no build deployed with a failed test stage |
| Change failure rate | builds where *Verify production* failed / rolled back | failed ÷ production deployments |
| Time to restore | rollback stage duration / `deploy.sh --rollback` timing | seconds from failure to healthy |
| Availability | Grafana stat *Availability (last 24 h)* or `avg_over_time(probe_success[...])` | % over the demo window |
| Response time (95%) | Prometheus: `quantile_over_time(0.95, probe_duration_seconds{job="app-page"}[1h])` (the start page, as in the brief) | < 0.5 s |
| Resource headroom | Grafana CPU / Memory panels | averages during normal use |
| Reproducibility | Role 1's timed rebuild | minutes |
| Security hygiene | `git log -p | Select-String -Pattern "token|password|BEGIN .*PRIVATE"` on a clone; security-group review | 0 findings |

Keep a shared spreadsheet: build number, commit, start, end, result, approved by, notes. It makes the report's
metrics section a copy-paste.

## 6. Testing, issues and the report

Follow [testing-and-report-plan.md](testing-and-report-plan.md):
- run the **acceptance tests** (section 4) on each part as soon as its owner says it is done, and again after any
  pipeline change;
- run the **fault tests** (section 5) once the app is in production;
- keep the **issues log** (section 3) complete: every row gets an investigation, cause, fix, verification and
  prevention, then an E-entry in the report;
- do the **maintenance checks** (section 6) every work session.

Your report sections: Level 2 – monitoring; Testing; Issues and resolutions; Success metrics (section 7 of the
plan lists every section and its owner). Lessons learned and future work (Terraform/Ansible, HTTPS, Kubernetes,
alert notifications) are written by the team.

**Presentation** (Week 11, 4–8 slides, 15 min + Q&A): ① problem & goal ② pipeline diagram ③ tools and why
④ live demo: push → Jenkins → test → approve → production (Role 3's demo A) ⑤ Grafana during load ⑥ blocked
build (demo B) ⑦ metrics achieved ⑧ problems & lessons. Have screenshots ready in case a server is down, and start the instances well before the session.

## Troubleshooting

| Problem | Fix |
|---|---|
| `set GRAFANA_ADMIN_PASSWORD in .env` | create `.env` in `~/monitoring` (step 1) |
| target `node` DOWN | node-exporter uses the host network: `curl localhost:9100/metrics` on the server; `extra_hosts` must be in prometheus' service |
| target `app-health` UP but probe_success 0 | app not deployed yet, or `/api/health` not returning `"status":"ok"` |
| cadvisor restarting | check `docker compose logs cadvisor`; cgroup v2 on Amazon Linux 2023 needs the volumes as given |
| Grafana not reachable | `grafana-sg` not attached to frog-prod, or your IP changed (rule set to My IP) |
| Dashboard empty | wrong time range (top right), or Prometheus data source not "Prometheus" (uid `prometheus`) |
| Memory above 80% | `docker stats --no-stream`; ask Role 1 to resize frog-prod to t3.medium (Stop → Change instance type → Start) |

## Evidence checklist

- [ ] `docker compose ps` (monitoring) · Prometheus targets UP · alert rules page
- [ ] Grafana overview with data · load test before/during · a firing alert · outage + recovery
- [ ] CloudWatch graph · metrics spreadsheet · promtool output (M1, M2)
- [ ] Acceptance-test records (plan section 4) · fault-test records with predictions (plan section 5) · complete issues log

## Done when

- [ ] The dashboard shows live host, container, availability and response-time data for production (Level 2)
- [ ] Every success metric has a measured value; acceptance and fault tests recorded
- [ ] Every issues-log row is written up in the report; your report sections drafted
