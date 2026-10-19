"""Hello-world web server for SWE40006 Task 4.2.

Uses only the Python standard library, so the image needs nothing except the
Python run-time. The page shows where the container is running:
container ID, Python version, operating system and the Docker host's name.
"""
import os
import platform
import socket
import sys
from html import escape
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

# Listen on all interfaces. 127.0.0.1 would only accept connections from
# inside the container, so the published port (-p 8080:8080) could not reach it.
HOST = "0.0.0.0"
PORT = int(os.environ.get("PORT", "8080"))
DOCKER_HOST_NAME = os.environ.get("DOCKER_HOST_NAME", "not set (use docker run -e DOCKER_HOST_NAME=...)")


def os_name():
    """Return the distribution name from /etc/os-release, e.g. 'Ubuntu 24.04.3 LTS'."""
    try:
        with open("/etc/os-release", encoding="utf-8") as f:
            for line in f:
                if line.startswith("PRETTY_NAME="):
                    return line.split("=", 1)[1].strip().strip('"')
    except OSError:
        pass
    return platform.platform()


PAGE = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Hello from Docker</title>
<style>
  body {{ font-family: system-ui, Segoe UI, Arial, sans-serif; background: #f4f6fb; color: #1f2937; margin: 0; }}
  main {{ max-width: 640px; margin: 48px auto; background: #fff; padding: 32px; border-radius: 12px;
          box-shadow: 0 2px 12px rgba(0,0,0,.08); }}
  h1 {{ margin-top: 0; color: #1d63ed; }}
  table {{ border-collapse: collapse; width: 100%; }}
  td {{ padding: 8px 6px; border-bottom: 1px solid #e5e7eb; }}
  td:first-child {{ font-weight: 600; width: 40%; }}
  footer {{ margin-top: 24px; font-size: 13px; color: #6b7280; }}
</style>
</head>
<body>
<main>
  <h1>Hello, World!</h1>
  <p>This page is served by a Python web server running inside a Docker container.</p>
  <table>
    <tr><td>Container ID</td><td>{container}</td></tr>
    <tr><td>Python version</td><td>{python}</td></tr>
    <tr><td>Operating system (image)</td><td>{os}</td></tr>
    <tr><td>Docker host</td><td>{host}</td></tr>
  </table>
  <footer>SWE40006 Task 4.2 &ndash; Nguyen Tuan Kiet (105004515)</footer>
</main>
</body>
</html>
"""


class Handler(BaseHTTPRequestHandler):
    server_version = "hello-python/1.0"

    def _send(self, status, content_type, body):
        data = body.encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path == "/health":
            self._send(200, "text/plain; charset=utf-8", "ok\n")
        elif self.path in ("/", "/index.html"):
            page = PAGE.format(
                container=escape(socket.gethostname()),  # Docker sets the hostname to the short container ID
                python=escape(platform.python_version()),
                os=escape(os_name()),
                host=escape(DOCKER_HOST_NAME),
            )
            self._send(200, "text/html; charset=utf-8", page)
        else:
            self._send(404, "text/plain; charset=utf-8", "404 - not found\n")


def main():
    server = ThreadingHTTPServer((HOST, PORT), Handler)
    print(f"hello-python listening on http://{HOST}:{PORT} (Python {platform.python_version()})", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        # docker stop sends SIGINT (see STOPSIGNAL in the Dockerfile), which Python raises as KeyboardInterrupt.
        print("Stopping server", flush=True)
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
