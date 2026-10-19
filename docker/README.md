# SWE40006 Task 4 – Deploying containers using Docker

Nguyen Tuan Kiet – 105004515. Source code and scripts for the Task 4 report.
Replace `<docker-id>` with your Docker Hub username.

| Folder / file | Sub-task | What it is |
|---|---|---|
| `hello-python/` | 4.2 | Python hello-world web server on `ubuntu:24.04` with the Python run-time installed in the Dockerfile |
| `studylog/` | 4.3 | Study Log web app: Flask + Gunicorn and Redis, two containers run with Docker Compose |
| `gradetracker-cli/` | 4.4 | Grade Tracker CLI: .NET 10 console app (non-web) that reuses `src/GradeTracker.Core` from Task 1; multi-stage Dockerfile |
| `ec2-user-data.sh` | 4.2 | EC2 user data: installs Docker Engine and the Compose plugin on Amazon Linux 2023 |

## Build and run (PowerShell on the PC)

```powershell
# 4.2 hello-python  (port 8080 on this PC is used by another program, so host port 8081)
cd docker/hello-python
docker build -t <docker-id>/hello-python:1.0 .
docker run -d --name hello-web -p 8081:8080 -e DOCKER_HOST_NAME=$env:COMPUTERNAME <docker-id>/hello-python:1.0

# 4.3 Study Log
cd ../studylog
$env:DOCKERHUB_USER = "<docker-id>"
docker compose build
docker compose up -d                       # http://localhost

# 4.4 Grade Tracker CLI (build from the repository root)
cd ../..
docker build -f docker/gradetracker-cli/Dockerfile -t <docker-id>/gradetracker-cli:1.0 .
docker run --rm <docker-id>/gradetracker-cli:1.0
Get-Content docker/gradetracker-cli/bad-units.csv | docker run -i --rm <docker-id>/gradetracker-cli:1.0 -
```

## Run on another Docker device (EC2, Amazon Linux 2023)

```bash
docker run -d --name hello-web -p 8080:8080 -e DOCKER_HOST_NAME=$(hostname) --restart unless-stopped <docker-id>/hello-python:1.0

mkdir -p ~/studylog && cd ~/studylog
curl -fsSLO https://raw.githubusercontent.com/Nguyenstudent/COS40006/main/docker/studylog/compose.yaml
export DOCKERHUB_USER=<docker-id>
docker compose pull && docker compose up -d

docker run --rm <docker-id>/gradetracker-cli:1.0
```
