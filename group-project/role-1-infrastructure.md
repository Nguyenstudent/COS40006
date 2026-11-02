# Role 1 – Infrastructure lead

**You own:** the AWS Academy Learner Lab account that hosts everything, the three EC2 servers, their firewalls
(security groups), fixed addresses (Elastic IPs) and SSH access for the team.
**Levels:** 1 (servers), and you keep everything running for 2–4.
**You need:** a Learner Lab account, the two scripts `repo-files/infra/user-data-jenkins.sh` and
`repo-files/infra/user-data-app-server.sh`.
**You hand over:** H1 (IPs), H2 (deploy SSH key), H3 (production access) – see README.

Where each step happens: **[LL]** Learner Lab page · **[AWS]** AWS console · **[PS]** PowerShell on your laptop · **[EC2]** SSH session on a server.

---

## 1. Start the lab and get the key

1. **[LL]** Start Lab → wait for the green dot → **AWS Details → Download PEM** → save as `labsuser.pem`
   in a folder outside any Git repository (e.g. `C:\keys\labsuser.pem`) – never commit it. It is the private
   key of the key pair **vockey**.
2. **[PS]** Make it private once: `icacls labsuser.pem /inheritance:r /grant:r "$($env:USERNAME):R"`
3. **[LL]** Click **AWS** → the console opens in **us-east-1 (N. Virginia)**. Stay in this region.

📷 Learner Lab page (green dot, remaining budget).

## 2. Security groups

**[AWS]** EC2 → Network & Security → **Security Groups → Create security group** (VPC: the default one). Create three:

| Name | Inbound rules | Used by |
|---|---|---|
| `jenkins-sg` | SSH 22 – Anywhere-IPv4 (key-only login; home IPs change) · Custom TCP **8080** – Anywhere-IPv4 (GitHub webhooks + team browsers) | frog-jenkins |
| `app-sg` | SSH 22 – Anywhere-IPv4 · HTTP **80** – Anywhere-IPv4 | frog-test, frog-prod |
| `grafana-sg` | Custom TCP **3000** – *My IP* of each member (or Anywhere-IPv4 during the demo, with a strong Grafana password) | frog-prod only |

Outbound: leave "All traffic". Prometheus (9090) and the exporters (9100, 9115) are **not** opened – they are reached through SSH tunnels.

📷 the inbound rules of each group.

> Tighter alternative: allow SSH on `app-sg` only from source `jenkins-sg` plus your IP. That works because
> Jenkins talks to the servers through their **private** IPs (see step 6).

## 3. Launch the three instances

**[AWS]** EC2 → Instances → **Launch instances**, three times:

| Setting | frog-jenkins | frog-test | frog-prod |
|---|---|---|---|
| AMI | Amazon Linux 2023, **64-bit (x86)** | same | same |
| Instance type | **t3.medium** | t3.small | t3.small |
| Key pair | vockey | vockey | vockey |
| Security groups (*Select existing*) | jenkins-sg | app-sg | app-sg **and** grafana-sg |
| Storage | **20 GiB** gp3 | 16 GiB gp3 | 16 GiB gp3 |
| Advanced details → User data | paste all of `user-data-jenkins.sh` | paste all of `user-data-app-server.sh` | same as test |

Wait for **Running** and **2/2 status checks**. 📷 Instances list with all three running.

> If a type is refused ("not authorized"), Learner Lab blocks it – try t3.large for Jenkins or t2.medium/small,
> and tell the team (the brief's Table 1 lists t3.medium/t3.small).

## 4. Elastic IPs (fixed public addresses)

**[AWS]** EC2 → Network & Security → **Elastic IPs → Allocate Elastic IP address** → Allocate. Do it 3 times, then for
each: select → **Actions → Associate Elastic IP address** → Instance → pick the server → Associate.

Without them the public IPs change every time the lab restarts, which breaks the GitHub webhook and every link.
📷 Elastic IPs list showing the association.

## 5. Check the set-up scripts finished

**[PS]** For each server: `ssh -i labsuser.pem ec2-user@<elastic-ip>` (type `yes` the first time;
if it will not accept typing, add `-o StrictHostKeyChecking=accept-new`).

**[EC2]**
```bash
sudo tail -n 20 /var/log/cloud-init-output.log   # ends with JENKINS SETUP DONE / APP SERVER SETUP DONE
docker version                                    # test/prod: run "exit" and ssh in again first if "permission denied"
docker compose version                            # test/prod
```
On **frog-jenkins** also:
```bash
node --version                                    # v24.x
systemctl status jenkins --no-pager | head -5     # active (running)
docker image ls postgres                          # postgres:17 is present
sudo cat /var/lib/jenkins/secrets/initialAdminPassword   # give this to Role 2 privately
```
In a browser: `http://<jenkins-elastic-ip>:8080` shows **Unlock Jenkins**. 📷 each check.

If the log stops with an error: read the last 40 lines (`sudo tail -n 40 ...`). The usual causes are a
temporary download failure (run the failed command again by hand with `sudo`) or the Jenkins repository key
having changed (see Troubleshooting).

## 6. Record the addresses (hand-off H1)

**[AWS]** For each instance note the **Elastic IP** and the **Private IPv4 address** (instance → Details).
Fill in the "Shared values" table in the README and post it in the group chat.

Jenkins will use the **private** IPs as `TEST_HOST` / `PROD_HOST`: they never change, and traffic stays inside AWS.

## 7. SSH access for the team and for Jenkins (hand-off H2)

- **Jenkins deploy key:** the quickest option is `labsuser.pem` itself – give it privately to Role 2, who stores
  it as the Jenkins credential `app-servers-ssh` (user `ec2-user`). Every instance launched with vockey accepts it.
- **Teammates:** each member runs **[PS]** `ssh-keygen -t ed25519` and sends you the content of
  `C:\Users\<them>\.ssh\id_ed25519.pub`. On every server **[EC2]**:
  ```bash
  echo 'ssh-ed25519 AAAA... member@laptop' >> ~/.ssh/authorized_keys
  ```
  They can then `ssh ec2-user@<elastic-ip>` with their own key.

## 8. Run the lab for the team

- Start the lab before agreed work sessions; the instances start with it. Sessions last 4 hours –
  restart the lab (Start Lab again) to extend before it expires.
- After a restart, check `http://<jenkins-ip>:8080` loads and `docker ps` on production shows the app and monitoring containers (they restart by themselves: `restart: unless-stopped`).
- Watch the credit balance on the Learner Lab page. Stop the instances (Instances → Instance state → Stop) when
  the team is not working; Elastic IPs stay attached.

## 9. Reproducibility test (success metric)

Before the presentation, prove a server can be rebuilt from the repository scripts in under 30 minutes:
note the time, **terminate frog-test**, launch a new one exactly as in step 3, associate the same Elastic IP,
let Role 2 run the pipeline (it redeploys the app automatically), note the time again. Update the private IP in
Jenkins (`TEST_HOST`). 📷 timings + the green pipeline run.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| SSH times out | `jenkins-sg`/`app-sg` missing port 22, or wrong IP (Elastic IP vs old public IP) |
| `UNPROTECTED PRIVATE KEY FILE` | run the `icacls` line in step 1 |
| `Permission denied (publickey)` | wrong user (always `ec2-user`) or wrong key file |
| User data did nothing | it only runs at **first** boot; check `/var/log/cloud-init-output.log`, or run the script by hand: `sudo bash user-data-app-server.sh` after copying it with scp |
| `dnf install jenkins` GPG/key error | Jenkins rotated its signing key: open https://pkg.jenkins.io/redhat-stable/ and use the key named there in the `rpm --import` line, then `sudo dnf install -y jenkins` |
| Elastic IP limit reached | release unused ones (Actions → Release); 2 are enough if needed (Jenkins + production) |

## Evidence checklist (for Role 4)

- [ ] Learner Lab running · security groups with rules · 3 instances Running with types visible
- [ ] Elastic IPs associated · cloud-init "DONE" lines · `docker version` on each server
- [ ] Jenkins unlock page in the browser · reproducibility test timings

## Done when

- [ ] All three servers reachable by SSH with their Elastic IPs; Docker works on each; Jenkins page loads
- [ ] Shared values table filled in and posted; Role 2 has the deploy key and the initial admin password
