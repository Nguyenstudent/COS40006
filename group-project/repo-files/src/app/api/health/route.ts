// Health check for the DevOps pipeline: smoke tests, the Docker HEALTHCHECK and the
// Prometheus blackbox probe all call GET /api/health. It never touches the database,
// so it reports whether the web server itself is up.
export const dynamic = "force-dynamic";

// Bump this (and push) to see a new release flow through the pipeline.
const RELEASE = "1.0.0";
const startedAt = new Date().toISOString();

export function GET() {
  return Response.json(
    {
      status: "ok",
      release: RELEASE,
      commit: process.env.APP_COMMIT ?? "dev",
      startedAt,
      uptimeSeconds: Math.round(process.uptime()),
    },
    { headers: { "Cache-Control": "no-store" } },
  );
}
