# Task 4.3 - Study Log web app (Flask + Gunicorn). Redis runs in its own container (compose.yaml).
FROM python:3.13-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

# Non-root user with a home directory (Gunicorn keeps its control socket in ~/.gunicorn).
RUN useradd --create-home appuser

WORKDIR /app

# Copy and install the requirements BEFORE the code: when only the code changes,
# this layer comes from the build cache and pip does not run again.
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app.py test_app.py ./
COPY templates/ templates/

USER appuser
EXPOSE 8000

# Docker marks the container healthy only when /health answers (it pings Redis).
# timeout=10s: raised from 3s after error E2 (see the report).
HEALTHCHECK --interval=15s --timeout=10s --start-period=10s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=8)"

CMD ["gunicorn", "--bind", "0.0.0.0:8000", "--workers", "2", "--access-logfile", "-", "app:app"]
