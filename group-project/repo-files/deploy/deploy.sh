#!/usr/bin/env bash
# Runs ON the test/production server (in ~/frog). Called by Jenkins through remote-deploy.sh.
#   ./deploy.sh <image> <tag>    deploy a release; if it cannot be pulled, started or never becomes
#                                healthy, the previous release is restored automatically
#   ./deploy.sh --rollback       go back to the previous release
# current.env holds the running release, previous.env the one before it.
set -euo pipefail
cd "$(dirname "$0")"

up() {
  docker compose --env-file current.env pull --quiet &&
    docker compose --env-file current.env up -d --remove-orphans
}

healthy() {
  for _ in $(seq 1 45); do
    if curl -fsS --max-time 3 http://localhost/api/health > /dev/null; then return 0; fi
    sleep 2
  done
  return 1
}

release() { tr '\n' ' ' < current.env; }

if [ "${1:-}" = "--rollback" ]; then
  [ -f previous.env ] || { echo "No previous release to roll back to."; exit 1; }
  cp previous.env current.env
  echo "Rolling back to $(release)"
  if up && healthy; then echo "Rollback healthy."; exit 0; fi
  echo "Rollback did not become healthy - check: docker compose --env-file current.env logs app"
  exit 1
fi

[ $# -eq 2 ] || { echo "usage: $0 <image> <tag> | --rollback"; exit 2; }
[ -f previous.env ] && cp previous.env previous.env.bak   # restored if this deployment fails
[ -f current.env ] && cp current.env previous.env
printf 'IMAGE=%s\nTAG=%s\n' "$1" "$2" > current.env
echo "Deploying $1:$2"

if up && healthy; then
  echo "Release $2 is healthy."
  rm -f previous.env.bak
  docker image prune -f > /dev/null   # keep the disk from filling up with old releases
  exit 0
fi

echo "Release $2 could not be pulled, started or made healthy:"
docker compose --env-file current.env logs --tail 30 app || true
if [ -f previous.env ]; then
  cp previous.env current.env
  [ -f previous.env.bak ] && mv previous.env.bak previous.env   # keep the older release as the rollback target
  echo "Automatic rollback to $(release)"
  up && healthy && echo "Previous release is serving again." || echo "Rollback also failed - investigate now."
else
  echo "No previous release to restore (first deployment)."
fi
exit 1
