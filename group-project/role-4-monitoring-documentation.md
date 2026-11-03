# Role 4 – Monitoring and documentation lead

**You own:** the production instrumentation (Level 2), the success-metric evidence, and assembling the
presentation and final report.
**Levels:** 2, plus the evidence for every level.
**You need:** SSH access to frog-prod and its Elastic IP (Role 1, H3); later, Jenkins access (Role 2, H6).

What runs on the production server (all in `monitoring/compose.yaml`, next to the app):

| Container | Collects / does | Port |
|---|---|---|
| node-exporter | host CPU, memory, disk, network | 9100 (host network, not opened in the security group) |
| cadvisor | CPU and memory per container (app + monitoring) | internal |
| blackbox | probes `http://host/api/health` every 15 s → availability + response time | internal |
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
→ browser `http://localhost:9090/targets`: **node, cadvisor, app-health, prometheus all UP**.
`http://localhost:9090/alerts` lists AppDown, AppSlow, HighCpu, HighMemory, DiskAlmostFull.
📷 both pages.

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
- **Alert:** on production `docker run --rm -d --name burn alpine sh -c "yes > /dev/null"` (×2 for 2 vCPUs) →
  after 5 min *HighCpu* is **Firing** at `localhost:9090/alerts` → `docker rm -f burn`. 📷 firing alert.
- **Outage:** `cd ~/frog && docker compose --env-file current.env stop` → *App up* turns red, *AppDown* fires after
  1 min → `docker compose --env-file current.env start`. (Do this on a quiet moment – it is a real outage.)
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
| Response time (95%) | Prometheus: `quantile_over_time(0.95, probe_duration_seconds{job="app-health"}[1h])` | < 0.5 s |
| Resource headroom | Grafana CPU / Memory panels | averages during normal use |
| Reproducibility | Role 1's timed rebuild | minutes |
| Security hygiene | `git log -p | Select-String -Pattern "token|password|BEGIN .*PRIVATE"` on a clone; security-group review | 0 findings |

Keep a shared spreadsheet: build number, commit, start, end, result, approved by, notes. It makes the report's
metrics section a copy-paste.

## 6. Evidence and report

**Report** (evidence of every sub-task, captioned screenshots, console output, errors and fixes, how well each
level was met against the metrics, lessons learned). Suggested structure:

1. Introduction and final architecture (update Figure 1 of the brief if anything changed)
2. Level 1 – servers (Role 1 evidence), Jenkins set-up and pipeline stages (Role 2), test stages (Role 3)
3. Level 2 – monitoring stack, targets, dashboard, load/alert/outage demos (this guide)
4. Level 3 – application deployment to test and production, smoke tests, `/api/health`
5. Level 4 – webhook-triggered build, deferred approval, AUTO_PROMOTE run, blocked failing build, rollback
6. Success metrics – table of target vs achieved (section 5 above)
7. Problems and resolutions – e.g. the format-check baseline, the `/api/health` locale redirect, and the change from the Learner Lab (brief) to a team AWS account – why, and the cost controls used
8. Lessons learned and future work (Terraform/Ansible, HTTPS, Kubernetes, notifications)

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
- [ ] CloudWatch graph · metrics spreadsheet · collected screenshots from Roles 1–3

## Done when

- [ ] The dashboard shows live host, container, availability and response-time data for production (Level 2)
- [ ] Every success metric has a measured value; report and slides drafted from the evidence
