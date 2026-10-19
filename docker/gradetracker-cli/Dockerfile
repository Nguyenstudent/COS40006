# Task 4.4 - Grade Tracker CLI (non-web .NET console app). Multi-stage build.
# Build from the REPOSITORY ROOT, because the app needs src/GradeTracker.Core:
#   docker build -f docker/gradetracker-cli/Dockerfile -t <docker-id>/gradetracker-cli:1.0 .

# ---- Stage 1: build with the full .NET SDK (about 900 MB, never shipped) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first and restore: this layer stays cached until a .csproj changes.
COPY src/GradeTracker.Core/GradeTracker.Core.csproj src/GradeTracker.Core/
COPY docker/gradetracker-cli/GradeTracker.Cli.csproj docker/gradetracker-cli/
RUN dotnet restore docker/gradetracker-cli/GradeTracker.Cli.csproj

# Then copy the source code and publish.
COPY src/GradeTracker.Core/ src/GradeTracker.Core/
COPY docker/gradetracker-cli/ docker/gradetracker-cli/
RUN dotnet publish docker/gradetracker-cli/GradeTracker.Cli.csproj -c Release -o /app --no-restore

# ---- Stage 2: run with the small .NET run-time image only ----
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
COPY docker/gradetracker-cli/sample-units.csv .

# Non-root user that the .NET images provide (UID 1654).
USER $APP_UID

# ENTRYPOINT = the program, CMD = its default argument.
# "docker run <image> /data/units.csv" replaces only the argument.
ENTRYPOINT ["dotnet", "GradeTracker.Cli.dll"]
CMD ["/app/sample-units.csv"]
