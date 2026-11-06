# Role 2 – CI/CD pipeline lead

**You own:** the Jenkins server's configuration, plugins, users and credentials, the Docker Hub repository,
the `frog-pipeline` job, the GitHub webhook, and the approval step.
**Levels:** 1 (CI/build server + test stages), 3 (deployment), 4 (automatic trigger + deferred deployment).
**You need from others:** Jenkins URL and initial admin password (Role 1), the deploy SSH key `frog-key.pem`
(Role 1), the repository changes merged to `main` (Role 3), admin rights on the GitHub repo for the webhook
(repo owner).
**You hand over:** H5 (Jenkins logins), H6 (build history).

The pipeline itself is already written: `Jenkinsfile` in the repo (see `repo-files/Jenkinsfile`). Your job is to
give Jenkins everything that file expects.

```
Checkout → Install → Static checks → Unit tests → Database tests → Build image → Push image
        → Deploy to test → Smoke test: test → Approve production → Deploy to production → Verify production
```

---

## 1. Unlock Jenkins and install plugins

1. Browser → `http://<jenkins-elastic-ip>:8080` → paste the initial admin password (from Role 1, or
   `sudo cat /var/lib/jenkins/secrets/initialAdminPassword` on frog-jenkins).
2. **Install suggested plugins** (includes Pipeline, Git, GitHub, Credentials Binding, Timestamper, JUnit).
3. Create the first admin user, then set **Jenkins URL** to `http://<jenkins-elastic-ip>:8080/` → Save and Finish.
4. Manage Jenkins → **Plugins → Available plugins** → install **SSH Agent** (not in the suggested set) → restart
   when done (`http://<ip>:8080/safeRestart`).

📷 the plugin list showing SSH Agent, GitHub, Pipeline, Timestamper, JUnit.

## 2. Users and security

- Manage Jenkins → **Users → Create User**: one account per member (H5). Share each password privately.
- Manage Jenkins → **Security**: Authorization **"Logged-in users can do anything"**; untick *Allow anonymous read access*.
- Manage Jenkins → **Nodes → Built-In Node → Configure**: Number of executors **2** (the approval wait holds one).

## 3. Docker Hub repository and token

1. hub.docker.com → **Repositories → Create repository** → name `frog-app`, visibility **Public** (anonymous pulls
   from the servers, no extra login there).
2. Account settings → **Personal access tokens → Generate new token**: description `jenkins`, permissions
   **Read & Write** → copy the token (shown once).

## 4. Credentials

Manage Jenkins → **Credentials → System → Global credentials → Add Credentials**. The IDs must be exactly these:

| ID | Kind | Values |
|---|---|---|
| `dockerhub` | Username with password | Username = Docker ID, Password = the access token |
| `app-servers-ssh` | SSH Username with private key | Username `ec2-user`; Private Key → Enter directly → paste all of `frog-key.pem` |

📷 the credentials list (values are hidden – safe to screenshot).

## 5. Global environment variables

Manage Jenkins → **System → Global properties → Environment variables** → Add:

| Name | Value |
|---|---|
| `DOCKERHUB_REPO` | `<docker-id>/frog-app` |
| `TEST_HOST` | private IP of frog-test (from Role 1) |
| `PROD_HOST` | private IP of frog-prod |
| `NEXT_PUBLIC_SUPABASE_URL` | *(optional; leave out to deploy the setup screen)* |
| `NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY` | *(optional; browser-safe key, not the service-role key)* |

Save. (Private IPs never change; Jenkins reaches the servers inside AWS.)

## 6. Check Jenkins can use its tools

Manage Jenkins → **Script Console** is not needed – just run a throw-away job:
New Item → `toolcheck` → **Pipeline** → script:
```groovy
pipeline { agent any; stages { stage('tools') { steps {
  sh 'node --version && npm --version && docker version && docker image ls postgres'
  sshagent(credentials: ['app-servers-ssh']) {
    sh 'ssh -o StrictHostKeyChecking=accept-new ec2-user@$TEST_HOST "hostname && docker ps"'
    sh 'ssh -o StrictHostKeyChecking=accept-new ec2-user@$PROD_HOST "hostname && docker ps"'
  }
}}}}
```
Build Now → Console Output must show Node 24, Docker, and both server hostnames. Delete the job afterwards.

| If you see | Fix |
|---|---|
| `permission denied ... docker.sock` | `sudo usermod -aG docker jenkins && sudo systemctl restart jenkins` on frog-jenkins |
| `node: not found` | the user data did not finish – ask Role 1; `ls -l /usr/local/bin/node` |
| `Permission denied (publickey)` | `app-servers-ssh` must be user `ec2-user` with the whole `frog-key.pem` including the BEGIN/END lines |
| `No such DSL method 'sshagent'` | SSH Agent plugin missing or Jenkins not restarted |

## 7. Create the pipeline job

(After Role 3 has merged the repository changes – H4.)

New Item → name **`frog-pipeline`** → **Pipeline** → OK:
- **General → GitHub project**: `https://github.com/NhatNguyen2407/frog-management/`
- **Triggers**: tick **GitHub hook trigger for GITScm polling**
- **Pipeline → Definition**: *Pipeline script from SCM* · SCM **Git** · Repository URL
  `https://github.com/NhatNguyen2407/frog-management.git` · Branch `*/main` · Script Path `Jenkinsfile`
  (public repo → no credentials; if it becomes private, add a GitHub personal access token as a credential here)
- Save → **Build Now** (the first run also registers the `AUTO_PROMOTE` parameter).

The first build takes 5–10 minutes (npm and Docker caches are empty). Watch **Console Output** or the
**Pipeline Overview** graph. It stops at **Approve production**: open the build → click **Deploy**.
📷 the stage view with every stage green, the approval prompt, and the Console Output end
(`Release <sha> is live on http://...`).

Check from your laptop: `http://<prod-elastic-ip>/vi` and `http://<prod-elastic-ip>/api/health`.

## 8. GitHub webhook (automatic trigger, Level 4)

Needs admin on the repository – the repo owner does this, or adds you as an admin collaborator.

GitHub repo → **Settings → Webhooks → Add webhook**:
- Payload URL: `http://<jenkins-elastic-ip>:8080/github-webhook/` (**trailing slash required**)
- Content type: `application/json` · Events: **Just the push event** · Active → Add webhook

GitHub sends a ping; **Recent Deliveries** must show a green tick (HTTP 200). Push any commit to `main` → a new
`frog-pipeline` build starts within seconds, labelled "Started by GitHub push".
📷 Recent Deliveries 200 + the build cause.

If deliveries fail: port 8080 not open in `jenkins-sg`, Jenkins URL wrong, or the slash missing. Fallback in the
job: Triggers → **Poll SCM** `H/2 * * * *` (checks every 2 minutes).

## 9. Deferred vs automatic deployment

- Default (webhook builds): the build waits at **Approve production** for up to 2 hours → a member clicks
  **Deploy** = deferred deployment. Not approved in time → build is *aborted*, production untouched.
- **Build with Parameters → AUTO_PROMOTE ✓** → the approval stage is skipped = fully automatic deployment.
- Rollback is automatic: `deploy.sh` on the server rolls back if the new container never becomes healthy, and the
  *Verify production* stage rolls back if the smoke test fails. Manual rollback:
  `ssh ec2-user@<prod> "cd ~/frog && ./deploy.sh --rollback"`.

## 10. Keep it healthy

- Disk: `docker system df` on frog-jenkins; clean with `docker system prune -af` if it nears 20 GiB.
- Build history is kept for 30 builds (Jenkinsfile `buildDiscarder`).
- Back up the job: `/var/lib/jenkins/jobs/frog-pipeline/config.xml` (the pipeline itself is in Git).

---

## Troubleshooting the pipeline

| Stage fails | Likely cause → fix |
|---|---|
| Static checks – `format:check` | the 21 unformatted upstream files → Role 3's formatting commit (their step 2) |
| Database tests – `Unable to find image 'postgres:17'` | `docker pull postgres:17` on frog-jenkins (the stage also pulls it) |
| Database tests – container name in use | a previous run was killed: `docker rm -f frog-operations-phase1-tests` |
| Build image – killed / out of memory | check the 2 GiB swap exists (`free -h`); resize to t3.large if needed |
| Push image – `denied` | `DOCKERHUB_REPO` must start with the Docker ID that owns the token; token needs Write |
| Deploy – `Host key verification failed` | the scripts use `accept-new`; delete stale entries: `sudo -u jenkins ssh-keygen -R <ip>` |
| Deploy – `docker: permission denied` on the server | ec2-user not in the docker group yet → Role 1 reboots the server once |
| Smoke test – FAIL `/api/health` HTTP 000 | app container not up: on the server `cd ~/frog && docker compose --env-file current.env logs app` |

## Evidence checklist (for your report section)

Save console output as text as well as screenshots, and log every error in the shared issues log when it happens
([testing-and-report-plan.md](testing-and-report-plan.md)). Role 4 runs the acceptance tests on your part afterwards.

- [ ] Plugins and credentials pages · global environment variables (blur the IPs if you like)
- [ ] `frog-pipeline` configuration (SCM + trigger) · first green run stage view · approval prompt
- [ ] Docker Hub repo showing tags = commit hashes · webhook Recent Deliveries 200
- [ ] A build "Started by GitHub push" · an AUTO_PROMOTE run · a red build that stopped before deployment

## Done when

- [ ] A push to `main` starts the pipeline automatically, deploys to test, waits for approval and deploys to production
- [ ] Every member can log in to Jenkins and approve a deployment
