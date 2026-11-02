# Frog Management DevOps pipeline – implementation guide (team overview)

This folder turns the project brief into work. Each role has its own step-by-step guide:

| Role (brief Table 10) | Guide | Owns | Levels |
|---|---|---|---|
| **1. Infrastructure** | [role-1-infrastructure.md](role-1-infrastructure.md) | AWS Learner Lab account, 3 EC2 servers, security groups, Elastic IPs, SSH access | 1 |
| **2. CI/CD pipeline** | [role-2-cicd-pipeline.md](role-2-cicd-pipeline.md) | Jenkins set-up, plugins, credentials, Docker Hub, the pipeline job, GitHub webhook, approvals | 1, 3, 4 |
| **3. Application and testing** | [role-3-application-testing.md](role-3-application-testing.md) | Repository changes (Dockerfile, health endpoint…), test stages, smoke tests, Level 4 demo | 1, 3, 4 |
| **4. Monitoring and documentation** | [role-4-monitoring-documentation.md](role-4-monitoring-documentation.md) | Prometheus/Grafana on production, success-metric evidence, report and slides | 2 + all |

`repo-files/` holds every file to add to the [frog-management](https://github.com/NhatNguyen2407/frog-management)
repository (same paths as in that repo), and `devops-pipeline.patch` is the same change as one patch.
Role 3 commits them there (step 3 of their guide). It implements the pipeline described in our submitted project brief.

---

## What was already checked (on 2 Nov 2026, with Node.js 24.22.2)

| Check | Result |
|---|---|
| `npm ci`, `npm run typecheck` | pass |
| Unit tests (`node --test`, 55 tests) | **55 pass, 0 fail** |
| `npm run build` with `output: "standalone"` | pass; `/api/health` is a dynamic route |
| Standalone server + `deploy/smoke-test.sh` | `/api/health`, `/vi`, `/en` all PASS; smoke test exits 1 against a dead host |
| `npm run format:check` on upstream `main` | **FAILS – 21 existing files are not Prettier-formatted** → Role 3 fixes this first (one formatting-only commit) |
| New YAML/JSON files | parse correctly and pass Prettier |
| `deploy/deploy.sh` release/rollback logic (with a simulated `docker`) | good → good → bad tag: the bad one is rejected, the previous release keeps serving, and `--rollback` still goes back to the one before |
| `devops-pipeline.patch` | applies cleanly to the current upstream `main` |
| Not tested (no Docker available at the time) | `docker build`, `npm run test:db`, the monitoring containers, Jenkins itself |

---

## Order of work and hand-offs

```
 Day 1  ─ Role 1: servers up ──────────────┐            Role 3: repo changes + formatting commit (laptop)
                                            ▼                         │
 Day 2  ─ Role 2: Jenkins set-up ◀── IPs, keys (from Role 1)          │
          Role 4: monitoring on production ◀── production IP          │
                                            ▼                         ▼
 Day 3  ─ Role 2: pipeline job + webhook ◀── repo changes merged (from Role 3)
          first green build → deployed to test → approved → production
 Day 4+ ─ Role 3: Level 4 demo rehearsal   Role 4: metrics + evidence + report/slides
```

| Hand-off | From → to | What |
|---|---|---|
| H1 | Role 1 → everyone | Elastic IPs and **private** IPs of the 3 servers, security group names, the date/time the lab will be running |
| H2 | Role 1 → Role 2 | SSH key that can log in to the test/production servers as `ec2-user` (for the `app-servers-ssh` credential) |
| H3 | Role 1 → Role 4 | Production server Elastic IP; SSH access |
| H4 | Role 3 → Role 2 | Repository changes merged into `main` (Jenkinsfile, Dockerfile, deploy scripts) |
| H5 | Role 2 → everyone | Jenkins URL and a personal Jenkins login for each member |
| H6 | Role 2 → Role 4 | Build history access for the success metrics |

## Shared values – fill in once and share in the group chat (not in the public repo)

| Name | Value |
|---|---|
| Learner Lab owner (whose AWS account hosts everything) | |
| Jenkins Elastic IP → Jenkins URL `http://<ip>:8080/` | |
| Test server Elastic IP / private IP | / |
| Production server Elastic IP / private IP | / |
| Docker Hub repository (e.g. `ourteam/frog-app`) | |
| GitHub repository | https://github.com/NhatNguyen2407/frog-management |
| Grafana URL `http://<prod-ip>:3000/` | |

## Ground rules

- **One AWS account.** Every Learner Lab is a separate AWS account, so all three servers live in the **Infrastructure lead's** lab. Nobody else can see them in the console; they use SSH, the Jenkins page and Grafana. The servers only run while that person's lab session is started – agree on work times.
- **Secrets never go in Git**: Docker Hub token, SSH keys, Grafana password and any Supabase values live in Jenkins credentials or in `.env` files on the servers.
- **`main` = production.** Work on a branch, open a pull request, merge to `main`; every merge runs the full pipeline.
- **Screenshot as you go.** Every guide ends with an evidence list; Role 4 collects them for the report.
- **Stop instances you are not using** (Role 1) – credits are limited.

## Glossary of names used in all guides

| Name | Meaning |
|---|---|
| `frog-jenkins`, `frog-test`, `frog-prod` | the three EC2 instances |
| `jenkins-sg`, `app-sg`, `grafana-sg` | security groups |
| `frog-pipeline` | the Jenkins pipeline job |
| `dockerhub`, `app-servers-ssh` | Jenkins credential IDs (the Jenkinsfile uses these exact IDs) |
| `DOCKERHUB_REPO`, `TEST_HOST`, `PROD_HOST` | Jenkins global environment variables |
| `~/frog`, `~/monitoring` | folders on the test/production servers |
