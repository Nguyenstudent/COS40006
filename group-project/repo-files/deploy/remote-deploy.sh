#!/usr/bin/env bash
# Runs on the Jenkins server: copies the deployment files to a target server and runs deploy.sh there.
#   bash deploy/remote-deploy.sh <host> <image> <tag>
# Needs an SSH key for ec2-user on the target (Jenkins provides it through the sshagent step).
set -euo pipefail
host="$1"; image="$2"; tag="$3"
here="$(cd "$(dirname "$0")" && pwd)"
ssh_opts=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=15)

ssh "${ssh_opts[@]}" "ec2-user@$host" "mkdir -p ~/frog"
scp "${ssh_opts[@]}" "$here/compose.yaml" "$here/deploy.sh" "ec2-user@$host:~/frog/"
ssh "${ssh_opts[@]}" "ec2-user@$host" "chmod +x ~/frog/deploy.sh && ~/frog/deploy.sh '$image' '$tag'"
