#!/bin/bash
# EC2 user data for the TEST and PRODUCTION servers (Amazon Linux 2023, x86_64, t3.small, 16 GiB).
# Paste into EC2 > Launch instance > Advanced details > User data. Runs once as root at first boot.
# Log: /var/log/cloud-init-output.log   Finished when it prints "APP SERVER SETUP DONE".
set -euxo pipefail

dnf update -y
dnf install -y docker git

# 1 GiB swap as a safety margin on the 2 GiB t3.small (app + monitoring stack on production)
if [ ! -f /swapfile ]; then
  dd if=/dev/zero of=/swapfile bs=1M count=1024
  chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
  echo '/swapfile none swap defaults 0 0' >> /etc/fstab
fi

systemctl enable --now docker
usermod -aG docker ec2-user   # Jenkins deploys as ec2-user over SSH

# Docker Compose v2 plugin (not packaged for Amazon Linux 2023)
mkdir -p /usr/local/lib/docker/cli-plugins
curl -fsSL "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-$(uname -m)" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

sudo -u ec2-user mkdir -p /home/ec2-user/frog /home/ec2-user/monitoring
docker version; docker compose version
echo "APP SERVER SETUP DONE"
