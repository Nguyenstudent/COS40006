#!/usr/bin/env bash
# Smoke test a deployed environment from outside the server.
#   bash deploy/smoke-test.sh http://<host>     (exit code 0 = all checks passed)
set -uo pipefail
base="${1%/}"
fail=0

check() {   # check <path> <expected text>
  local path="$1" expect="$2" body code
  for _ in 1 2 3 4 5; do
    body="$(curl -sSL --max-time 10 -w '\n%{http_code}' "$base$path" 2>&1)"
    code="${body##*$'\n'}"
    [ "$code" = "200" ] && break
    sleep 3
  done
  if [ "$code" = "200" ] && grep -qi -- "$expect" <<< "$body"; then
    echo "PASS  $path  (HTTP $code)"
  else
    echo "FAIL  $path  (HTTP $code, expected 200 containing '$expect')"
    fail=1
  fi
}

check /api/health '"status":"ok"'
check /vi "<html"   # -L: with Supabase configured this follows the redirect to the login page
check /en '<html'
curl -sS --max-time 10 "$base/api/health" && echo
exit $fail
