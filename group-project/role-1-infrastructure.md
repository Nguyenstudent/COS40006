# Role 1 – Infrastructure lead

**You own:** the team's AWS account (security, logins, cost), the three EC2 servers, their firewalls
(security groups), fixed addresses (Elastic IPs) and SSH access for the team.
**Levels:** 1 (servers), and you keep everything running for 2–4.
**You need:** an email address for the account, a payment card (AWS verifies it even if you only use credits), a
phone for verification and MFA, and the two scripts `repo-files/infra/user-data-jenkins.sh` and
`repo-files/infra/user-data-app-server.sh`.
**You hand over:** H1 (IPs and console logins), H2 (deploy SSH key), H3 (production access) – see README.

Where each step happens: **[AWS]** AWS console in the browser · **[PS]** PowerShell on your laptop · **[EC2]** SSH session on a server.

> **Change from the brief:** the brief planned the AWS Academy Learner Lab. The team now uses its own AWS
> account. Benefits: every member gets a console login, no 4-hour sessions, the servers can stay up between
> work sessions, more instance types. Cost: the account is real, so steps 2 and 10 (billing alarms, stopping
> idle servers) are not optional. Record this change and the reasons in the final report.

---

## 1. Create the account (account owner only, once)

1. **[AWS]** https://aws.amazon.com → **Create an AWS account**. Use a team email you can all reach (e.g. a shared
   Gmail) – not a personal university address that expires. Account name: `frog-devops`.
2. Choose the plan when asked:
   - **Free plan (recommended)** – uses only the sign-up credits; AWS does not charge the card while it is active
     (it ends after 6 months or when the credits run out, and you can upgrade later). Some instance types may be
     restricted to the ones marked *Free tier eligible* (see step 5).
   - **Paid plan** – no restrictions, but anything beyond the credits is billed to the card.
3. Phone verification → support plan **Basic (free)**. New accounts can take from minutes to a few hours before
   EC2 can be used ("account pending verification").

## 2. Secure the root user and set billing alarms (account owner)

1. **[AWS]** Signed in as root: top right account menu → **Security credentials → Assign MFA device** → authenticator
   app. Never create access keys for root; after this step use root only for billing.
2. **Billing and Cost Management → Budgets → Create budget → Use a template**:
   - **Zero spend budget** – emails you as soon as anything costs money beyond the free credits, and
   - **Monthly cost budget** – amount `20` USD, email recipients = the whole team.
3. **Billing → Billing preferences** → tick *Receive Free Tier usage alerts* and *Receive CloudWatch billing alerts*.
4. **Billing → Credits** shows the remaining credit; check it weekly.

📷 MFA assigned · the two budgets · credits page (blur the account ID if you like).

## 3. A console login for every member (IAM)

Everyone gets their own login – no sharing of root or passwords.

1. **[AWS]** IAM → **User groups → Create group**:
   - `frog-admins` with policy **AdministratorAccess** (Infrastructure lead, and one backup member)
   - `frog-team` with policies **AmazonEC2FullAccess** and **CloudWatchReadOnlyAccess** (the other members: they can
     see, start and stop the servers and read metrics, but not change billing or IAM)
2. IAM → **Users → Create user** for each member: tick *Provide user access to the AWS Management Console*,
   *I want to create an IAM user*, custom password, *Users must create a new password at next sign-in* → add to a group.
3. Copy the **console sign-in URL** shown on the IAM dashboard (`https://<account-id>.signin.aws.amazon.com/console`)
   and send each member their username and temporary password privately. Ask everyone to add MFA
   (account menu → Security credentials) on first sign-in.
4. From now on, **you** also work as your IAM admin user, not root.

📷 IAM users list with groups (no passwords visible).

## 4. Region and key pair

1. **[AWS]** Top-right region menu → **Asia Pacific (Singapore) ap-southeast-1** – closest to Vietnam, so SSH and the
   web pages feel fast. Everyone must use the same region, or the servers seem to "disappear".
2. EC2 → Network & Security → **Key pairs → Create key pair**: name `frog-key`, type **RSA**, format **.pem** → it
   downloads once. Keep it **outside any Git repository** (e.g. `C:\keys\frog-key.pem`).
3. **[PS]** Make it private: `icacls C:\keys\frog-key.pem /inheritance:r /grant:r "$($env:USERNAME):R"`

## 5. Security groups

**[AWS]** EC2 → Network & Security → **Security Groups → Create security group** (VPC: the default one). Create three:

| Name | Inbound rules | Used by |
|---|---|---|
| `jenkins-sg` | SSH 22 – Anywhere-IPv4 (key-only login; home IPs change) · Custom TCP **8080** – Anywhere-IPv4 (GitHub webhooks + team browsers) | frog-jenkins |
| `app-sg` | SSH 22 – Anywhere-IPv4 · HTTP **80** – Anywhere-IPv4 | frog-test, frog-prod |
| `grafana-sg` | Custom TCP **3000** – *My IP* of each member (or Anywhere-IPv4 during the demo, with a strong Grafana password) | frog-prod only |

Outbound: leave "All traffic". Prometheus (9090) and the exporters (9100, 9115) are **not** opened – they are reached
through SSH tunnels. 📷 the inbound rules of each group.

> Tighter alternative: allow SSH on `app-sg` only from source `jenkins-sg` plus your IP. That works because Jenkins
> talks to the servers through their **private** IPs (see step 8).

## 6. Launch the three instances

**[AWS]** EC2 → Instances → **Launch instances**, three times:

| Setting | frog-jenkins | frog-test | frog-prod |
|---|---|---|---|
| AMI | Amazon Linux 2023, **64-bit (x86)** | same | same |
| Instance type | **t3.medium** (or **c7i-flex.large** – see note) | t3.small | t3.small |
| Key pair | frog-key | frog-key | frog-key |
| Security groups (*Select existing*) | jenkins-sg | app-sg | app-sg **and** grafana-sg |
| Storage | **20 GiB** gp3 | 16 GiB gp3 | 16 GiB gp3 |
| Advanced details → User data | paste all of `user-data-jenkins.sh` | paste all of `user-data-app-server.sh` | same as test |

Wait for **Running** and **2/2 status checks**. 📷 Instances list with all three running.

> **Free plan accounts:** if t3.medium is refused or not marked *Free tier eligible* in the instance-type list, use
> **c7i-flex.large** (2 vCPU, 4 GiB) for Jenkins – same size class, and listed as Free-Tier eligible at the time of
> writing. Keep t3.small for test/production if it is eligible; otherwise t3.micro works for test but production
> also runs the monitoring stack, so ask the team before going below 2 GiB.

## 7. Elastic IPs (fixed public addresses)

**[AWS]** EC2 → Network & Security → **Elastic IPs → Allocate Elastic IP address** → Allocate. Do it 3 times, then for
each: select → **Actions → Associate Elastic IP address** → Instance → pick the server → Associate.

Without them the public IP changes every time an instance is stopped and started, which breaks the GitHub webhook
and every link. 📷 Elastic IPs list showing the associations.

> AWS charges every public IPv4 address (≈ US$0.005/hour each, attached or not) – about US$11/month for three.
> Release any you stop using (Actions → Release).

## 8. Check the set-up scripts finished

**[PS]** For each server: `ssh -i C:\keys\frog-key.pem ec2-user@<elastic-ip>` (type `yes` the first time;
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

If the log stops with an error, read the last 40 lines (`sudo tail -n 40 ...`). The usual causes are a temporary
download failure (run the failed command again by hand with `sudo`) or the Jenkins repository key having changed
(see Troubleshooting).

Then for each instance note the **Elastic IP** and the **Private IPv4 address** (instance → Details), fill in the
"Shared values" table in the README and post it in the group chat (hand-off H1). Jenkins uses the **private** IPs as
`TEST_HOST` / `PROD_HOST`: they never change, and traffic stays inside AWS.

## 9. SSH access for the team and for Jenkins (hand-off H2)

- **Jenkins deploy key:** give `frog-key.pem` privately to Role 2, who stores it as the Jenkins credential
  `app-servers-ssh` (user `ec2-user`). Every instance launched with frog-key accepts it.
- **Teammates:** each member runs **[PS]** `ssh-keygen -t ed25519` and sends you the content of
  `C:\Users\<them>\.ssh\id_ed25519.pub`. On every server **[EC2]**:
  ```bash
  echo 'ssh-ed25519 AAAA... member@laptop' >> ~/.ssh/authorized_keys
  ```
  They can then `ssh ec2-user@<elastic-ip>` with their own key, and nobody else needs a copy of frog-key.pem.

## 10. Keep the cost under control

Approximate on-demand prices in Singapore (check https://aws.amazon.com/ec2/pricing/on-demand/):

| Item | Running | Stopped |
|---|---|---|
| frog-jenkins t3.medium | ≈ US$0.053/h | 0 |
| frog-test + frog-prod, 2 × t3.small | ≈ US$0.053/h | 0 |
| 3 public IPv4 / Elastic IPs | ≈ US$0.015/h | ≈ US$0.015/h |
| 52 GiB gp3 disks | ≈ US$5/month | ≈ US$5/month |
| **Total** | **≈ US$0.12/h ≈ US$2.90/day** | **≈ US$16/month** |

- **Stop the three instances when nobody is working** (select all → Instance state → **Stop**). Elastic IPs, disks and
  everything on them are kept; start them again before the next session (the containers restart by themselves).
- Any member can stop/start them with their own IAM login – agree in the group chat who stops them at the end of
  the day.
- Before the presentation leave them running; after marking, **terminate** them and release the Elastic IPs (step 12).
- If a budget email arrives, check **Billing → Bills** for what is costing money.

## 11. Reproducibility test (success metric)

Before the presentation, prove a server can be rebuilt from the repository scripts in under 30 minutes:
note the time, **terminate frog-test**, launch a new one exactly as in step 6, associate the same Elastic IP,
let Role 2 run the pipeline (it redeploys the app automatically), note the time again. Update the new private IP
in Jenkins (`TEST_HOST`). 📷 timings + the green pipeline run.

## 12. Clean up after marking

Terminate the three instances (disks are deleted with them) → release the three Elastic IPs → delete the key pair
and security groups → delete the IAM users → check **Billing → Bills** shows nothing running → optionally close
the account (account menu → Account → Close account).

---

## Troubleshooting

| Problem | Fix |
|---|---|
| "Your account is pending verification" / cannot launch | wait (up to 24 h) for AWS to finish verifying the card and phone |
| `VcpuLimitExceeded` | new accounts sometimes have a low vCPU quota: Service Quotas → Amazon EC2 → *Running On-Demand Standard instances* → Request increase (e.g. to 8), or launch Jenkins first and test/prod after approval |
| Instance type not allowed on the Free plan | use a *Free tier eligible* type (step 6 note) |
| Instances "missing" in the console | wrong region – switch to ap-southeast-1 |
| SSH times out | `jenkins-sg`/`app-sg` missing port 22, or wrong IP (Elastic IP vs old public IP) |
| `UNPROTECTED PRIVATE KEY FILE` | run the `icacls` line in step 4 |
| `Permission denied (publickey)` | wrong user (always `ec2-user`) or wrong key file |
| User data did nothing | it only runs at **first** boot; check `/var/log/cloud-init-output.log`, or run the script by hand: copy it with scp, then `sudo bash user-data-app-server.sh` |
| `dnf install jenkins` GPG/key error | Jenkins rotated its signing key: open https://pkg.jenkins.io/redhat-stable/ and use the key named there in the `rpm --import` line, then `sudo dnf install -y jenkins` |

## Evidence checklist (for Role 4)

- [ ] Root MFA · budgets · IAM users and groups · region and key pair
- [ ] Security groups with rules · 3 instances Running with types visible · Elastic IPs associated
- [ ] cloud-init "DONE" lines · `docker version` on each server · Jenkins unlock page · reproducibility timings

## Done when

- [ ] Every member can sign in to the console with their own IAM user (MFA on)
- [ ] All three servers reachable by SSH with their Elastic IPs; Docker works on each; Jenkins page loads
- [ ] Budgets active; shared values table filled in; Role 2 has the deploy key and the initial admin password
