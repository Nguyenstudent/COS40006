"""Study Log - SWE40006 Task 4.3.

A Flask web app that records how long a student studies each unit.
Data is stored in Redis, which runs in a second container (see compose.yaml).
"""
import json
import os
import re
import socket
import time

import redis
from flask import Flask, redirect, render_template, request, url_for
from redis.backoff import NoBackoff
from redis.retry import Retry

app = Flask(__name__)

UNIT_RE = re.compile(r"^[A-Z]{3}\d{5}$")      # e.g. SWE40006
MINUTES_RE = re.compile(r"^\d{1,3}$", re.ASCII)
MAX_MINUTES = 600
MAX_NOTE = 100
MAX_SESSIONS = 500                              # only the latest 500 sessions are kept

SESSIONS_KEY = "studylog:sessions"              # list of JSON strings, newest first
TOTALS_KEY = "studylog:totals"                  # hash: unit code -> total minutes
COUNT_KEY = "studylog:count"                    # number of sessions ever added

# "redis" is the service name in compose.yaml; Docker's internal DNS resolves it
# to the Redis container. The client fails fast (2-second connect timeout, one retry,
# no backoff) so that a Redis outage gives a 503 page in a few seconds instead of
# blocking a Gunicorn worker past its 30-second timeout (error E2 in the report).
db = redis.Redis(
    host=os.environ.get("REDIS_HOST", "redis"),
    port=int(os.environ.get("REDIS_PORT", "6379")),
    decode_responses=True,
    socket_connect_timeout=2,
    socket_timeout=2,
    retry=Retry(NoBackoff(), 1),
)

UNAVAILABLE = ("<h1>503 - Database unavailable</h1>"
               "<p>The Redis container cannot be reached. Try again shortly.</p>")


def validate(form):
    """Check the submitted form. Returns (errors, cleaned values). Needs no Redis."""
    unit = (form.get("unit") or "").strip().upper()
    minutes_text = (form.get("minutes") or "").strip()
    note = (form.get("note") or "").strip()
    errors = []

    if not UNIT_RE.match(unit):
        errors.append("Unit code must be three letters followed by five digits, for example SWE40006.")
    minutes = int(minutes_text) if MINUTES_RE.match(minutes_text) else None
    if minutes is None or not 1 <= minutes <= MAX_MINUTES:
        errors.append(f"Minutes must be a whole number from 1 to {MAX_MINUTES}.")
    if len(note) > MAX_NOTE:
        errors.append(f"The note must be {MAX_NOTE} characters or fewer.")

    return errors, {"unit": unit, "minutes": minutes, "note": note}


def hours_minutes(total):
    return f"{total // 60} h {total % 60} min"


def load_page_data():
    totals = {unit: int(m) for unit, m in db.hgetall(TOTALS_KEY).items()}
    rows = sorted(totals.items(), key=lambda kv: (-kv[1], kv[0]))
    biggest = max(totals.values(), default=1)
    recent = [json.loads(s) for s in db.lrange(SESSIONS_KEY, 0, 9)]
    return {
        "totals": [{"unit": u, "minutes": m, "label": hours_minutes(m), "pct": round(m * 100 / biggest)}
                   for u, m in rows],
        "grand_total": hours_minutes(sum(totals.values())),
        "count": int(db.get(COUNT_KEY) or 0),
        "recent": recent,
        "redis_version": db.info("server").get("redis_version", "?"),
        "container_id": socket.gethostname(),
    }


@app.get("/")
def index():
    return render_template("index.html", errors=[], form={}, **load_page_data())


@app.post("/")
def add_session():
    errors, values = validate(request.form)
    if errors:
        return render_template("index.html", errors=errors, form=request.form, **load_page_data()), 400

    session = {**values, "time": time.strftime("%Y-%m-%d %H:%M")}
    # MULTI/EXEC: the three changes are applied together or not at all.
    pipe = db.pipeline(transaction=True)
    pipe.lpush(SESSIONS_KEY, json.dumps(session))
    pipe.ltrim(SESSIONS_KEY, 0, MAX_SESSIONS - 1)
    pipe.hincrby(TOTALS_KEY, values["unit"], values["minutes"])
    pipe.incr(COUNT_KEY)
    pipe.execute()
    # Post/Redirect/Get: refreshing the page after saving does not submit the form again.
    return redirect(url_for("index"), code=303)


@app.get("/health")
def health():
    db.ping()       # raises RedisError if Redis is down -> 503 from the handler below
    return "ok\n", 200, {"Content-Type": "text/plain; charset=utf-8"}


@app.errorhandler(redis.exceptions.RedisError)
def redis_unavailable(error):
    app.logger.error("Redis error: %s", error)
    return UNAVAILABLE, 503
