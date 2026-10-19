#!/bin/bash
# EC2 user data for the second Docker device (Amazon Linux 2023, x86_64).
# Paste into "Advanced details -> User data" when launching the instance.
# It runs ONCE, as root, at first boot. Output: /var/log/cloud-init-output.log
set -euxo pipefail

# 1. Install Docker Engine from the Amazon Linux repository.
dnf update -y
dnf install -y docker

# 2. Start Docker now and on every reboot.
systemctl enable --now docker

# 3. Let ec2-user run docker without sudo (takes effect at the NEXT login - see error E5).
usermod -aG docker ec2-user

# 4. Docker Compose v2 plugin (Amazon Linux 2023 does not package it).
mkdir -p /usr/local/lib/docker/cli-plugins
curl -fsSL "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-$(uname -m)" \
     -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

# 5. Record the versions in the log.
docker version
docker compose version
echo "Docker setup finished"
