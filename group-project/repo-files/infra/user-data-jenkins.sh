#!/bin/bash
# EC2 user data for the Jenkins CI/build server (Amazon Linux 2023, x86_64, t3.medium, 20 GiB).
# Paste into EC2 > Launch instance > Advanced details > User data. Runs once as root at first boot.
# Log: /var/log/cloud-init-output.log   Finished when it prints "JENKINS SETUP DONE".
set -euxo pipefail

dnf update -y
dnf install -y git docker fontconfig java-21-amazon-corretto-headless tar xz

# 2 GiB swap: the Next.js build plus Jenkins can briefly exceed 4 GiB of RAM.
if [ ! -f /swapfile ]; then
  dd if=/dev/zero of=/swapfile bs=1M count=2048
  chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
  echo '/swapfile none swap defaults 0 0' >> /etc/fstab
fi

# Docker (image builds, postgres:17 for the database tests)
systemctl enable --now docker

# Node.js 24 (the app requires >= 24): latest 24.x from nodejs.org into /opt, linked into /usr/local/bin
NODE_DIST=https://nodejs.org/dist/latest-v24.x
NODE_FILE=$(curl -fsSL "$NODE_DIST/SHASUMS256.txt" | awk '/linux-x64.tar.xz$/ {print $2}')
curl -fsSL "$NODE_DIST/$NODE_FILE" | tar -xJ -C /opt
ln -sf "/opt/${NODE_FILE%.tar.xz}/bin/node" /usr/local/bin/node
ln -sf "/opt/${NODE_FILE%.tar.xz}/bin/npm" /usr/local/bin/npm
ln -sf "/opt/${NODE_FILE%.tar.xz}/bin/npx" /usr/local/bin/npx

# Jenkins LTS from the official repository
curl -fsSL -o /etc/yum.repos.d/jenkins.repo https://pkg.jenkins.io/redhat-stable/jenkins.repo
rpm --import https://pkg.jenkins.io/redhat-stable/jenkins.io-2023.key
dnf install -y jenkins
usermod -aG docker jenkins          # let pipeline steps run docker without sudo
systemctl enable --now jenkins

# Pre-pull the database test image (the test runner uses --pull=never)
docker pull postgres:17

node --version; java -version; docker --version
echo "Initial admin password: $(cat /var/lib/jenkins/secrets/initialAdminPassword 2>/dev/null || echo 'not ready yet')"
echo "JENKINS SETUP DONE"
