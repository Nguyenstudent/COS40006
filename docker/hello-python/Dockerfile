# Task 4.2 - Python hello-world web server.
# Starts from plain Ubuntu, which has NO Python, and installs the Python run-time itself.
FROM ubuntu:24.04

# Add the Python run-time (one layer; the apt lists are deleted to keep the layer small).
RUN apt-get update \
 && apt-get install -y --no-install-recommends python3 \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY app.py .

# Print log lines immediately so that docker logs shows them.
ENV PYTHONUNBUFFERED=1

# Run as the image's non-root user instead of root.
USER ubuntu

EXPOSE 8080

# docker stop sends SIGINT; Python turns it into KeyboardInterrupt and the server stops at once
# (with the default SIGTERM, Docker would wait 10 seconds and then kill the process).
STOPSIGNAL SIGINT

CMD ["python3", "app.py"]
